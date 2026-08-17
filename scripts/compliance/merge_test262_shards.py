#!/usr/bin/env python3
"""Merge test262 shard reports and prepare CI triage outputs.

Only artifacts whose names match the selected phase are considered:

    test262-PHASE-shard-N.json
    test262-PHASE-shard-N-status.json
    test262-PHASE-shard-N-retry.json
    test262-PHASE-shard-N-retry-status.json

The retry attempt, when present, replaces the original attempt completely. A
well-formed runner report is conclusive even when it contains ordinary test
failures. Missing and malformed reports are infrastructure gaps and are
reported as incomplete shards so the workflow can retry exactly those slices.
"""

from __future__ import annotations

import argparse
import json
import os
import re
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Callable, Iterable


DEFAULT_PROBLEM_LIMIT = 10
DEFAULT_BIGGEST_PROBLEM_LIMIT = 3
DEFAULT_TIMEOUT_LIMIT = 10
PATH_SAMPLE_LIMIT = 5
_RUN_CONFIGURATION_FIELDS = (
    "suiteRef",
    "selectionMode",
    "selectionLabel",
    "candidateCount",
    "selectedCountBeforeSharding",
    "shardCount",
    "subsetPatterns",
    "featurePatterns",
    "featureMatch",
    "testTimeoutSeconds",
    "memoryLimitMb",
    "maxWorkers",
    "shuffleSeed",
    "includeNegative",
    "prioritizeFragile",
    "runnerOs",
    "runnerArch",
    "dotnetVersion",
)

_ARTIFACT_NAME = re.compile(
    r"^test262-(?P<phase>.+)-shard-(?P<index>\d+)"
    r"(?P<retry>-retry)?(?P<status>-status)?\.json$"
)
_ANSI_ESCAPE = re.compile(r"\x1b\[[0-?]*[ -/]*[@-~]")
_HEX_ADDRESS = re.compile(r"\b0x[0-9a-fA-F]+\b")
_GUID = re.compile(
    r"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-"
    r"[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b"
)
_LINE_NUMBER = re.compile(r"(?i)(\bline\s+|:\s*)\d+(?:(?:,|:)\d+)?")
_LONG_NUMBER = re.compile(r"\b\d{3,}\b")
_EXCEPTION_HEADER = re.compile(
    r"^(?:Unhandled exception\.\s*)?"
    r"(?P<type>(?:[A-Za-z_][\w+]*\.)*[A-Za-z_][\w+]*(?:Exception|Error))"
    r"(?::\s*(?P<message>.*))?$",
    re.IGNORECASE,
)
_STACK_FRAME = re.compile(r"^\s*at\s+(?P<context>.+?)(?:\s+in\s+|\(|:|$)")

_STATUS_ALIASES = {
    "passed": "passed",
    "pass": "passed",
    "failed": "failed",
    "fail": "failed",
    "skipped": "skipped",
    "skip": "skipped",
    "timedout": "timedOut",
    "timeout": "timedOut",
    "timed-out": "timedOut",
}
_STATUS_RANK = {
    "skipped": 0,
    "passed": 1,
    "failed": 2,
    "timedOut": 3,
}
_BIGGEST_KIND_RANK = {
    "IncompleteShards": 5,
    "Crash": 4,
    "NoOutput": 2,
    "Failure": 1,
}


@dataclass(frozen=True)
class Artifact:
    path: Path
    phase: str
    shard_index: int
    attempt: int
    is_status: bool
    payload: Any
    read_error: str | None


def _normalise_path(value: object) -> str:
    path = str(value or "").strip().replace("\\", "/")
    while path.startswith("./"):
        path = path[2:]
    return re.sub(r"/+", "/", path)


def _canonical_status(value: object) -> str | None:
    return _STATUS_ALIASES.get(str(value or "").strip().lower())


def _read_artifact(path: Path, match: re.Match[str]) -> Artifact:
    payload: Any = None
    error: str | None = None
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        error = f"invalid JSON at line {exc.lineno}, column {exc.colno}"
    except OSError as exc:
        error = f"could not read artifact: {exc}"

    return Artifact(
        path=path,
        phase=match.group("phase"),
        shard_index=int(match.group("index")),
        attempt=1 if match.group("retry") else 0,
        is_status=bool(match.group("status")),
        payload=payload,
        read_error=error,
    )


def discover_artifacts(shard_dir: Path, phase: str) -> list[Artifact]:
    """Return phase-matching report and status artifacts recursively."""
    artifacts: list[Artifact] = []
    if not shard_dir.exists():
        return artifacts
    for path in sorted(shard_dir.rglob("*.json"), key=lambda item: item.as_posix()):
        match = _ARTIFACT_NAME.fullmatch(path.name)
        if match is None or match.group("phase") != phase:
            continue
        artifacts.append(_read_artifact(path, match))
    return artifacts


def _validate_status(artifact: Artifact) -> str | None:
    if artifact.read_error:
        return artifact.read_error
    payload = artifact.payload
    if not isinstance(payload, dict):
        return "status document is not a JSON object"
    if not any(key in payload for key in ("exitCode", "failureReason", "failureDetail")):
        return "status document has no exitCode or failure reason"
    if "shardIndex" in payload:
        try:
            embedded_index = int(payload["shardIndex"])
        except (TypeError, ValueError):
            return "status shardIndex is not an integer"
        if embedded_index != artifact.shard_index:
            return (
                f"status shardIndex {embedded_index} does not match filename "
                f"index {artifact.shard_index}"
            )
    return None


