#!/usr/bin/env python3

from __future__ import annotations

import argparse
import glob
import json
import re
from collections import Counter
from pathlib import Path

from run_test262 import DEFAULT_SUITE_REF, Test262Repository, UNSUPPORTED_FLAGS, parse_metadata


def load_manifest_paths(manifests: list[str], manifest_globs: list[str]) -> tuple[list[str], list[str]]:
    manifest_files = [Path(path).resolve() for path in manifests]
    for pattern in manifest_globs:
        manifest_files.extend(Path(path).resolve() for path in glob.glob(pattern, recursive=True))

    unique_manifest_files = sorted(dict.fromkeys(manifest_files))
    requested_paths: list[str] = []
    for manifest_file in unique_manifest_files:
        for line in manifest_file.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if line and not line.startswith("#"):
                requested_paths.append(line)

    return requested_paths, [str(path) for path in unique_manifest_files]


def collect_suite_test_paths(suite_root: Path) -> list[str]:
    test_root = suite_root / "test"
    if not test_root.is_dir():
        raise FileNotFoundError(f"Missing test262 test directory: {test_root}")

    return sorted(path.relative_to(suite_root).as_posix() for path in test_root.rglob("*.js"))


def parse_negative_metadata(source: str) -> dict[str, str] | None:
    metadata_match = re.search(r"/\*---\n(.*?)\n---\*/", source, re.S)
    if metadata_match is None:
        return None

    negative_match = re.search(r"(?m)^negative:\s*\n((?:[ \t]+.*\n?)*)", metadata_match.group(1))
    if negative_match is None:
        return None

    negative_block = negative_match.group(1)
    summary: dict[str, str] = {}
    for key in ("phase", "type"):
        value_match = re.search(rf"(?m)^[ \t]+{key}:\s*(.+?)\s*$", negative_block)
        if value_match is not None:
            summary[key] = value_match.group(1)

    return summary if summary else None


def classify_test(source: str) -> dict[str, object]:
    metadata, _ = parse_metadata(source)
    flags = list(metadata["flags"])
    unsupported_flags = sorted(UNSUPPORTED_FLAGS & set(flags))
    negative = parse_negative_metadata(source)
    return {
        "flags": flags,
        "unsupportedFlags": unsupported_flags,
        "negative": negative,
        "isScriptHostVerifiable": not unsupported_flags and negative is None,
    }


def build_audit_summary(
    repo: Test262Repository,
    suite_root: Path,
    suite_ref: str,
    requested_paths: list[str],
    manifest_files: list[str],
) -> dict[str, object]:
    suite_paths = collect_suite_test_paths(suite_root)
    manifest_expanded_paths = repo.expand_paths(requested_paths)

    unsupported_flag_counts: Counter[str] = Counter()
    unsupported_flagged_tests = 0
    negative_tests = 0
    async_script_host_verifiable_tests = 0
    no_strict_script_host_verifiable_tests = 0
    script_host_verifiable_tests = 0

    manifest_unsupported_tests: list[str] = []
    manifest_negative_tests: list[str] = []
    manifest_script_host_verifiable_tests = 0

    manifest_path_set = set(manifest_expanded_paths)
    for path in suite_paths:
        classification = classify_test(repo.read_text(path))
        unsupported_flags = classification["unsupportedFlags"]
        negative = classification["negative"]
        flags = classification["flags"]

        if unsupported_flags:
            unsupported_flagged_tests += 1
            unsupported_flag_counts.update(unsupported_flags)

        if negative is not None:
            negative_tests += 1

        if classification["isScriptHostVerifiable"]:
            script_host_verifiable_tests += 1
            if "async" in flags:
                async_script_host_verifiable_tests += 1
            if "noStrict" in flags:
                no_strict_script_host_verifiable_tests += 1

        if path not in manifest_path_set:
            continue

        if unsupported_flags:
            manifest_unsupported_tests.append(path)
        if negative is not None:
            manifest_negative_tests.append(path)
        if classification["isScriptHostVerifiable"]:
            manifest_script_host_verifiable_tests += 1

    suite_tests_discovered = len(suite_paths)
    manifest_unique_tests = len(manifest_expanded_paths)
    manifest_entries = len(requested_paths)

    return {
        "suiteRef": suite_ref,
        "suiteRoot": str(suite_root),
        "suiteTestsDiscovered": suite_tests_discovered,
        "unsupportedFlaggedTests": unsupported_flagged_tests,
        "unsupportedFlagCounts": dict(sorted(unsupported_flag_counts.items())),
        "negativeTests": negative_tests,
        "scriptHostVerifiableTests": script_host_verifiable_tests,
        "asyncScriptHostVerifiableTests": async_script_host_verifiable_tests,
        "noStrictScriptHostVerifiableTests": no_strict_script_host_verifiable_tests,
        "manifestFiles": manifest_files,
        "manifestEntries": manifest_entries,
        "manifestUniqueTests": manifest_unique_tests,
        "manifestUnsupportedTests": manifest_unsupported_tests,
        "manifestNegativeTests": manifest_negative_tests,
        "manifestScriptHostVerifiableTests": manifest_script_host_verifiable_tests,
        "manifestCoverageOfSuitePercent": (
            manifest_unique_tests * 100.0 / suite_tests_discovered if suite_tests_discovered else 0.0
        ),
        "manifestCoverageOfScriptHostVerifiablePercent": (
            manifest_script_host_verifiable_tests * 100.0 / script_host_verifiable_tests
            if script_host_verifiable_tests
            else 0.0
        ),
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Audit test262 suite discovery and pinned manifest coverage."
    )
    parser.add_argument(
        "--suite-ref",
        default=DEFAULT_SUITE_REF,
        help="Pinned test262 commit SHA",
    )
    parser.add_argument(
        "--suite-root",
        required=True,
        help="Path to a local test262 checkout",
    )
    parser.add_argument(
        "--manifest",
        action="append",
        default=[],
        help="Path to a manifest file containing test262 paths",
    )
    parser.add_argument(
        "--manifest-glob",
        action="append",
        default=[],
        help="Glob pattern for manifest files",
    )
    parser.add_argument(
        "--output",
        help="Optional path for machine-readable JSON output",
    )
    args = parser.parse_args()

    requested_paths, manifest_files = load_manifest_paths(args.manifest, args.manifest_glob)
    suite_root = Path(args.suite_root).resolve()
    repo = Test262Repository(args.suite_ref, str(suite_root))
    summary = build_audit_summary(
        repo,
        suite_root,
        args.suite_ref,
        requested_paths,
        manifest_files,
    )

    if args.output:
        output_path = Path(args.output)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")

    print(json.dumps(summary, indent=2))

    # The current raw script-host runner only produces trustworthy pass/fail totals for
    # manifests whose entries are script-host-verifiable. Failing here prevents CI from
    # silently reporting coverage for unsupported-flag or negative-metadata files that
    # this workflow does not validate correctly yet.
    return 1 if summary["manifestUnsupportedTests"] or summary["manifestNegativeTests"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
