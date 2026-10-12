#!/usr/bin/env python3
"""Compare two whole-suite test262 runs of this engine, path by path.

The comparison template T2 of the shared JavaScript libraries programme judges a change to this
engine by joining a base run and an after run on (path, variant) and reading every changed verdict.
This script does the join and nothing else: it decides no class itself. A changed verdict is
admitted only when a classification file, written by a person and retained beside the report,
names it with a class and a reason. Totals are not compared, because a total can improve while a
row regresses.

Inputs are the merged report of merge_test262_shards.py or a single run_test262.py report. Both
runs must be comparable: complete, with no configuration failure, at the same suite revision, and
with the same run configuration except the fields in ``_UNCOMPARED_FIELDS``, which change no
verdict but a timeout. A row's verdict is its ``status``; ``hostMode`` is reported beside it.

A timeout is confirmed by running the path again alone. ``--base-rerun`` and ``--after-rerun``
take run_test262.py reports of those re-runs; each of their rows replaces the same (path, variant)
row of its side, and the replacement is reported, never hidden.

The classification file is JSON: ``{"path|variant": {"class": "C1", "reason": "..."}}``. The
admitted classes are C1 (correction), C3 (implementation-defined) and C5 (data policy by decision).
A row that passed at the base and fails, times out or is skipped after is a regression whatever the
file says; a classification of such a row is reported as refused.

Exit status: 0 when every changed row is admitted, 1 when a row is a regression or unclassified,
2 when the runs are not comparable or an input is malformed.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

ADMITTED_CLASSES = ("C1", "C3", "C5")
_CONFIGURATION_FIELDS = (
    "suiteRef",
    "selectionMode",
    "testTimeoutSeconds",
    "memoryLimitMb",
    "includeNegative",
    "minifier",
    "minifierVersion",
    "minifierProfile",
    "minifierTimeoutSeconds",
    "minifierOptions",
    "variants",
    "hostModes",
    "subsetPatterns",
    "featurePatterns",
    "featureMatch",
)
# Recorded in the report when they differ; they change no verdict but a timeout, and a timeout is
# confirmed by a re-run alone.
_UNCOMPARED_FIELDS = ("maxWorkers", "shardCount", "runnerOs", "runnerArch", "dotnetVersion")


class NotComparable(Exception):
    pass


def _load(path: Path) -> dict[str, Any]:
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError) as exc:
        raise NotComparable(f"{path}: {exc}") from exc
    if not isinstance(payload, dict) or not isinstance(payload.get("results"), list):
        raise NotComparable(f"{path}: not a test262 report (no results list)")
    return payload


def _configuration(payload: dict[str, Any]) -> dict[str, Any]:
    """The run configuration of a merged report, or the top-level fields of a runner report."""
    merged = payload.get("runConfiguration")
    source = merged if isinstance(merged, dict) and merged else payload
    configuration = {
        field: source.get(field, payload.get(field))
        for field in _CONFIGURATION_FIELDS + _UNCOMPARED_FIELDS
    }
    configuration["suiteRef"] = payload.get("suiteRef") or source.get("suiteRef")
    return configuration


def _completeness_problems(name: str, payload: dict[str, Any]) -> list[str]:
    problems = []
    if payload.get("incompleteShards"):
        problems.append(f"{name}: incomplete shards {payload['incompleteShards']!r}")
    if payload.get("configurationFailures"):
        problems.append(f"{name}: configuration failures {payload['configurationFailures']!r}")
    if "suiteRefs" in payload and len(payload["suiteRefs"]) != 1:
        problems.append(f"{name}: suite revisions {payload['suiteRefs']!r}")
    return problems


def _rows(name: str, payload: dict[str, Any]) -> dict[tuple[str, str], dict[str, Any]]:
    rows: dict[tuple[str, str], dict[str, Any]] = {}
    for result in payload["results"]:
        key = (str(result["path"]), str(result.get("variant", "original")))
        if key in rows:
            raise NotComparable(f"{name}: duplicate row {key[0]} [{key[1]}]")
        rows[key] = result
    return rows


def _apply_reruns(
    name: str,
    rows: dict[tuple[str, str], dict[str, Any]],
    rerun_paths: list[Path],
) -> list[dict[str, Any]]:
    replaced = []
    for rerun_path in rerun_paths:
        for key, result in _rows(f"{name} re-run {rerun_path}", _load(rerun_path)).items():
            if key not in rows:
                raise NotComparable(f"{name} re-run {rerun_path}: {key[0]} [{key[1]}] is not in the run")
            replaced.append(
                {
                    "path": key[0],
                    "variant": key[1],
                    "before": rows[key]["status"],
                    "rerun": result["status"],
                    "rerunReport": str(rerun_path),
                }
            )
            rows[key] = result
    return replaced


def compare(
    base: dict[str, Any],
    after: dict[str, Any],
    classifications: dict[str, Any],
    base_reruns: list[Path] | None = None,
    after_reruns: list[Path] | None = None,
) -> dict[str, Any]:
    problems = _completeness_problems("base", base) + _completeness_problems("after", after)
    base_configuration = _configuration(base)
    after_configuration = _configuration(after)
    for field in _CONFIGURATION_FIELDS:
        if base_configuration.get(field) != after_configuration.get(field):
            problems.append(
                f"configuration field {field} differs: "
                f"{base_configuration.get(field)!r} != {after_configuration.get(field)!r}"
            )
    if problems:
        raise NotComparable("; ".join(problems))

    base_rows = _rows("base", base)
    after_rows = _rows("after", after)
    replaced = {
        "base": _apply_reruns("base", base_rows, base_reruns or []),
        "after": _apply_reruns("after", after_rows, after_reruns or []),
    }
    missing = sorted(set(base_rows) - set(after_rows))
    added = sorted(set(after_rows) - set(base_rows))
    if missing or added:
        sample = [f"{p} [{v}]" for p, v in (missing + added)[:5]]
        raise NotComparable(
            f"the row sets differ: {len(missing)} only at base, {len(added)} only after, e.g. {sample}"
        )

    changed = []
    used_classifications = set()
    for key in sorted(base_rows):
        before = str(base_rows[key]["status"])
        now = str(after_rows[key]["status"])
        if before == now:
            continue
        label = f"{key[0]}|{key[1]}"
        entry: dict[str, Any] = {
            "path": key[0],
            "variant": key[1],
            "hostMode": after_rows[key].get("hostMode", "script"),
            "base": before,
            "after": now,
        }
        given = classifications.get(label)
        if given is not None:
            used_classifications.add(label)
        if before == "passed":
            entry["verdict"] = "regression"
            if given is not None:
                entry["refusedClassification"] = given
        elif isinstance(given, dict) and given.get("class") in ADMITTED_CLASSES and given.get("reason"):
            entry["verdict"] = "admitted"
            entry["class"] = given["class"]
            entry["reason"] = given["reason"]
        else:
            entry["verdict"] = "unclassified"
            if given is not None:
                entry["refusedClassification"] = given
        changed.append(entry)

    unused = sorted(set(classifications) - used_classifications)
    regressions = [row for row in changed if row["verdict"] == "regression"]
    unclassified = [row for row in changed if row["verdict"] == "unclassified"]
    return {
        "suiteRef": base_configuration["suiteRef"],
        "baseCommit": base.get("broilerCommit"),
        "afterCommit": after.get("broilerCommit"),
        "uncomparedFields": {
            field: {"base": base_configuration.get(field), "after": after_configuration.get(field)}
            for field in _UNCOMPARED_FIELDS
        },
        "rows": len(base_rows),
        "replacedByRerun": replaced,
        "changed": changed,
        "unusedClassifications": unused,
        "regressions": len(regressions),
        "unclassified": len(unclassified),
        "admitted": len(changed) - len(regressions) - len(unclassified),
        "allChangesAdmitted": not regressions and not unclassified,
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--base", type=Path, required=True, help="the base run's report")
    parser.add_argument("--after", type=Path, required=True, help="the after run's report")
    parser.add_argument("--classifications", type=Path, help="the classification file, if any")
    parser.add_argument("--base-rerun", type=Path, action="append", default=[])
    parser.add_argument("--after-rerun", type=Path, action="append", default=[])
    parser.add_argument("--output", type=Path, help="write the report here as JSON")
    args = parser.parse_args(argv)

    try:
        classifications: dict[str, Any] = {}
        if args.classifications is not None:
            classifications = json.loads(args.classifications.read_text(encoding="utf-8"))
            if not isinstance(classifications, dict):
                raise NotComparable(f"{args.classifications}: not a JSON object")
        report = compare(
            _load(args.base),
            _load(args.after),
            classifications,
            args.base_rerun,
            args.after_rerun,
        )
    except (NotComparable, OSError, ValueError) as exc:
        print(f"not comparable: {exc}", file=sys.stderr)
        return 2

    text = json.dumps(report, indent=2, sort_keys=True) + "\n"
    if args.output is not None:
        args.output.write_text(text, encoding="utf-8", newline="\n")
    for row in report["changed"]:
        detail = row.get("class", "")
        print(f"{row['verdict']:<12} {row['base']:>8} -> {row['after']:<8} {row['path']} [{row['variant']}] {detail}".rstrip())
    print(
        f"rows {report['rows']}; changed {len(report['changed'])}; admitted {report['admitted']}; "
        f"unclassified {report['unclassified']}; regressions {report['regressions']}"
    )
    return 0 if report["allChangesAdmitted"] else 1


if __name__ == "__main__":
    sys.exit(main())
