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
import math
import os
import re
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Callable, Iterable


DEFAULT_PROBLEM_LIMIT = 10
DEFAULT_BIGGEST_PROBLEM_LIMIT = 3
DEFAULT_TIMEOUT_LIMIT = 10
DEFAULT_MINIFIER_ONLY_LIMIT = 10
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
    "minifier",
    "minifierVersion",
    "minifierProfile",
    "minifierTimeoutSeconds",
    "minifierOptions",
    "variants",
    "expectedVariantCountPerPath",
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
_ORIGINAL_VARIANT = "original"
# Every minified variant a report may carry. A single run configures exactly ONE of them —
# a report is ["original"] or ["original", <one minifier>] — but the merger must recognise
# each, because which one a shard ran is a run-configuration field it validates rather than
# something it may assume.
_MINIFIED_VARIANTS = ("terser", "closure")
_VARIANTS = (_ORIGINAL_VARIANT, *_MINIFIED_VARIANTS)
_VARIANT_RANK = {variant: index for index, variant in enumerate(_VARIANTS)}
_MINIFIER_PROFILES = {
    "terser": "test262-safe-mangle-v2",
    "closure": "test262-closure-advanced-v1",
}
_MINIFIER_NOT_APPLICABLE = "minifier-not-applicable"
# The source could not be minified at all because the minifier's own parser rejected it
# (an escaped keyword used as an IdentifierName, sloppy-mode `let`, an Annex B assignment
# target, decorators, import attributes with a trailing comma — all of which the engine
# runs). There is no minified variant to execute, so the case is not applicable rather
# than a failure; the kind is kept distinct from `minifier-not-applicable` so a report can
# still tell "the minifier could not read it" apart from "this test cannot be minified
# before execution".
_MINIFIER_UNSUPPORTED_SYNTAX = "minifier-unsupported-syntax"
# The minifier accepted the source and then crashed compiling it: Closure walks
# `for (x.y of [23])` into a NullPointerException and asserts on the `import(specifier)` a
# script may contain. Nothing came out, so the case is not applicable for the same reason a
# declined parse is; the kind stays distinct because the two say different things about the
# minifier. A crash the worker could not tie to the test's own source is still an
# infrastructure failure and never reaches here.
_MINIFIER_INTERNAL_ERROR = "minifier-internal-error"
# The two verdicts a reference engine can return about a FAILING minified case (see
# run_test262.ReferenceEngine): it could not parse what the minifier emitted, or it ran the
# body and failed the same way the engine did. Either way the minified program is not
# evidence about the engine, so the case is not applicable rather than a failure. Kept
# apart from each other and from the kinds above because they answer different questions —
# "the minifier could not read it", "this test cannot be minified at all", "the minifier
# wrote something invalid", "the minifier changed what the test measures".
_MINIFIER_INVALID_OUTPUT = "minifier-invalid-output"
_MINIFIER_CHANGED_SEMANTICS = "minifier-changed-semantics"
_MINIFIER_SKIP_KINDS = frozenset(
    {
        _MINIFIER_NOT_APPLICABLE,
        _MINIFIER_UNSUPPORTED_SYNTAX,
        _MINIFIER_INTERNAL_ERROR,
        _MINIFIER_INVALID_OUTPUT,
        _MINIFIER_CHANGED_SEMANTICS,
    }
)
# The two verdicts a case that KEEPS failing carries out of the cross-check (see
# run_test262.cross_check_minified_failure). `engine-divergence` is the reference engine
# passing both variants — the transformation preserved the test, so the failure is this
# engine's, and the case is worth a reduction. `inconclusive` is the reference engine
# failing the ORIGINAL too, which is no opinion at all: the case is reported against the
# engine because that is the safe direction, not because anything established it is a
# defect. A report that adds the two together invites a triage to start on the wrong end.
_ENGINE_DIVERGENCE = "engine-divergence"
_CROSS_CHECK_INCONCLUSIVE = "inconclusive"
# The exact transformation each profile stands for, mirrored from
# run_test262.MINIFIER_OPTIONS. A report that records anything else is not the run this
# merger can describe, so it is a configuration failure rather than a silent relabelling.
_MINIFIER_OPTIONS = {
    "terser": {
        "compress": False,
        "mangle": True,
        "mangleProperties": False,
        "mangleEval": False,
        # Identifiers the test itself quotes are held back from mangling: see
        # run_test262.TERSER_PROFILE.
        "reserveQuotedNames": True,
        "toplevel": False,
        "module": False,
        "keepFunctionNames": True,
        "keepClassNames": True,
        "asciiOnly": False,
        "comments": False,
        "semicolons": True,
    },
    "closure": {
        "compilationLevel": "ADVANCED_OPTIMIZATIONS",
        "languageIn": "ECMASCRIPT_NEXT",
        "languageOut": "ECMASCRIPT_NEXT",
        "ecmaScriptYear": 2026,
        "mangle": True,
        # ADVANCED renames properties too, which is the whole difference from the Terser
        # profile and the reason a Closure run reports far more cases the transformation
        # moved out of being the test.
        "mangleProperties": True,
        "harnessExterns": True,
        # The engine's own globals are externs as well, generated per run from what the
        # engine says it implements: see run_test262.collect_host_property_names.
        "hostExterns": True,
        "reserveQuotedNames": True,
        "emitUseStrict": False,
        "isolationMode": "NONE",
        "processCommonJsModules": False,
        "rewritePolyfills": False,
        "warningLevel": "QUIET",
    },
}
_BIGGEST_KIND_RANK = {
    "IncompleteShards": 5,
    "Crash": 4,
    "MinifierFailure": 3,
    "NoOutput": 2,
    "Failure": 1,
}
_PROBLEM_KIND_TIEBREAK = {
    "IncompleteShard": 0,
    "Crash": 1,
    "MinifierFailure": 2,
    "Timeout": 3,
    "NoOutput": 4,
    "Failure": 5,
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


def _canonical_variant(value: object) -> str | None:
    return value if isinstance(value, str) and value in _VARIANTS else None


def _result_identity(result: dict[str, Any]) -> tuple[str, str]:
    return str(result["path"]), str(result["variant"])


def _case_reference(result: dict[str, Any]) -> dict[str, str]:
    path, variant = _result_identity(result)
    return {"path": path, "variant": variant}


def _is_not_applicable(result: dict[str, Any]) -> bool:
    return result.get("notApplicable") is True


def _valid_variant_shapes() -> tuple[tuple[str, ...], ...]:
    """The variant lists one run may declare: originals alone, or plus one minifier."""
    return (
        (_ORIGINAL_VARIANT,),
        *((_ORIGINAL_VARIANT, variant) for variant in _MINIFIED_VARIANTS),
    )


def _expected_report_variants(
    payload: dict[str, Any], observed: set[str]
) -> tuple[tuple[str, ...] | None, str | None]:
    raw_minifier = payload.get("minifier")
    minifier: str | None = None
    if raw_minifier is not None:
        minifier = str(raw_minifier).strip().lower()
        if minifier not in ("none", *_MINIFIED_VARIANTS):
            return None, (
                "report minifier must be 'none' or one of "
                f"{', '.join(repr(name) for name in _MINIFIED_VARIANTS)}"
            )

    declared: tuple[str, ...] | None = None
    if "variants" in payload:
        raw_variants = payload["variants"]
        if not isinstance(raw_variants, list) or not raw_variants:
            return None, "report variants is not a non-empty array"
        canonical = [_canonical_variant(value) for value in raw_variants]
        if any(value is None for value in canonical):
            return None, "report variants contains an unknown variant"
        declared = tuple(str(value) for value in canonical)
        if len(set(declared)) != len(declared):
            return None, "report variants contains duplicate entries"
        if declared not in _valid_variant_shapes():
            return None, (
                "report variants must be ['original'] or ['original', <minifier>]"
            )

    configured = (
        (_ORIGINAL_VARIANT, minifier)
        if minifier in _MINIFIED_VARIANTS
        else ((_ORIGINAL_VARIANT,) if minifier == "none" else None)
    )
    if configured is not None and declared is not None and configured != declared:
        return None, "report minifier and variants settings disagree"

    expected = configured or declared
    if expected is None:
        # Legacy reports omit both fields. Infer their shape, while still
        # requiring consistent variant coverage for every base path.
        expected = tuple(
            sorted(observed or {"original"}, key=lambda value: _VARIANT_RANK[value])
        )
    if _ORIGINAL_VARIANT not in expected:
        return None, "report variants does not include the required original case"
    if len(expected) > 2:
        return None, "report variants declares more than one minified variant"
    return expected, None


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
    identities: set[tuple[str, str]] = set()
    variants_by_path: dict[str, set[str]] = defaultdict(set)
    observed_variants: set[str] = set()
    status_counts: Counter[str] = Counter()
    not_applicable_count = 0
    for position, result in enumerate(results):
        if not isinstance(result, dict):
            return f"result {position} is not a JSON object"
        path = _normalise_path(result.get("path"))
        if not path:
            return f"result {position} has no path"
        status = _canonical_status(result.get("status"))
        if status is None:
            return f"result {position} has unknown status {result.get('status')!r}"
        # Pre-minifier reports have no variant field. They are original-source
        # evidence and remain valid inputs after the schema extension. An
        # explicitly empty/null/unknown variant is still malformed.
        variant = (
            "original"
            if "variant" not in result
            else _canonical_variant(result.get("variant"))
        )
        if variant is None:
            return f"result {position} has unknown variant {result.get('variant')!r}"
        identity = (path, variant)
        if identity in identities:
            return (
                f"report contains duplicate result for {path!r} "
                f"variant {variant!r}"
            )
        identities.add(identity)
        result_paths.append(path)
        variants_by_path[path].add(variant)
        observed_variants.add(variant)
        status_counts[status] += 1

        if "infrastructure" in result and not isinstance(
            result["infrastructure"], bool
        ):
            return f"result {position} infrastructure is not a boolean"
        if result.get("infrastructure") is True and status != "failed":
            return f"result {position} marks a non-failed case as infrastructure"

        failure_type = str(result.get("failureType") or "").strip()
        if failure_type in ("MinifierError", "MinifierTimeout") and (
            variant not in _MINIFIED_VARIANTS
            or status != "failed"
            or result.get("infrastructure") is not True
        ):
            return (
                f"result {position} {failure_type} must be an infrastructure "
                "failure for a minified variant"
            )

        if "notApplicable" in result and not isinstance(
            result["notApplicable"], bool
        ):
            return f"result {position} notApplicable is not a boolean"
        not_applicable = result.get("notApplicable") is True
        has_not_applicable_kind = result.get("skipKind") in _MINIFIER_SKIP_KINDS
        if not_applicable:
            if status != "skipped":
                return f"result {position} marks a non-skipped case notApplicable"
            if variant not in _MINIFIED_VARIANTS:
                return (
                    f"result {position} marks a case that is not a minified "
                    "variant notApplicable"
                )
            if not has_not_applicable_kind:
                return (
                    f"result {position} notApplicable case has no "
                    f"minifier skipKind (expected one of "
                    f"{', '.join(sorted(_MINIFIER_SKIP_KINDS))})"
                )
            if not str(result.get("reason") or "").strip():
                return f"result {position} notApplicable case has no reason"
            not_applicable_count += 1
        elif has_not_applicable_kind:
            return (
                f"result {position} has {result.get('skipKind')!r} skipKind "
                "without notApplicable=true"
            )

    expanded_paths = payload.get("expandedPaths")
    if expanded_paths is not None:
        if not isinstance(expanded_paths, list) or any(
            not _normalise_path(path) for path in expanded_paths
        ):
            return "expandedPaths is not an array of paths"
        expected_paths = sorted({_normalise_path(path) for path in expanded_paths})
        if len(expected_paths) != len(expanded_paths):
            return "expandedPaths contains duplicate base paths"
        if sorted(set(result_paths)) != expected_paths:
            return "results do not cover every expanded path"

    expected_variants, variant_error = _expected_report_variants(
        payload, observed_variants
    )
    if variant_error:
        return variant_error
    assert expected_variants is not None
    expected_variant_set = set(expected_variants)

    if "expectedVariantCountPerPath" in payload:
        raw_expected_count = payload["expectedVariantCountPerPath"]
        if isinstance(raw_expected_count, bool) or not isinstance(
            raw_expected_count, int
        ):
            return "report expectedVariantCountPerPath is not an integer"
        if raw_expected_count != len(expected_variants):
            return (
                "report expectedVariantCountPerPath does not match its "
                "configured variants"
            )

    for path in sorted(variants_by_path):
        actual = variants_by_path[path]
        if actual != expected_variant_set:
            missing = sorted(expected_variant_set - actual)
            unexpected = sorted(actual - expected_variant_set)
            details: list[str] = []
            if missing:
                details.append("missing " + ", ".join(missing))
            if unexpected:
                details.append("unexpected " + ", ".join(unexpected))
            return (
                f"result variants for {path!r} are incomplete "
                f"({'; '.join(details)})"
            )

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
        "notApplicable": not_applicable_count,
        "total": len(results),
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

    variant_summary = payload.get("variantSummary")
    if variant_summary is not None:
        if not isinstance(variant_summary, dict):
            return "report variantSummary is not an object"
        if set(variant_summary) != expected_variant_set:
            return "report variantSummary does not match configured variants"
        for variant in expected_variants:
            declared_summary = variant_summary.get(variant)
            if not isinstance(declared_summary, dict):
                return f"report variantSummary.{variant} is not an object"
            variant_results = [
                result
                for result in results
                if _canonical_variant(result.get("variant", "original")) == variant
            ]
            variant_counts = Counter(
                _canonical_status(result.get("status"))
                for result in variant_results
            )
            actual_summary = {
                "total": len(variant_results),
                "executed": (
                    variant_counts["passed"]
                    + variant_counts["failed"]
                    + variant_counts["timedOut"]
                ),
                "passed": variant_counts["passed"],
                "failed": variant_counts["failed"],
                "skipped": variant_counts["skipped"],
                "timedOut": variant_counts["timedOut"],
                "notApplicable": sum(
                    result.get("notApplicable") is True
                    for result in variant_results
                ),
            }
            for key, actual in actual_summary.items():
                raw_declared = declared_summary.get(key)
                if isinstance(raw_declared, bool) or not isinstance(
                    raw_declared, int
                ):
                    return (
                        f"report variantSummary.{variant}.{key} is not an integer"
                    )
                if raw_declared != actual:
                    return (
                        f"report variantSummary.{variant}.{key} count "
                        f"{raw_declared} does not match results ({actual})"
                    )

    path_summary = payload.get("pathSummary")
    if path_summary is not None:
        if not isinstance(path_summary, dict):
            return "report pathSummary is not an object"
        path_statuses: dict[str, str] = {}
        for path in variants_by_path:
            case_statuses = {
                _canonical_status(result.get("status"))
                for result in results
                if _normalise_path(result.get("path")) == path
            }
            if "timedOut" in case_statuses:
                path_statuses[path] = "timedOut"
            elif "failed" in case_statuses:
                path_statuses[path] = "failed"
            elif "passed" in case_statuses:
                path_statuses[path] = "passed"
            else:
                path_statuses[path] = "skipped"
        actual_path_summary = {
            "total": len(path_statuses),
            "executed": sum(
                status != "skipped" for status in path_statuses.values()
            ),
            "passed": sum(
                status == "passed" for status in path_statuses.values()
            ),
            "failed": sum(
                status == "failed" for status in path_statuses.values()
            ),
            "skipped": sum(
                status == "skipped" for status in path_statuses.values()
            ),
            "timedOut": sum(
                status == "timedOut" for status in path_statuses.values()
            ),
        }
        for key, actual in actual_path_summary.items():
            raw_declared = path_summary.get(key)
            if isinstance(raw_declared, bool) or not isinstance(raw_declared, int):
                return f"report pathSummary.{key} is not an integer"
            if raw_declared != actual:
                return (
                    f"report pathSummary.{key} count {raw_declared} does not "
                    f"match results ({actual})"
                )

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
    # The runner module is machine-local provenance, not portable run data.
    # Drop it defensively even if an older producer wrote it on a case row.
    normalised.pop("terserModule", None)
    normalised.pop("closureModule", None)
    normalised["path"] = _normalise_path(result.get("path"))
    normalised["status"] = _canonical_status(result.get("status"))
    normalised["variant"] = _canonical_variant(result.get("variant", "original"))
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
    by_identity: dict[tuple[str, str], dict[str, Any]] = {}
    for artifact in reports:
        for raw_result in artifact.payload["results"]:
            result = _normalise_result(raw_result)
            identity = _result_identity(result)
            previous = by_identity.get(identity)
            if previous is None or _result_preference(result) > _result_preference(previous):
                by_identity[identity] = result
    return [
        by_identity[identity]
        for identity in sorted(
            by_identity,
            key=lambda item: (item[0], _VARIANT_RANK[item[1]]),
        )
    ]


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
                    "field": field,
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


def _validate_minifier_configuration(
    configuration: dict[str, Any],
    reports: list[Artifact],
    results: list[dict[str, Any]],
) -> list[dict[str, str]]:
    """Validate merged minifier provenance without exposing module paths."""
    if not reports:
        return []

    failures: list[dict[str, str]] = []
    minifier = configuration.get("minifier")
    variants = configuration.get("variants")
    observed_variants = sorted(
        {str(result["variant"]) for result in results},
        key=lambda value: _VARIANT_RANK[value],
    )

    # Reports from before minifier support intentionally omit this metadata.
    # Once any minifier metadata is present, require the complete contract.
    has_minifier_metadata = any(
        field in configuration
        for field in (
            "minifier",
            "minifierVersion",
            "minifierProfile",
            "minifierTimeoutSeconds",
            "minifierOptions",
            "variants",
            "expectedVariantCountPerPath",
        )
    )
    if not has_minifier_metadata:
        minified_observed = [
            variant for variant in observed_variants if variant in _MINIFIED_VARIANTS
        ]
        if minified_observed:
            failures.append(
                {
                    "kind": "MissingMinifierConfiguration",
                    "message": (
                        f"{minified_observed[0]} result cases were emitted without "
                        "the required minifier provenance fields."
                    ),
                }
            )
        return failures

    if minifier not in ("none", *_MINIFIED_VARIANTS):
        failures.append(
            {
                "kind": "InvalidMinifierConfiguration",
                "message": (
                    "Run configuration minifier must be 'none' or one of "
                    f"{', '.join(repr(name) for name in _MINIFIED_VARIANTS)}."
                ),
            }
        )
        return failures

    expected_variants = (
        [_ORIGINAL_VARIANT, minifier]
        if minifier in _MINIFIED_VARIANTS
        else [_ORIGINAL_VARIANT]
    )
    if variants != expected_variants:
        failures.append(
            {
                "kind": "InvalidMinifierConfiguration",
                "message": (
                    f"Run configuration variants must be {expected_variants!r} "
                    f"when minifier={minifier}."
                ),
            }
        )
    if observed_variants and observed_variants != expected_variants:
        failures.append(
            {
                "kind": "IncompleteVariantCoverage",
                "message": (
                    f"Observed variants {observed_variants!r} do not match the "
                    f"configured variants {expected_variants!r}."
                ),
            }
        )

    expected_variant_count = configuration.get("expectedVariantCountPerPath")
    if (
        isinstance(expected_variant_count, bool)
        or not isinstance(expected_variant_count, int)
        or expected_variant_count != len(expected_variants)
    ):
        failures.append(
            {
                "kind": "InvalidMinifierConfiguration",
                "message": (
                    "Run configuration expectedVariantCountPerPath must match "
                    "the configured variants."
                ),
            }
        )

    timeout = configuration.get("minifierTimeoutSeconds")
    minifier_options = configuration.get("minifierOptions")
    if minifier in _MINIFIED_VARIANTS:
        label = minifier.capitalize()
        expected_profile = _MINIFIER_PROFILES[minifier]
        expected_options = _MINIFIER_OPTIONS[minifier]
        version = configuration.get("minifierVersion")
        if not isinstance(version, str) or not version.strip():
            failures.append(
                {
                    "kind": "InvalidMinifierConfiguration",
                    "message": f"{label} runs must record a non-empty minifierVersion.",
                }
            )
        if configuration.get("minifierProfile") != expected_profile:
            failures.append(
                {
                    "kind": "InvalidMinifierConfiguration",
                    "message": (
                        f"{label} runs must record minifierProfile={expected_profile}."
                    ),
                }
            )
        options_are_exact = (
            isinstance(minifier_options, dict)
            and set(minifier_options) == set(expected_options)
            and all(
                type(minifier_options[key]) is type(expected)
                and minifier_options[key] == expected
                for key, expected in expected_options.items()
            )
        )
        if not options_are_exact:
            failures.append(
                {
                    "kind": "InvalidMinifierConfiguration",
                    "message": (
                        f"{label} runs must record the exact "
                        f"{expected_profile} minifierOptions."
                    ),
                }
            )
        if (
            isinstance(timeout, bool)
            or not isinstance(timeout, (int, float))
            or not math.isfinite(float(timeout))
            or float(timeout) <= 0
        ):
            failures.append(
                {
                    "kind": "InvalidMinifierConfiguration",
                    "message": (
                        f"{label} runs must record a positive finite "
                        "minifierTimeoutSeconds value."
                    ),
                }
            )
    else:
        if not isinstance(configuration.get("minifierVersion"), str):
            failures.append(
                {
                    "kind": "InvalidMinifierConfiguration",
                    "message": "Non-minified runs must record minifierVersion as a string.",
                }
            )
        if configuration.get("minifierProfile") not in (None, ""):
            failures.append(
                {
                    "kind": "InvalidMinifierConfiguration",
                    "message": "Non-minified runs must not record a minifier profile.",
                }
            )
        if minifier_options != {}:
            failures.append(
                {
                    "kind": "InvalidMinifierConfiguration",
                    "message": "Non-minified runs must record empty minifierOptions.",
                }
            )
        if timeout not in (None, 0, 0.0):
            failures.append(
                {
                    "kind": "InvalidMinifierConfiguration",
                    "message": (
                        "Non-minified runs must record minifierTimeoutSeconds as 0 or null."
                    ),
                }
            )
    return failures


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
    failure_type = str(result.get("failureType") or "").strip()
    if failure_type in ("MinifierError", "MinifierTimeout"):
        detail = reason or "requested minified evidence was not produced"
        label = f"Minifier failure ({failure_type}): {detail}"
        return (
            "MinifierFailure",
            f"minifier-failure|{failure_type.lower()}|{detail.lower()}",
            label,
        )
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
        variant: str | None = None,
        features: Iterable[str] = (),
        shard_index: int | None = None,
        cross_check: str = "",
    ) -> None:
        group = groups.setdefault(
            key,
            {
                "key": key,
                "kind": kind,
                "label": label,
                "count": 0,
                "_paths": set(),
                "_cases": set(),
                "_variants": Counter(),
                "_features": Counter(),
                "_shards": set(),
                "_crossCheck": Counter(),
            },
        )
        group["count"] += 1
        if path:
            group["_paths"].add(path)
            if variant:
                group["_cases"].add((path, variant))
        if variant:
            group["_variants"][variant] += 1
        group["_features"].update(features)
        if shard_index is not None:
            group["_shards"].add(shard_index)
        if cross_check:
            group["_crossCheck"][cross_check] += 1

    for result in results:
        status = result["status"]
        variant = str(result["variant"])
        if status == "failed":
            kind, key, label = _failure_descriptor(result)
            add(
                f"{key}|variant={variant}",
                kind,
                f"[{variant}] {label}",
                str(result["path"]),
                variant,
                _features(result),
                cross_check=str(result.get("referenceCrossCheck") or ""),
            )
        elif status == "timedOut":
            add(
                f"timeout|variant={variant}",
                "Timeout",
                f"[{variant}] Timed out",
                str(result["path"]),
                variant,
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
        cases = sorted(
            group.pop("_cases"),
            key=lambda item: (item[0], _VARIANT_RANK[item[1]]),
        )
        variant_counter: Counter[str] = group.pop("_variants")
        feature_counter: Counter[str] = group.pop("_features")
        shards = sorted(group.pop("_shards"))
        cross_check_counter: Counter[str] = group.pop("_crossCheck")
        # What the reference engine said about each case in this group. A minified group is
        # only actionable to the extent the cross-check CONFIRMED it (`engine-divergence`:
        # the second engine passes both variants, so the transformation preserved the test);
        # `inconclusive` means it could not run the original and therefore has no opinion,
        # which leaves the case reported here without anything having established it is a
        # defect. Triage reads a group very differently depending on which it is, so the
        # split rides on the group rather than being recoverable only from the raw JSON.
        group["crossCheck"] = [
            {"verdict": verdict, "count": count}
            for verdict, count in sorted(
                cross_check_counter.items(),
                key=lambda item: (-item[1], item[0]),
            )
        ]
        group["paths"] = paths
        group["pathSamples"] = paths[:PATH_SAMPLE_LIMIT]
        group["caseSamples"] = [
            {"path": path, "variant": variant}
            for path, variant in cases[:PATH_SAMPLE_LIMIT]
        ]
        group["variants"] = [
            {"variant": variant, "count": variant_counter[variant]}
            for variant in _VARIANTS
            if variant_counter[variant]
        ]
        group["features"] = [
            {"feature": feature, "count": count}
            for feature, count in sorted(
                feature_counter.items(),
                key=lambda item: (-item[1], item[0].lower(), item[0]),
            )
        ]
        group["shardIndexes"] = shards
        materialised.append(group)

    return sorted(
        materialised,
        key=lambda group: (
            -int(group["count"]),
            _PROBLEM_KIND_TIEBREAK.get(str(group["kind"]), 99),
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
                "caseSamples": [],
                "variants": [],
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
                "caseSamples": list(group["caseSamples"]),
                "variants": list(group["variants"]),
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
    feature_cases: dict[str, set[tuple[str, str]]] = defaultdict(set)
    for result in results:
        if result["status"] != "timedOut":
            continue
        path = str(result["path"])
        variant = str(result["variant"])
        features = _features(result)
        for feature in features:
            feature_cases[feature].add((path, variant))
        timeouts.append(
            {
                "path": path,
                "relativeTestPath": path,
                "variant": variant,
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
            _VARIANT_RANK[str(timeout["variant"])],
        )
    )
    feature_groups = [
        {
            "feature": feature,
            "count": len(cases),
            "pathSamples": sorted({path for path, _variant in cases})[
                :PATH_SAMPLE_LIMIT
            ],
            "caseSamples": [
                {"path": path, "variant": variant}
                for path, variant in sorted(
                    cases,
                    key=lambda item: (item[0], _VARIANT_RANK[item[1]]),
                )[:PATH_SAMPLE_LIMIT]
            ],
        }
        for feature, cases in sorted(
            feature_cases.items(),
            key=lambda item: (-len(item[1]), item[0].lower(), item[0]),
        )
    ]
    return timeouts[:limit], feature_groups


def _optional_size(result: dict[str, Any], key: str) -> int | None:
    value = result.get(key)
    if value is None or isinstance(value, bool):
        return None
    try:
        size = int(value)
    except (TypeError, ValueError):
        return None
    return size if size >= 0 else None


def _minification_ratio(result: dict[str, Any]) -> float | None:
    value = result.get("minificationRatio")
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        return None
    ratio = float(value)
    if not math.isfinite(ratio) or ratio < 0:
        return None
    return ratio


def _minified_variant(results: list[dict[str, Any]]) -> str | None:
    """The one minified variant this run carries, if it carries any."""
    for variant in _MINIFIED_VARIANTS:
        if any(str(result["variant"]) == variant for result in results):
            return variant
    return None


def _rank_minified_only_failures(
    results: list[dict[str, Any]],
) -> tuple[list[dict[str, Any]], list[dict[str, Any]], list[str]]:
    """Isolate paths whose original source passes and whose minified case does not.

    These cases are the only evidence in a run that minification, not the test
    itself, exposes the defect, so they are reported separately from the
    frequency, severity, and timeout views that mix both variants.
    """
    minified_variant = _minified_variant(results)
    if minified_variant is None:
        return [], [], []

    cases_by_path: dict[str, dict[str, dict[str, Any]]] = defaultdict(dict)
    for result in results:
        cases_by_path[str(result["path"])][str(result["variant"])] = result

    cases: list[dict[str, Any]] = []
    for path in sorted(cases_by_path):
        variants = cases_by_path[path]
        original = variants.get(_ORIGINAL_VARIANT)
        minified = variants.get(minified_variant)
        if original is None or minified is None:
            continue
        # Only an original pass proves the minified case regressed on its own.
        if original["status"] != "passed" or original.get("infrastructure") is True:
            continue
        if minified["status"] not in ("failed", "timedOut"):
            continue
        if minified["status"] == "timedOut":
            kind, key, label = (
                "Timeout",
                f"timeout|variant={minified_variant}",
                "Timed out",
            )
        else:
            kind, key, label = _failure_descriptor(minified)
        cases.append(
            {
                "path": path,
                "relativeTestPath": path,
                "variant": minified_variant,
                "status": str(minified["status"]),
                "kind": kind,
                "groupKey": key,
                "label": label,
                "reason": str(minified.get("reason") or ""),
                "failureType": str(minified.get("failureType") or ""),
                "referenceCrossCheck": str(minified.get("referenceCrossCheck") or ""),
                "infrastructure": minified.get("infrastructure") is True,
                "sourceSizeBytes": _source_size(minified),
                "minifiedSourceSizeBytes": _optional_size(
                    minified, "minifiedSourceSizeBytes"
                ),
                "minificationRatio": _minification_ratio(minified),
                "features": _features(minified),
            }
        )

    groups: dict[str, dict[str, Any]] = {}
    for case in cases:
        group = groups.setdefault(
            str(case["groupKey"]),
            {
                "key": str(case["groupKey"]),
                "kind": str(case["kind"]),
                "label": str(case["label"]),
                "count": 0,
                "paths": [],
                "_features": Counter(),
                "_crossCheck": Counter(),
            },
        )
        group["count"] += 1
        group["paths"].append(str(case["path"]))
        group["_features"].update(case["features"])
        if case["referenceCrossCheck"]:
            group["_crossCheck"][str(case["referenceCrossCheck"])] += 1

    materialised: list[dict[str, Any]] = []
    for group in groups.values():
        feature_counter: Counter[str] = group.pop("_features")
        cross_check_counter: Counter[str] = group.pop("_crossCheck")
        group["crossCheck"] = [
            {"verdict": verdict, "count": count}
            for verdict, count in sorted(
                cross_check_counter.items(),
                key=lambda item: (-item[1], item[0]),
            )
        ]
        group["paths"] = sorted(group["paths"])
        group["pathSamples"] = group["paths"][:PATH_SAMPLE_LIMIT]
        group["features"] = [
            {"feature": feature, "count": count}
            for feature, count in sorted(
                feature_counter.items(),
                key=lambda item: (-item[1], item[0].lower(), item[0]),
            )
        ]
        materialised.append(group)
    materialised.sort(
        key=lambda group: (
            -int(group["count"]),
            _PROBLEM_KIND_TIEBREAK.get(str(group["kind"]), 99),
            str(group["label"]).lower(),
            str(group["key"]),
        )
    )

    # The smallest minified body is the cheapest reduction, so it leads the
    # ranked list exactly like the size-ranked timeout report.
    def sort_key(case: dict[str, Any]) -> tuple[bool, int, str]:
        size = case["minifiedSourceSizeBytes"]
        if size is None:
            size = case["sourceSizeBytes"]
        return (size is None, int(size or 0), str(case["path"]))

    ranked = sorted(cases, key=sort_key)
    paths = sorted({str(case["path"]) for case in cases})
    return ranked, materialised, paths


def merge(
    shard_dir: Path,
    phase: str = "full",
    expected_shard_indexes: set[int] | None = None,
    problem_limit: int = DEFAULT_PROBLEM_LIMIT,
    biggest_problem_limit: int = DEFAULT_BIGGEST_PROBLEM_LIMIT,
    timeout_limit: int = DEFAULT_TIMEOUT_LIMIT,
    minifier_only_limit: int = DEFAULT_MINIFIER_ONLY_LIMIT,
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
    if minifier_only_limit < 1:
        raise ValueError("minifier_only_limit must be positive")

    reports, incomplete, retried, reported = _selected_attempts(
        Path(shard_dir), phase, expected_shard_indexes
    )
    results = _deduplicate_results(reports)

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
    configuration_failures.extend(
        _validate_minifier_configuration(run_configuration, reports, results)
    )

    statuses = ("passed", "failed", "skipped", "timedOut")
    status_cases: dict[str, list[dict[str, str]]] = {
        status: [
            _case_reference(result)
            for result in results
            if result["status"] == status
        ]
        for status in statuses
    }
    not_applicable_cases = [
        _case_reference(result) for result in results if _is_not_applicable(result)
    ]
    cases_by_path: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for result in results:
        cases_by_path[str(result["path"])].append(result)

    # Existing path arrays stay base-path-only. Their aggregate outcome uses a
    # deterministic severity order while case arrays retain every variant.
    path_outcomes: dict[str, str] = {}
    for path, cases in sorted(cases_by_path.items()):
        case_statuses = {str(case["status"]) for case in cases}
        if "timedOut" in case_statuses:
            path_outcomes[path] = "timedOut"
        elif "failed" in case_statuses:
            path_outcomes[path] = "failed"
        elif "passed" in case_statuses:
            path_outcomes[path] = "passed"
        else:
            path_outcomes[path] = "skipped"
    status_paths: dict[str, list[str]] = {
        status: [
            path for path in sorted(path_outcomes) if path_outcomes[path] == status
        ]
        for status in statuses
    }
    not_applicable_paths = sorted(
        {case["path"] for case in not_applicable_cases}
    )
    # Counted per kind so a not-applicable case stays VISIBLE after it leaves the failure
    # column: "684 cases the mangler changed out of being the test" is a fact about the
    # variant that a single skipped total hides.
    not_applicable_by_kind = dict(
        sorted(
            Counter(
                str(result.get("skipKind") or _MINIFIER_NOT_APPLICABLE)
                for result in results
                if _is_not_applicable(result)
            ).items()
        )
    )

    observed_variants = sorted(
        {str(result["variant"]) for result in results},
        key=lambda value: _VARIANT_RANK[value],
    )
    configured_variants = run_configuration.get("variants")
    expected_variants = (
        list(configured_variants)
        if configured_variants is not None
        and tuple(configured_variants) in _valid_variant_shapes()
        else (observed_variants or [_ORIGINAL_VARIANT])
    )
    expected_variant_set = set(expected_variants)
    partial_variant_paths = [
        path
        for path, cases in sorted(cases_by_path.items())
        if {str(case["variant"]) for case in cases} != expected_variant_set
    ]
    if partial_variant_paths:
        configuration_failures.append(
            {
                "kind": "IncompleteVariantCoverage",
                "message": (
                    f"{len(partial_variant_paths)} base path(s) did not emit every "
                    f"configured variant: {', '.join(partial_variant_paths[:5])}."
                ),
            }
        )

    candidate_executed_paths: list[str] = []
    for path, cases in sorted(cases_by_path.items()):
        cases_by_variant = {str(case["variant"]): case for case in cases}
        if set(cases_by_variant) != expected_variant_set:
            continue
        original = cases_by_variant.get("original")
        if (
            original is None
            or original["status"] != "passed"
            or original.get("infrastructure") is True
        ):
            continue
        if all(
            case.get("infrastructure") is not True
            and (
                case["status"] == "passed"
                or (
                    variant != "original"
                    and _is_not_applicable(case)
                )
            )
            for variant, case in cases_by_variant.items()
        ):
            candidate_executed_paths.append(path)
    case_executed_count = sum(
        len(status_cases[status]) for status in ("passed", "failed", "timedOut")
    )
    unique_path_count = len(cases_by_path)
    summary = {
        "total": len(results),
        "executed": case_executed_count,
        "passed": len(status_cases["passed"]),
        "failed": len(status_cases["failed"]),
        "skipped": len(status_cases["skipped"]),
        "timedOut": len(status_cases["timedOut"]),
        "notApplicable": len(not_applicable_cases),
        "notApplicableByKind": not_applicable_by_kind,
        "uniquePaths": unique_path_count,
    }

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
        and unique_path_count != configured_selection_count
    ):
        configuration_failures.append(
            {
                "kind": "IncompleteSelectionCoverage",
                "message": (
                    f"All {configured_shard_count} shards reported, but their "
                    f"{unique_path_count} unique base paths do not cover the "
                    f"{configured_selection_count} paths selected before sharding; "
                    f"the {summary['total']} variant cases are counted separately."
                ),
            }
        )

    # A configuration/coverage failure makes pass evidence unsafe to use for
    # clearing a path-only rerun manifest. Current failures are still added
    # below, so this conservative rule can only retain extra rerun coverage.
    executed_paths = (
        candidate_executed_paths if not configuration_failures else []
    )

    variant_summary: dict[str, dict[str, int]] = {}
    for variant in expected_variants:
        variant_results = [
            result for result in results if result["variant"] == variant
        ]
        variant_summary[variant] = {
            "total": len(variant_results),
            "executed": sum(
                result["status"] in ("passed", "failed", "timedOut")
                for result in variant_results
            ),
            "passed": sum(result["status"] == "passed" for result in variant_results),
            "failed": sum(result["status"] == "failed" for result in variant_results),
            "skipped": sum(result["status"] == "skipped" for result in variant_results),
            "timedOut": sum(
                result["status"] == "timedOut" for result in variant_results
            ),
            "notApplicable": sum(
                _is_not_applicable(result) for result in variant_results
            ),
            "uniquePaths": len(
                {str(result["path"]) for result in variant_results}
            ),
        }

    path_summary = {
        "total": unique_path_count,
        "executed": sum(
            len(status_paths[status])
            for status in ("passed", "failed", "timedOut")
        ),
        "refreshable": len(executed_paths),
        "passed": len(status_paths["passed"]),
        "failed": len(status_paths["failed"]),
        "skipped": len(status_paths["skipped"]),
        "timedOut": len(status_paths["timedOut"]),
        "notApplicable": len(not_applicable_paths),
    }

    all_groups = _materialise_problem_groups(results, incomplete)
    biggest = _rank_biggest_problems(
        all_groups, incomplete, biggest_problem_limit
    )
    timeouts, timeout_features = _rank_timeouts(results, timeout_limit)
    (
        minifier_only_cases,
        minifier_only_groups,
        minifier_only_paths,
    ) = _rank_minified_only_failures(results)
    minifier_only_infrastructure_failures = sum(
        1 for case in minifier_only_cases if case["kind"] == "MinifierFailure"
    )
    # Of the cases left after the infrastructure ones, how many the cross-check actually
    # settled against the engine. The rest are cases it had no opinion about — a reference
    # engine that cannot run the ORIGINAL cannot say whether the minified body is still the
    # test — and reading them as engine defects is what a triage does by accident when the
    # report gives it one number.
    minifier_only_confirmed = sum(
        1
        for case in minifier_only_cases
        if case.get("referenceCrossCheck") == _ENGINE_DIVERGENCE
    )
    minifier_only_inconclusive = sum(
        1
        for case in minifier_only_cases
        if case.get("referenceCrossCheck") == _CROSS_CHECK_INCONCLUSIVE
    )
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
        "pathSummary": path_summary,
        "variantSummary": variant_summary,
        "observedVariants": observed_variants,
        "uniquePathCount": unique_path_count,
        "passedPaths": status_paths["passed"],
        "failedPaths": status_paths["failed"],
        "skippedPaths": status_paths["skipped"],
        "timedOutPaths": status_paths["timedOut"],
        "notApplicablePaths": not_applicable_paths,
        "passedCases": status_cases["passed"],
        "failedCases": status_cases["failed"],
        "skippedCases": status_cases["skipped"],
        "timedOutCases": status_cases["timedOut"],
        "notApplicableCases": not_applicable_cases,
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
        # Which minifier produced the minified half of this run, so a reader (and the
        # issue this feeds) never has to infer it from a profile string.
        "minifiedVariant": _minified_variant(results) or "",
        "minifierOnlyLimit": minifier_only_limit,
        "minifierOnlyFailureCount": len(minifier_only_cases),
        "minifierOnlyMinifierFailureCount": minifier_only_infrastructure_failures,
        "minifierOnlyDivergenceCount": (
            len(minifier_only_cases) - minifier_only_infrastructure_failures
        ),
        "minifierOnlyConfirmedDivergenceCount": minifier_only_confirmed,
        "minifierOnlyInconclusiveCount": minifier_only_inconclusive,
        "minifierOnlyPathCount": len(minifier_only_paths),
        "minifierOnlyPaths": minifier_only_paths,
        "minifierOnlyFailures": minifier_only_cases[:minifier_only_limit],
        "minifierOnlyGroups": minifier_only_groups,
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
    """Update a text failure manifest using only safely refreshable paths."""
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


_CROSS_CHECK_DESCRIPTIONS = {
    _ENGINE_DIVERGENCE: "the reference engine passes both variants",
    _CROSS_CHECK_INCONCLUSIVE: "the reference engine cannot run the original either",
}


def _cross_check_line(group: dict[str, Any]) -> str:
    """The group's reference-engine verdicts, or "" when it was never cross-checked.

    Only a minified case is cross-checked at all, so an original-variant group has nothing
    here — and a group that IS cross-checked is read very differently depending on which
    verdict it carries. See the constants beside `_ENGINE_DIVERGENCE`.
    """
    verdicts = group.get("crossCheck") or []
    if not verdicts:
        return ""
    rendered = ", ".join(
        f"{_inline(item['verdict'])} {item['count']}"
        + (
            f" ({_CROSS_CHECK_DESCRIPTIONS[str(item['verdict'])]})"
            if str(item["verdict"]) in _CROSS_CHECK_DESCRIPTIONS
            else ""
        )
        for item in verdicts
    )
    return f"   - Reference cross-check: {rendered}"


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
        f"- Total variant cases: {summary['total']}",
        f"- Unique base paths: {summary.get('uniquePaths', summary['total'])}",
        f"- Passed cases: {summary['passed']}",
        f"- Failed cases: {summary['failed']}",
        f"- Timed-out cases: {summary['timedOut']}",
        f"- Skipped cases: {summary['skipped']}",
        f"- Minifier not-applicable cases: {summary.get('notApplicable', 0)}",
        (
            "- Configured variants: "
            + ", ".join(
                str(value)
                for value in (
                    merged.get("runConfiguration", {}).get("variants")
                    or merged.get("observedVariants")
                    or ["original"]
                )
            )
        ),
        f"- Incomplete shards: {len(merged['incompleteShards'])}",
        "",
    ]
    if merged.get("variantSummary"):
        lines.extend(["### Variant breakdown", ""])
        for variant, counts in merged["variantSummary"].items():
            lines.append(
                f"- **{_inline(variant)}** — {counts['passed']} passed, "
                f"{counts['failed']} failed, {counts['timedOut']} timed out, "
                f"{counts['skipped']} skipped "
                f"({counts.get('notApplicable', 0)} not applicable)"
            )
        lines.append("")
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
        if group.get("caseSamples"):
            samples = ", ".join(
                f"{_inline(case['path'])} [{_inline(case['variant'])}]"
                for case in group["caseSamples"]
            )
            lines.append(f"   - Variant-case samples: {samples}")
        elif group["pathSamples"]:
            samples = ", ".join(_inline(path) for path in group["pathSamples"])
            lines.append(f"   - Path samples: {samples}")
        cross_check = _cross_check_line(group)
        if cross_check:
            lines.append(cross_check)
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
        if problem.get("caseSamples"):
            samples = ", ".join(
                f"{_inline(case['path'])} [{_inline(case['variant'])}]"
                for case in problem["caseSamples"]
            )
            lines.append(f"   - Variant-case samples: {samples}")
        elif problem["pathSamples"]:
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
            f"{_inline(timeout['path'])} [{_inline(timeout['variant'])}]"
        )
        lines.append(f"   - Features: {_inline(features)}")

    feature_groups = merged.get("timeoutFeatureGroups") or []
    if feature_groups:
        lines.extend(["", "### Timeout feature clusters", ""])
        for group in feature_groups:
            samples = ", ".join(
                f"{_inline(case['path'])} [{_inline(case['variant'])}]"
                for case in group.get("caseSamples", [])
            )
            lines.append(
                f"- **{_inline(group['feature'])}** — "
                f"{group['count']} timeout case(s); variant-case samples: {samples}"
            )
    lines.extend(_metadata_lines(run_url, artifact_name))
    return "\n".join(lines) + "\n"


def _format_minified_size(case: dict[str, Any]) -> str:
    minified = case.get("minifiedSourceSizeBytes")
    original = case.get("sourceSizeBytes")
    ratio = case.get("minificationRatio")
    if minified is None:
        if original is None:
            return "unknown size"
        return f"{_format_size(original)} original, unknown minified size"
    label = f"{_format_size(minified)} minified"
    if original is not None:
        label += f" of {_format_size(original)} original"
    if isinstance(ratio, (int, float)) and not isinstance(ratio, bool):
        label += f" (ratio {float(ratio):g})"
    return label


def render_minifier_only_issue_markdown(
    merged: dict[str, Any],
    run_url: str | None = None,
    artifact_name: str = "test262-merged",
) -> str:
    count = int(merged.get("minifierOnlyFailureCount") or 0)
    divergences = int(merged.get("minifierOnlyDivergenceCount") or 0)
    confirmed = int(merged.get("minifierOnlyConfirmedDivergenceCount") or 0)
    inconclusive = int(merged.get("minifierOnlyInconclusiveCount") or 0)
    minifier_failures = int(merged.get("minifierOnlyMinifierFailureCount") or 0)
    limit = int(merged.get("minifierOnlyLimit") or DEFAULT_MINIFIER_ONLY_LIMIT)
    cases = merged.get("minifierOnlyFailures") or []
    groups = merged.get("minifierOnlyGroups") or []
    variant = str(merged.get("minifiedVariant") or "")
    label = variant.capitalize() if variant else "Minifier"
    profile = str(
        (merged.get("runConfiguration") or {}).get("minifierProfile")
        or _MINIFIER_PROFILES.get(variant, "the configured profile")
    )
    lines = [
        f"## test262 {merged['phase']} run — {count} {label}-only failure(s)",
        "",
        "_Every case below passes as original source and stops passing only after "
        f"{profile} minification, so minification — not the test itself — "
        "is the trigger._",
        "",
        f"- {label}-only non-passing cases: {count}",
        f"- Engine divergences after minification: {divergences}",
        f"  - confirmed by the reference engine (it passes both variants): {confirmed}",
        f"  - no opinion (the reference engine cannot run the original either): "
        f"{inconclusive}",
        f"- Minifier infrastructure failures: {minifier_failures}",
        f"- Affected base paths: {int(merged.get('minifierOnlyPathCount') or 0)}",
    ]
    attributed = merged["summary"].get("notApplicableByKind") or {}
    for kind, attribution in (
        (
            _MINIFIER_UNSUPPORTED_SYNTAX,
            "source the minifier cannot read or does not preserve",
        ),
        (_MINIFIER_INTERNAL_ERROR, "source the minifier crashed on"),
        (_MINIFIER_INVALID_OUTPUT, "minified body the reference engine cannot parse"),
        (_MINIFIER_CHANGED_SEMANTICS, "minification changed what the test measures"),
    ):
        if attributed.get(kind):
            lines.append(
                f"- Attributed to the minifier ({attribution}): {attributed[kind]}"
            )
    lines.append("")
    lines.extend([f"### Normalized {label}-only failure groups", ""])
    if not groups:
        lines.append("- None")
    for index, group in enumerate(groups, start=1):
        lines.append(
            f"{index}. **{_inline(group['label'])}** — "
            f"{group['count']} case(s) ({group['kind']})"
        )
        if group.get("pathSamples"):
            samples = ", ".join(_inline(path) for path in group["pathSamples"])
            lines.append(f"   - Path samples: {samples}")
        cross_check = _cross_check_line(group)
        if cross_check:
            lines.append(cross_check)
        if group.get("features"):
            features = ", ".join(
                f"{_inline(item['feature'])} ({item['count']})"
                for item in group["features"][:PATH_SAMPLE_LIMIT]
            )
            lines.append(f"   - Features: {features}")

    lines.extend(
        [
            "",
            f"### First {limit} case(s) by minified source size",
            "",
        ]
    )
    if not cases:
        lines.append("- None")
    for index, case in enumerate(cases, start=1):
        lines.append(
            f"{index}. **{_format_minified_size(case)}** — "
            f"{_inline(case['path'])}"
        )
        detail = _inline(case.get("reason")) or "(no reason reported)"
        status = "timed out" if case["status"] == "timedOut" else str(case["status"])
        lines.append(f"   - Minified case {status}: {detail}")
        features = ", ".join(_inline(feature) for feature in case.get("features") or [])
        lines.append(f"   - Features: {features or '(none reported)'}")

    if cases:
        lines.extend(
            [
                "",
                "### Reproduce",
                "",
                "Run one base path as both variants with "
                "`python scripts/compliance/run_test262.py --suite-ref "
                f"{_inline(merged.get('suiteRef')) or '<suite-ref>'} --subset "
                f"{_inline(cases[0]['path'])} --minifier {variant or 'terser'} "
                f"--{variant or 'terser'}-module <package-directory>`.",
            ]
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
    minifier_only_count = int(merged.get("minifierOnlyFailureCount") or 0)
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
        "not_applicable_count": summary.get("notApplicable", 0),
        "total_count": summary["total"],
        "unique_path_count": summary.get("uniquePaths", summary["total"]),
        "original_case_count": (
            merged.get("variantSummary", {}).get("original", {}).get("total", 0)
        ),
        "minified_variant": str(merged.get("minifiedVariant") or ""),
        "minified_case_count": (
            merged.get("variantSummary", {})
            .get(str(merged.get("minifiedVariant") or ""), {})
            .get("total", 0)
        ),
        "incomplete_shard_count": len(incomplete_indexes),
        "create_issue": "true" if has_failures else "false",
        "biggest_problem_count": biggest_count,
        "create_biggest_issue": "true" if biggest_count else "false",
        "timeout_count": timeout_count,
        "create_timeout_issue": "true" if timeout_count else "false",
        "minifier_only_failure_count": minifier_only_count,
        "minifier_only_divergence_count": int(
            merged.get("minifierOnlyDivergenceCount") or 0
        ),
        "minifier_only_infrastructure_failure_count": int(
            merged.get("minifierOnlyMinifierFailureCount") or 0
        ),
        "minifier_only_path_count": int(merged.get("minifierOnlyPathCount") or 0),
        "create_minifier_only_issue": "true" if minifier_only_count else "false",
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
    parser.add_argument("--minifier-only-issue-md", type=Path)
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
        "--minifier-only-limit", type=int, default=DEFAULT_MINIFIER_ONLY_LIMIT
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
    if args.minifier_only_limit < 1:
        parser.error("--minifier-only-limit must be a positive integer")

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
            minifier_only_limit=args.minifier_only_limit,
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
        (
            args.minifier_only_issue_md,
            render_minifier_only_issue_markdown(
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
        f"{summary['passed']} variant case(s) passed, "
        f"{summary['failed']} failed, {summary['timedOut']} timed out, "
        f"{summary['skipped']} skipped across "
        f"{summary.get('uniquePaths', summary['total'])} base path(s); "
        f"{len(merged['incompleteShards'])} incomplete shard(s); "
        f"{int(merged.get('minifierOnlyFailureCount') or 0)} minifier-only failure(s)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