def _validate_report(artifact: Artifact) -> str | None:
    if artifact.read_error:
        return artifact.read_error
    payload = artifact.payload
    if not isinstance(payload, dict):
        return "report is not a JSON object"

    results = payload.get("results")
    if not isinstance(results, list):
        return "report has no results array"

    if "shardIndex" in payload:
        try:
            embedded_index = int(payload["shardIndex"])
        except (TypeError, ValueError):
            return "report shardIndex is not an integer"
        if embedded_index != artifact.shard_index:
            return (
                f"report shardIndex {embedded_index} does not match filename "
                f"index {artifact.shard_index}"
            )

    result_paths: list[str] = []
    status_counts: Counter[str] = Counter()
    for position, result in enumerate(results):
        if not isinstance(result, dict):
            return f"result {position} is not a JSON object"
        path = _normalise_path(result.get("path"))
        if not path:
            return f"result {position} has no path"
        status = _canonical_status(result.get("status"))
        if status is None:
            return f"result {position} has unknown status {result.get('status')!r}"
        result_paths.append(path)
        status_counts[status] += 1

    expanded_paths = payload.get("expandedPaths")
    if expanded_paths is not None:
        if not isinstance(expanded_paths, list) or any(
            not _normalise_path(path) for path in expanded_paths
        ):
            return "expandedPaths is not an array of paths"
        expected_paths = sorted({_normalise_path(path) for path in expanded_paths})
        if sorted(set(result_paths)) != expected_paths:
            return "results do not cover every expanded path"

    if "selectedCountBeforeSharding" in payload:
        try:
            selected_count = int(payload["selectedCountBeforeSharding"])
        except (TypeError, ValueError):
            return "report selectedCountBeforeSharding is not an integer"
        if selected_count < 0:
            return "report selectedCountBeforeSharding is negative"

    declared_keys = {
        "passed": status_counts["passed"],
        "failed": status_counts["failed"],
        "skipped": status_counts["skipped"],
        "timedOut": status_counts["timedOut"],
        "executed": (
            status_counts["passed"]
            + status_counts["failed"]
            + status_counts["timedOut"]
        ),
    }
    for key, actual in declared_keys.items():
        if key not in payload:
            continue
        try:
            declared = int(payload[key])
        except (TypeError, ValueError):
            return f"report {key} count is not an integer"
        if declared != actual:
            return f"report {key} count {declared} does not match results ({actual})"

    return None


def _select_valid_artifact(
    artifacts: Iterable[Artifact],
    validator: Callable[[Artifact], str | None],
) -> tuple[Artifact | None, list[str]]:
    valid: list[Artifact] = []
    errors: list[str] = []
    for artifact in sorted(artifacts, key=lambda item: item.path.as_posix()):
        error = validator(artifact)
        if error is None:
            valid.append(artifact)
        else:
            errors.append(f"{artifact.path.name}: {error}")
    # Duplicate downloads are possible when artifacts retain their own
    # directories. Choose deterministically and never sum duplicate copies.
    return (valid[-1] if valid else None), errors


def _human_incomplete_reason(entry: dict[str, Any]) -> str:
    reason = str(entry.get("failureReason") or "IncompleteShard")
    detail = str(entry.get("failureDetail") or "").strip()
    suffix = " after retry" if entry.get("retried") else ""
    if detail:
        return f"{reason}{suffix}: {detail}"
    exit_code = entry.get("exitCode", "unknown")
    if exit_code not in ("unknown", "missing", None):
        return f"{reason}{suffix} (exit {exit_code})"
    return f"{reason}{suffix}"


def _incomplete_entry(
    shard_index: int,
    attempt: int,
    report_errors: list[str],
    status_artifact: Artifact | None,
    status_errors: list[str],
    discovered: bool,
) -> dict[str, Any]:
    status = status_artifact.payload if status_artifact is not None else {}
    exit_code: object = status.get("exitCode", "missing" if not discovered else "unknown")
    recorded_reason = str(status.get("failureReason") or "").strip()
    recorded_detail = str(status.get("failureDetail") or "").strip()

    if recorded_reason:
        failure_reason = recorded_reason
        detail = recorded_detail
        extra_errors = report_errors + status_errors
        if extra_errors:
            diagnostic = "; ".join(extra_errors)
            detail = f"{detail}; {diagnostic}" if detail else diagnostic
    elif report_errors:
        failure_reason = "MalformedReport"
        detail = "; ".join(report_errors + status_errors)
    elif status_errors:
        failure_reason = "MalformedStatus"
        detail = "; ".join(status_errors)
    elif discovered:
        failure_reason = "MissingReport"
        detail = recorded_detail or "A status artifact was uploaded without a complete report."
    else:
        failure_reason = "MissingReport"
        detail = "Expected shard uploaded neither a report nor a status artifact."

    entry: dict[str, Any] = {
        "shardIndex": shard_index,
        "attempt": "retry" if attempt else "initial",
        "retried": attempt > 0,
        "exitCode": exit_code,
        "failureReason": failure_reason,
        "failureDetail": detail,
    }
    entry["reason"] = _human_incomplete_reason(entry)
    return entry


def _selected_attempts(
    shard_dir: Path,
    phase: str,
    expected_shard_indexes: set[int] | None,
) -> tuple[list[Artifact], list[dict[str, Any]], list[int], list[int]]:
    artifacts = discover_artifacts(shard_dir, phase)
    buckets: dict[int, dict[int, dict[str, list[Artifact]]]] = defaultdict(
        lambda: {
            0: {"reports": [], "statuses": []},
            1: {"reports": [], "statuses": []},
        }
    )
    for artifact in artifacts:
        key = "statuses" if artifact.is_status else "reports"
        buckets[artifact.shard_index][artifact.attempt][key].append(artifact)

    # An explicit expected set is also the run's scope boundary. Artifact
    # downloads can be flattened from several jobs/phases, and a stale or
    # otherwise unexpected same-phase file must not affect totals or the
    # failure manifest for a targeted shard run.
    indexes = (
        set(expected_shard_indexes)
        if expected_shard_indexes is not None
        else set(buckets)
    )

    reports: list[Artifact] = []
    incomplete: list[dict[str, Any]] = []
    retried: list[int] = []
    reported: list[int] = []

    for shard_index in sorted(indexes):
        attempts = buckets.get(shard_index)
        discovered = attempts is not None
        if attempts is None:
            attempt = 0
            report_artifacts: list[Artifact] = []
            status_artifacts: list[Artifact] = []
        else:
            has_retry = bool(
                attempts[1]["reports"] or attempts[1]["statuses"]
            )
            attempt = 1 if has_retry else 0
            if has_retry:
                retried.append(shard_index)
            report_artifacts = attempts[attempt]["reports"]
            status_artifacts = attempts[attempt]["statuses"]

        report, report_errors = _select_valid_artifact(
            report_artifacts, _validate_report
        )
        status, status_errors = _select_valid_artifact(
            status_artifacts, _validate_status
        )

        # A complete report is conclusive. In particular, an exit code of one
        # simply means test262 found ordinary failures and must not trigger an
        # infrastructure retry.
        if report is not None:
            reports.append(report)
            reported.append(shard_index)
            continue

        incomplete.append(
            _incomplete_entry(
                shard_index,
                attempt,
                report_errors,
                status,
                status_errors,
                discovered,
            )
        )

    return reports, incomplete, retried, reported


def _normalise_result(result: dict[str, Any]) -> dict[str, Any]:
    normalised = {str(key): value for key, value in sorted(result.items())}
    normalised["path"] = _normalise_path(result.get("path"))
    normalised["status"] = _canonical_status(result.get("status"))
    features = _features(normalised)
    if features:
        normalised["features"] = features
    source_size = _source_size(normalised)
    if source_size is not None:
        normalised["sourceSizeBytes"] = source_size
    return normalised


def _result_preference(result: dict[str, Any]) -> tuple[int, int, str]:
    encoded = json.dumps(result, sort_keys=True, separators=(",", ":"), default=str)
    nonempty = sum(value not in (None, "", [], {}) for value in result.values())
    return (_STATUS_RANK[str(result["status"])], nonempty, encoded)


def _deduplicate_results(reports: Iterable[Artifact]) -> list[dict[str, Any]]:
    by_path: dict[str, dict[str, Any]] = {}
    for artifact in reports:
        for raw_result in artifact.payload["results"]:
            result = _normalise_result(raw_result)
            path = str(result["path"])
            previous = by_path.get(path)
            if previous is None or _result_preference(result) > _result_preference(previous):
                by_path[path] = result
    return [by_path[path] for path in sorted(by_path)]


def _aggregate_run_configuration(
    reports: list[Artifact],
) -> tuple[dict[str, Any], list[dict[str, str]]]:
    """Collect invariant shard settings and flag cross-shard drift."""
    configuration: dict[str, Any] = {}
    failures: list[dict[str, str]] = []
    missing = object()

    for field in _RUN_CONFIGURATION_FIELDS:
        values = [report.payload.get(field, missing) for report in reports]
        present = [value for value in values if value is not missing]
        if not present:
            continue

        encoded = {
            json.dumps(value, sort_keys=True, separators=(",", ":"), default=str)
            for value in present
        }
        has_missing = len(present) != len(values)
        if has_missing or len(encoded) != 1:
            failures.append(
                {
                    "kind": "InconsistentShardConfiguration",
                    "message": (
                        f"Shard reports disagree about {field}, or omit it in "
                        "only part of the run."
                    ),
                }
            )
            configuration[field] = sorted(encoded)
        else:
            configuration[field] = present[0]

    if reports and "suiteRef" not in configuration:
        failures.append(
            {
                "kind": "MissingSuiteRef",
                "message": "Conclusive shard reports did not identify the test262 suite commit.",
            }
        )
    return configuration, failures


def _features(result: dict[str, Any]) -> list[str]:
    value: object = result.get("features")
    if value is None and isinstance(result.get("metadata"), dict):
        value = result["metadata"].get("features")
    if value is None:
        return []
    if isinstance(value, str):
        values = [value]
    elif isinstance(value, (list, tuple, set)):
        values = list(value)
    else:
        return []
    return sorted({str(item).strip() for item in values if str(item).strip()})


def _source_size(result: dict[str, Any]) -> int | None:
    candidates: list[object] = [
        result.get("sourceSizeBytes"),
        result.get("fileSizeBytes"),
        result.get("source_size_bytes"),
    ]
    if isinstance(result.get("metadata"), dict):
        candidates.extend(
            [
                result["metadata"].get("sourceSizeBytes"),
                result["metadata"].get("fileSizeBytes"),
            ]
        )
    for candidate in candidates:
        if candidate is None or isinstance(candidate, bool):
            continue
        try:
            size = int(candidate)
        except (TypeError, ValueError):
            continue
        if size >= 0:
            return size
    return None


def _clean_line(value: object) -> str:
    text = _ANSI_ESCAPE.sub("", str(value or ""))
    text = _GUID.sub("{GUID}", text)
    text = _HEX_ADDRESS.sub("0x{ADDRESS}", text)
    text = _LINE_NUMBER.sub(lambda match: f"{match.group(1)}{{N}}", text)
    text = _LONG_NUMBER.sub("{N}", text)
    return " ".join(text.split()).strip()


def _first_output_line(result: dict[str, Any]) -> str:
    for field in ("stderr", "stdout"):
        for line in str(result.get(field) or "").splitlines():
            cleaned = _clean_line(line)
            if cleaned:
                return cleaned[:300]
    return ""


def _exception_signature(result: dict[str, Any]) -> tuple[str, str, str] | None:
    output = "\n".join(
        part
        for part in (
            str(result.get("stderr") or ""),
            str(result.get("stdout") or ""),
        )
        if part
    )
    lines = [line.strip() for line in output.splitlines() if line.strip()]
    for index, line in enumerate(lines):
        match = _EXCEPTION_HEADER.match(_ANSI_ESCAPE.sub("", line))
        if match is None:
            continue
        exception_type = match.group("type")
        message = _clean_line(match.group("message") or "(no message)")
        context = "(unknown context)"
        for following in lines[index + 1 :]:
            frame = _STACK_FRAME.match(following)
            if frame:
                context = _clean_line(frame.group("context"))
                break
        return exception_type, context, message
    return None


def _failure_descriptor(result: dict[str, Any]) -> tuple[str, str, str]:
    reason = _clean_line(result.get("reason"))
    explicit_kind = " ".join(
        str(result.get(key) or "")
        for key in ("category", "failureType", "kind")
    ).lower()
    output = "\n".join(
        str(result.get(field) or "") for field in ("stderr", "stdout")
    )
    crash_words = (
        "crash",
        "segmentation fault",
        "stack overflow",
        "fatal error",
        "core dumped",
        "access violation",
    )
    explicitly_a_crash = any(word in explicit_kind for word in crash_words)
    output_looks_fatal = any(word in output.lower() for word in crash_words)
    has_unhandled_exception = "unhandled exception" in output.lower()
    looks_like_crash = explicitly_a_crash or output_looks_fatal

    if result.get("infrastructure"):
        label = f"Crash: {reason or 'test runner infrastructure failure'}"
        return "Crash", f"crash|{label.lower()}", label

    exception = _exception_signature(result)
    is_javascript_exception = bool(
        exception
        and (
            exception[0].lower() == "jsexception"
            or exception[0].lower().endswith(".jsexception")
        )
    )
    # The script host deliberately surfaces an uncaught JavaScript throw via
    # .NET's unhandled-exception path. That is a conformance failure, not an
    # engine/process crash. Native/CLR exceptions and fatal process signals
    # remain severity-ranked as crashes.
    if is_javascript_exception and not explicitly_a_crash:
        looks_like_crash = False
    elif has_unhandled_exception:
        looks_like_crash = True

    # Negative tests can intentionally produce an exception of the wrong type.
    # Their explicit reason is more useful than calling the expected exception a
    # process crash.
    if reason and reason.lower().startswith("negative test expected") and not any(
        word in reason.lower() for word in ("crash", "fatal")
    ):
        label = f"Failure: {reason}"
        return "Failure", label.lower(), label

    if exception is not None and is_javascript_exception and not looks_like_crash:
        exception_type, context, message = exception
        label = f"Failure: {exception_type} at {context}: {message}"
        return "Failure", f"failure|{label.lower()}", label

    if exception is not None and (looks_like_crash or not reason):
        exception_type, context, message = exception
        label = f"{exception_type} at {context}: {message}"
        return "Crash", f"crash|{label.lower()}", label

    if reason:
        kind = "Crash" if looks_like_crash else "Failure"
        label = f"{kind}: {reason}"
        return kind, f"{kind.lower()}|{reason.lower()}", label

    line = _first_output_line(result)
    if line:
        kind = "Crash" if looks_like_crash else "Failure"
        label = f"{kind}: {line}"
        return kind, f"{kind.lower()}|{line.lower()}", label

    return "NoOutput", "no-output", "Failure with no output"


def _path_bucket(path: str, depth: int = 4) -> str:
    parts = [part for part in path.split("/") if part]
    return "/".join(parts[:depth]) if parts else "."


def _path_weight(path: str) -> float:
    lowered = path.lower()
    if lowered.startswith("test/language/"):
        return 3.0
    if lowered.startswith(("test/built-ins/", "test/intl/", "test/intl402/")):
        return 2.0
    if lowered.startswith(("test/harness/", "test/staging/")):
        return 1.5
    return 1.0


def _materialise_problem_groups(
    results: list[dict[str, Any]],
    incomplete: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    groups: dict[str, dict[str, Any]] = {}

    def add(
        key: str,
        kind: str,
        label: str,
        path: str | None = None,
        features: Iterable[str] = (),
        shard_index: int | None = None,
    ) -> None:
        group = groups.setdefault(
            key,
            {
                "key": key,
                "kind": kind,
                "label": label,
                "count": 0,
                "_paths": set(),
                "_features": Counter(),
                "_shards": set(),
            },
        )
        group["count"] += 1
        if path:
            group["_paths"].add(path)
        group["_features"].update(features)
        if shard_index is not None:
            group["_shards"].add(shard_index)

    for result in results:
        status = result["status"]
        if status == "failed":
            kind, key, label = _failure_descriptor(result)
            add(key, kind, label, str(result["path"]), _features(result))
        elif status == "timedOut":
            add(
                "timeout",
                "Timeout",
                "Timed out",
                str(result["path"]),
                _features(result),
            )

    for status in incomplete:
        reason = str(status.get("failureReason") or "IncompleteShard")
        detail = _clean_line(status.get("failureDetail"))
        label = f"Incomplete shard: {reason}"
        if detail:
            label += f" — {detail}"
        add(
            f"incomplete|{reason.lower()}|{detail.lower()}",
            "IncompleteShard",
            label,
            shard_index=int(status["shardIndex"]),
        )

    materialised: list[dict[str, Any]] = []
    for group in groups.values():
        paths = sorted(group.pop("_paths"))
        feature_counter: Counter[str] = group.pop("_features")
        shards = sorted(group.pop("_shards"))
        group["paths"] = paths
        group["pathSamples"] = paths[:PATH_SAMPLE_LIMIT]
        group["features"] = [
            {"feature": feature, "count": count}
            for feature, count in sorted(
                feature_counter.items(),
                key=lambda item: (-item[1], item[0].lower(), item[0]),
            )
        ]
        group["shardIndexes"] = shards
        materialised.append(group)

    kind_tiebreak = {
        "IncompleteShard": 0,
        "Crash": 1,
        "Timeout": 2,
        "NoOutput": 3,
        "Failure": 4,
    }
    return sorted(
        materialised,
        key=lambda group: (
            -int(group["count"]),
            kind_tiebreak.get(str(group["kind"]), 99),
            str(group["label"]).lower(),
            str(group["key"]),
        ),
    )


def _rank_biggest_problems(
    groups: list[dict[str, Any]],
    incomplete: list[dict[str, Any]],
    limit: int,
) -> list[dict[str, Any]]:
    candidates: list[dict[str, Any]] = []
    if incomplete:
        indexes = [int(status["shardIndex"]) for status in incomplete]
        candidates.append(
            {
                "kind": "IncompleteShards",
                "title": (
                    f"{len(indexes)} incomplete shard(s): "
                    + ", ".join(str(index) for index in indexes)
                ),
                "count": len(indexes),
                "impactScore": float(len(indexes)),
                "paths": [],
                "pathSamples": [],
                "features": [],
                "shardIndexes": indexes,
                "details": [str(status["reason"]) for status in incomplete],
            }
        )

    for group in groups:
        # Timeouts have their own size-ranked report. Keeping them out of this
        # list prevents the same symptom from opening both severity and timeout
        # issues when a run has no other failures.
        if group["kind"] in ("IncompleteShard", "Timeout"):
            continue
        paths = list(group["paths"])
        breadth = len({_path_bucket(path) for path in paths})
        impact = sum(_path_weight(path) for path in paths) * max(1, breadth)
        candidate_kind = str(group["kind"])
        candidates.append(
            {
                "kind": candidate_kind,
                "title": str(group["label"]),
                "count": int(group["count"]),
                "impactScore": round(impact, 2),
                "paths": paths,
                "pathSamples": list(group["pathSamples"]),
                "features": list(group["features"]),
                "shardIndexes": [],
                "details": [],
            }
        )

    candidates.sort(
        key=lambda problem: (
            -_BIGGEST_KIND_RANK.get(str(problem["kind"]), 0),
            -float(problem["impactScore"]),
            -int(problem["count"]),
            str(problem["title"]).lower(),
        )
    )
    return candidates[:limit]


def _rank_timeouts(
    results: list[dict[str, Any]], limit: int
) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    timeouts: list[dict[str, Any]] = []
    feature_paths: dict[str, set[str]] = defaultdict(set)
    for result in results:
        if result["status"] != "timedOut":
            continue
        path = str(result["path"])
        features = _features(result)
        for feature in features:
            feature_paths[feature].add(path)
        timeouts.append(
            {
                "path": path,
                "relativeTestPath": path,
                "sourceSizeBytes": _source_size(result),
                "features": features,
                "reason": str(result.get("reason") or ""),
            }
        )

    timeouts.sort(
        key=lambda timeout: (
            timeout["sourceSizeBytes"] is None,
            timeout["sourceSizeBytes"]
            if timeout["sourceSizeBytes"] is not None
            else 0,
            timeout["path"],
        )
    )
    feature_groups = [
        {
            "feature": feature,
            "count": len(paths),
            "pathSamples": sorted(paths)[:PATH_SAMPLE_LIMIT],
        }
        for feature, paths in sorted(
            feature_paths.items(),
            key=lambda item: (-len(item[1]), item[0].lower(), item[0]),
        )
    ]
    return timeouts[:limit], feature_groups


def merge(
    shard_dir: Path,
    phase: str = "full",
    expected_shard_indexes: set[int] | None = None,
    problem_limit: int = DEFAULT_PROBLEM_LIMIT,
    biggest_problem_limit: int = DEFAULT_BIGGEST_PROBLEM_LIMIT,
    timeout_limit: int = DEFAULT_TIMEOUT_LIMIT,
    broiler_commit: str = "",
    run_url: str = "",
    artifact_name: str = "test262-merged",
) -> dict[str, Any]:
    """Merge the selected phase into a deterministic report."""
    if problem_limit < 1:
        raise ValueError("problem_limit must be positive")
    if biggest_problem_limit < 1:
        raise ValueError("biggest_problem_limit must be positive")
    if timeout_limit < 1:
        raise ValueError("timeout_limit must be positive")

    reports, incomplete, retried, reported = _selected_attempts(
        Path(shard_dir), phase, expected_shard_indexes
    )
    results = _deduplicate_results(reports)

    status_paths: dict[str, list[str]] = {
        status: [
            str(result["path"]) for result in results if result["status"] == status
        ]
        for status in ("passed", "failed", "skipped", "timedOut")
    }
    executed_paths = sorted(
        status_paths["passed"] + status_paths["failed"] + status_paths["timedOut"]
    )
    summary = {
        "total": len(results),
        "executed": len(executed_paths),
        "passed": len(status_paths["passed"]),
        "failed": len(status_paths["failed"]),
        "skipped": len(status_paths["skipped"]),
        "timedOut": len(status_paths["timedOut"]),
    }

    declared_selection_counts: list[int] = []
    every_report_declares_selection = bool(reports)
    for report in reports:
        value = report.payload.get("selectedCountBeforeSharding")
        if value is None:
            every_report_declares_selection = False
            continue
        declared_selection_counts.append(int(value))

    run_configuration, configuration_failures = _aggregate_run_configuration(
        reports
    )
    if every_report_declares_selection and max(declared_selection_counts) == 0:
        configuration_failures.append(
            {
                "kind": "EmptySelection",
                "message": (
                    "The requested assembly/subset/feature selection matched no "
                    "runnable test262 paths before sharding."
                ),
            }
        )
    elif summary["total"] > 0 and summary["executed"] == 0:
        configuration_failures.append(
            {
                "kind": "NoExecutedTests",
                "message": (
                    "The selected paths were all skipped; the run produced no "
                    "executed test262 evidence."
                ),
            }
        )

    configured_shard_count = run_configuration.get("shardCount")
    configured_selection_count = run_configuration.get(
        "selectedCountBeforeSharding"
    )
    covers_full_shard_space = (
        isinstance(configured_shard_count, int)
        and expected_shard_indexes
        == set(range(configured_shard_count))
    )
    if (
        not incomplete
        and covers_full_shard_space
        and isinstance(configured_selection_count, int)
        and configured_selection_count > 0
        and summary["total"] != configured_selection_count
    ):
        configuration_failures.append(
            {
                "kind": "IncompleteSelectionCoverage",
                "message": (
                    f"All {configured_shard_count} shards reported, but their "
                    f"{summary['total']} unique results do not cover the "
                    f"{configured_selection_count} paths selected before sharding."
                ),
            }
        )

    all_groups = _materialise_problem_groups(results, incomplete)
    biggest = _rank_biggest_problems(
        all_groups, incomplete, biggest_problem_limit
    )
    timeouts, timeout_features = _rank_timeouts(results, timeout_limit)
    suite_refs = sorted(
        {
            str(report.payload.get("suiteRef"))
            for report in reports
            if report.payload.get("suiteRef")
        }
    )

    return {
        "phase": phase,
        "suiteRef": suite_refs[0] if len(suite_refs) == 1 else "",
        "suiteRefs": suite_refs,
        "broilerCommit": broiler_commit,
        "runUrl": run_url,
        "artifactName": artifact_name,
        "runConfiguration": run_configuration,
        "shardCount": len(reports),
        "expectedShards": (
            sorted(expected_shard_indexes)
            if expected_shard_indexes is not None
            else sorted(
                set(reported)
                | {int(item["shardIndex"]) for item in incomplete}
            )
        ),
        "reportedShards": reported,
        "retriedShards": retried,
        "incompleteShards": incomplete,
        "configurationFailures": configuration_failures,
        "selectedCountBeforeSharding": (
            max(declared_selection_counts) if declared_selection_counts else None
        ),
        "summary": summary,
        "passedPaths": status_paths["passed"],
        "failedPaths": status_paths["failed"],
        "skippedPaths": status_paths["skipped"],
        "timedOutPaths": status_paths["timedOut"],
        "executedPaths": executed_paths,
        "results": results,
        "problemLimit": problem_limit,
        "problemGroupCount": len(all_groups),
        "problemGroups": all_groups[:problem_limit],
        "biggestProblemLimit": biggest_problem_limit,
        "biggestProblems": biggest,
        "timeoutLimit": timeout_limit,
        "timeoutCount": summary["timedOut"],
        "timeouts": timeouts,
        "timeoutFeatureGroups": timeout_features,
    }


def load_merged_report(path: Path) -> dict[str, Any]:
    """Load a canonical merged artifact for scope-safe manifest persistence."""
    path = Path(path)
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise ValueError(
            f"merged report is invalid JSON at line {exc.lineno}, column {exc.colno}"
        ) from exc
    except OSError as exc:
        raise ValueError(f"could not read merged report: {exc}") from exc
    if not isinstance(payload, dict):
        raise ValueError("merged report is not a JSON object")
    required_lists = (
        "executedPaths",
        "failedPaths",
        "timedOutPaths",
        "incompleteShards",
        "results",
    )
    for key in required_lists:
        if not isinstance(payload.get(key), list):
            raise ValueError(f"merged report has no {key} array")
    if not isinstance(payload.get("summary"), dict):
        raise ValueError("merged report has no summary object")
    if not str(payload.get("phase") or "").strip():
        raise ValueError("merged report has no phase")
    return payload


def _read_text_manifest(path: Path) -> tuple[list[str], set[str]]:
    if not path.is_file():
        return [], set()
    comments: list[str] = []
    paths: set[str] = set()
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line:
            continue
        if line.startswith("#"):
            comments.append(raw_line.rstrip())
            continue
        normalised = _normalise_path(line)
        if normalised:
            paths.add(normalised)
    return comments, paths


def merge_into_manifest(
    merged: dict[str, Any], manifest_path: Path
) -> dict[str, Any]:
    """Update a text failure manifest using only conclusively executed paths."""
    manifest_path = Path(manifest_path)
    comments, old_paths = _read_text_manifest(manifest_path)
    executed = set(merged.get("executedPaths") or [])
    current_failures = set(merged.get("failedPaths") or []) | set(
        merged.get("timedOutPaths") or []
    )
    manifest_paths = sorted((old_paths - executed) | current_failures)

    lines = list(comments)
    if lines and manifest_paths:
        lines.append("")
    lines.extend(manifest_paths)
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(
        "\n".join(lines) + ("\n" if lines else ""),
        encoding="utf-8",
    )

    result = dict(merged)
    result["manifestPaths"] = manifest_paths
    return result


def _inline(value: object) -> str:
    return " ".join(str(value or "").replace(chr(96), "'").split())


def _metadata_lines(run_url: str | None, artifact_name: str) -> list[str]:
    return [
        "",
        "### CI metadata",
        f"- Workflow run: {run_url}" if run_url else "- Workflow run: (unknown)",
        f"- Artifact: {_inline(artifact_name)}",
        "",
        "_Auto-generated from the structured test262 shard reports._",
    ]


def render_issue_markdown(
    merged: dict[str, Any],
    run_url: str | None = None,
    artifact_name: str = "test262-merged",
) -> str:
    summary = merged["summary"]
    groups = merged.get("problemGroups") or []
    lines = [
        f"## test262 {merged['phase']} run — most common problems",
        "",
        f"- Total results: {summary['total']}",
        f"- Passed: {summary['passed']}",
        f"- Failed: {summary['failed']}",
        f"- Timed out: {summary['timedOut']}",
        f"- Skipped: {summary['skipped']}",
        f"- Incomplete shards: {len(merged['incompleteShards'])}",
        "",
    ]
    if merged.get("configurationFailures"):
        lines.extend(["### Configuration failures", ""])
        for failure in merged["configurationFailures"]:
            lines.append(
                f"- **{_inline(failure['kind'])}** — "
                f"{_inline(failure['message'])}"
            )
        lines.append("")
    lines.extend(
        [
            f"### Top {merged['problemLimit']} normalized failure groups",
            "",
        ]
    )
    if not groups:
        lines.append("- None")
    for index, group in enumerate(groups, start=1):
        lines.append(
            f"{index}. **{_inline(group['label'])}** — "
            f"{group['count']} occurrence(s)"
        )
        if group["pathSamples"]:
            samples = ", ".join(_inline(path) for path in group["pathSamples"])
            lines.append(f"   - Path samples: {samples}")
        if group["features"]:
            features = ", ".join(
                f"{_inline(item['feature'])} ({item['count']})"
                for item in group["features"][:PATH_SAMPLE_LIMIT]
            )
            lines.append(f"   - Features: {features}")
        if group["shardIndexes"]:
            shards = ", ".join(str(index) for index in group["shardIndexes"])
            lines.append(f"   - Shards: {shards}")

    # Keep every infrastructure gap actionable even when the frequency limit
    # is filled by larger test-failure groups.
    if merged["incompleteShards"]:
        lines.extend(["", "### Incomplete shards", ""])
        for status in merged["incompleteShards"]:
            lines.append(
                f"- Shard {status['shardIndex']}: "
                f"{_inline(status['reason'])}"
            )
    lines.extend(_metadata_lines(run_url, artifact_name))
    return "\n".join(lines) + "\n"


def render_biggest_problems_markdown(
    merged: dict[str, Any],
    run_url: str | None = None,
    artifact_name: str = "test262-merged",
) -> str:
    problems = merged.get("biggestProblems") or []
    lines = [
        f"## test262 {merged['phase']} run — biggest problems",
        "",
        "_Ranked by severity first, then weighted test-area impact and path breadth._",
        "",
        f"### Top {merged['biggestProblemLimit']} severity/impact groups",
        "",
    ]
    if not problems:
        lines.append("- None")
    for index, problem in enumerate(problems, start=1):
        lines.append(
            f"{index}. **{_inline(problem['title'])}** "
            f"({problem['kind']}, impact {problem['impactScore']:g})"
        )
        if problem["details"]:
            for detail in problem["details"][:PATH_SAMPLE_LIMIT]:
                lines.append(f"   - {_inline(detail)}")
        if problem["pathSamples"]:
            samples = ", ".join(
                _inline(path) for path in problem["pathSamples"]
            )
            lines.append(f"   - Path samples: {samples}")
        if problem["features"]:
            features = ", ".join(
                f"{_inline(item['feature'])} ({item['count']})"
                for item in problem["features"][:PATH_SAMPLE_LIMIT]
            )
            lines.append(f"   - Features: {features}")
    lines.extend(_metadata_lines(run_url, artifact_name))
    return "\n".join(lines) + "\n"


def _format_size(value: int | None) -> str:
    if value is None:
        return "unknown size"
    if value < 1024:
        return f"{value} B"
    return f"{value / 1024:.1f} KiB"


def render_timeout_issue_markdown(
    merged: dict[str, Any],
    run_url: str | None = None,
    artifact_name: str = "test262-merged",
) -> str:
    timeouts = merged.get("timeouts") or []
    lines = [
        f"## test262 {merged['phase']} run — {merged['timeoutCount']} timeout(s)",
        "",
        "_Small source files rank first because they are the strongest signal of a hang._",
        "",
        f"### First {merged['timeoutLimit']} timeouts by source size",
        "",
    ]
    if not timeouts:
        lines.append("- None")
    for index, timeout in enumerate(timeouts, start=1):
        features = ", ".join(timeout["features"]) or "(none reported)"
        lines.append(
            f"{index}. **{_format_size(timeout['sourceSizeBytes'])}** — "
            f"{_inline(timeout['path'])}"
        )
        lines.append(f"   - Features: {_inline(features)}")

    feature_groups = merged.get("timeoutFeatureGroups") or []
    if feature_groups:
        lines.extend(["", "### Timeout feature clusters", ""])
        for group in feature_groups:
            samples = ", ".join(_inline(path) for path in group["pathSamples"])
            lines.append(
                f"- **{_inline(group['feature'])}** — "
                f"{group['count']} timeout(s); path samples: {samples}"
            )
    lines.extend(_metadata_lines(run_url, artifact_name))
    return "\n".join(lines) + "\n"


def _parse_expected_shards(
    parser: argparse.ArgumentParser, value: str | None
) -> set[int] | None:
    if value is None:
        return None
    tokens = [token.strip() for token in value.split(",") if token.strip()]
    if not tokens:
        parser.error("--expected-shards must contain at least one shard index")
    try:
        indexes = {int(token) for token in tokens}
    except ValueError:
        parser.error(
            "--expected-shards must be a comma-separated list of integers"
        )
    if any(index < 0 for index in indexes):
        parser.error("--expected-shards indexes must be non-negative")
    return indexes


def _write_github_outputs(path: Path, merged: dict[str, Any]) -> None:
    summary = merged["summary"]
    incomplete_indexes = [
        int(status["shardIndex"]) for status in merged["incompleteShards"]
    ]
    biggest_count = len(merged.get("biggestProblems") or [])
    timeout_count = int(merged.get("timeoutCount") or 0)
    configuration_failure_count = len(merged.get("configurationFailures") or [])
    has_failures = (
        summary["failed"] > 0
        or timeout_count > 0
        or bool(incomplete_indexes)
        or configuration_failure_count > 0
    )
    matrix = json.dumps(
        [{"shard-index": index} for index in incomplete_indexes],
        separators=(",", ":"),
    )
    outputs = {
        # WPT-style failed_count includes timeouts; timeout_count remains
        # available separately for the dedicated timeout issue.
        "failed_count": summary["failed"] + summary["timedOut"],
        "passed_count": summary["passed"],
        "skipped_count": summary["skipped"],
        "timed_out_count": summary["timedOut"],
        "total_count": summary["total"],
        "incomplete_shard_count": len(incomplete_indexes),
        "create_issue": "true" if has_failures else "false",
        "biggest_problem_count": biggest_count,
        "create_biggest_issue": "true" if biggest_count else "false",
        "timeout_count": timeout_count,
        "create_timeout_issue": "true" if timeout_count else "false",
        "configuration_failure_count": configuration_failure_count,
        "incomplete_shard_indexes": ",".join(
            str(index) for index in incomplete_indexes
        ),
        "incomplete_shard_matrix": matrix,
        "has_incomplete_shards": "true" if incomplete_indexes else "false",
        "retried_shard_count": len(merged.get("retriedShards") or []),
        "suite_passed": "false" if has_failures else "true",
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as handle:
        for key, value in outputs.items():
            handle.write(f"{key}={value}\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--shard-dir", type=Path)
    source.add_argument(
        "--merged-input",
        type=Path,
        help="Canonical merged JSON to reuse for manifest persistence",
    )
    parser.add_argument("--phase", default="full")
    parser.add_argument("--expected-shards")
    parser.add_argument("--merged-json", type=Path)
    parser.add_argument(
        "--merge-into",
        type=Path,
        help="Text failure manifest to update in place",
    )
    parser.add_argument("--issue-md", type=Path)
    parser.add_argument("--biggest-issue-md", type=Path)
    parser.add_argument("--timeout-issue-md", type=Path)
    parser.add_argument(
        "--problem-limit", type=int, default=DEFAULT_PROBLEM_LIMIT
    )
    parser.add_argument(
        "--biggest-problem-limit",
        type=int,
        default=DEFAULT_BIGGEST_PROBLEM_LIMIT,
    )
    parser.add_argument(
        "--timeout-limit", type=int, default=DEFAULT_TIMEOUT_LIMIT
    )
    parser.add_argument(
        "--run-url",
        default=os.environ.get("TEST262_RUN_URL"),
    )
    parser.add_argument(
        "--broiler-commit",
        default=os.environ.get("BROILER_COMMIT", ""),
    )
    parser.add_argument("--artifact-name", default="test262-merged")
    parser.add_argument("--github-output", type=Path)
    args = parser.parse_args(argv)

    if not args.phase.strip():
        parser.error("--phase must not be empty")
    if args.problem_limit < 1:
        parser.error("--problem-limit must be a positive integer")
    if args.biggest_problem_limit < 1:
        parser.error("--biggest-problem-limit must be a positive integer")
    if args.timeout_limit < 1:
        parser.error("--timeout-limit must be a positive integer")

    if args.merged_input:
        if args.expected_shards is not None:
            parser.error("--expected-shards cannot be used with --merged-input")
        try:
            merged = load_merged_report(args.merged_input)
        except ValueError as exc:
            parser.error(str(exc))
    else:
        expected = _parse_expected_shards(parser, args.expected_shards)
        merged = merge(
            args.shard_dir,
            phase=args.phase,
            expected_shard_indexes=expected,
            problem_limit=args.problem_limit,
            biggest_problem_limit=args.biggest_problem_limit,
            timeout_limit=args.timeout_limit,
            broiler_commit=args.broiler_commit,
            run_url=args.run_url or "",
            artifact_name=args.artifact_name,
        )
    if args.merge_into:
        merged = merge_into_manifest(merged, args.merge_into)

    outputs = (
        (
            args.merged_json,
            json.dumps(merged, indent=2, sort_keys=True) + "\n",
        ),
        (
            args.issue_md,
            render_issue_markdown(
                merged, args.run_url, args.artifact_name
            ),
        ),
        (
            args.biggest_issue_md,
            render_biggest_problems_markdown(
                merged, args.run_url, args.artifact_name
            ),
        ),
        (
            args.timeout_issue_md,
            render_timeout_issue_markdown(
                merged, args.run_url, args.artifact_name
            ),
        ),
    )
    for path, content in outputs:
        if path is None:
            continue
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")

    if args.github_output:
        _write_github_outputs(args.github_output, merged)

    summary = merged["summary"]
    print(
        f"Merged {merged['shardCount']} test262 {merged['phase']} shard(s): "
        f"{summary['passed']} passed, {summary['failed']} failed, "
        f"{summary['timedOut']} timed out, {summary['skipped']} skipped; "
        f"{len(merged['incompleteShards'])} incomplete shard(s)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
