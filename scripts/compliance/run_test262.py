#!/usr/bin/env python3

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import tempfile
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path


DEFAULT_SUITE_REF = "ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e"
DEFAULT_BROILER_DLL = (
    "/home/runner/work/Broiler.JS/Broiler.JS/"
    "Broiler.JS/Broiler.JavaScript/bin/Debug/net8.0/BroilerJS.dll"
)
UNSUPPORTED_FLAGS = {"async", "module", "raw", "onlyStrict", "noStrict"}


class Test262Repository:
    def __init__(self, suite_ref: str):
        self.suite_ref = suite_ref
        self.contents_cache: dict[str, list[dict[str, object]]] = {}
        self.text_cache: dict[str, str] = {}

    def _fetch_json(self, path: str):
        encoded_path = urllib.parse.quote(path.strip("/"))
        url = (
            f"https://api.github.com/repos/tc39/test262/contents/{encoded_path}"
            f"?ref={self.suite_ref}"
        )
        request = urllib.request.Request(
            url,
            headers={
                "Accept": "application/vnd.github+json",
                "User-Agent": "Broiler.JS compliance runner",
            },
        )
        with urllib.request.urlopen(request) as response:
            return json.load(response)

    def read_text(self, path: str) -> str:
        if path not in self.text_cache:
            url = (
                f"https://raw.githubusercontent.com/tc39/test262/"
                f"{self.suite_ref}/{path}"
            )
            request = urllib.request.Request(
                url, headers={"User-Agent": "Broiler.JS compliance runner"}
            )
            with urllib.request.urlopen(request) as response:
                self.text_cache[path] = response.read().decode("utf-8")
        return self.text_cache[path]

    def expand_paths(self, paths: list[str]) -> list[str]:
        files: list[str] = []
        for path in paths:
            files.extend(self._expand_path(path.strip("/")))
        return sorted(dict.fromkeys(files))

    def _expand_path(self, path: str) -> list[str]:
        if path.endswith(".js"):
            return [path]

        if path in self.contents_cache:
            entries = self.contents_cache[path]
        else:
            response = self._fetch_json(path)
            if isinstance(response, dict) and response.get("type") == "file":
                return [path]
            entries = response
            self.contents_cache[path] = entries

        files: list[str] = []
        for entry in entries:
            entry_path = str(entry["path"])
            entry_type = str(entry["type"])
            if entry_type == "file" and entry_path.endswith(".js"):
                files.append(entry_path)
            elif entry_type == "dir":
                files.extend(self._expand_path(entry_path))
        return files


def parse_metadata(source: str) -> tuple[dict[str, list[str]], str]:
    match = re.search(r"/\*---\n(.*?)\n---\*/", source, re.S)
    if match is None:
        return {"includes": [], "flags": []}, source

    block = match.group(1)
    body = source[: match.start()] + source[match.end() :]

    def parse_list(name: str) -> list[str]:
        list_match = re.search(rf"{name}:\s*\[(.*?)\]", block, re.S)
        if list_match is None:
            return []
        values = list_match.group(1)
        return [
            item.strip().strip("'\"")
            for item in values.split(",")
            if item.strip()
        ]

    return {"includes": parse_list("includes"), "flags": parse_list("flags")}, body


def run_test(
    repo: Test262Repository,
    broiler_dll: str,
    path: str,
    harness_cache: dict[str, str],
) -> dict[str, object]:
    source = repo.read_text(path)
    metadata, body = parse_metadata(source)

    unsupported = sorted(UNSUPPORTED_FLAGS & set(metadata["flags"]))
    if unsupported:
        return {
            "path": path,
            "status": "skipped",
            "reason": f"unsupported flags: {', '.join(unsupported)}",
        }

    def harness_text(name: str) -> str:
        if name not in harness_cache:
            harness_cache[name] = repo.read_text(f"harness/{name}")
        return harness_cache[name]

    parts = [harness_text("assert.js"), harness_text("sta.js")]
    for include in metadata["includes"]:
        parts.append(harness_text(include))
    parts.append(body)

    with tempfile.NamedTemporaryFile(
        "w",
        suffix=".js",
        delete=False,
        dir="/tmp",
        encoding="utf-8",
    ) as handle:
        handle.write("\n".join(parts))
        script_path = handle.name

    try:
        process = subprocess.run(
            ["dotnet", broiler_dll, "--script-host", script_path],
            capture_output=True,
            text=True,
            check=False,
        )
    finally:
        os.unlink(script_path)

    if process.returncode == 0:
        return {"path": path, "status": "passed"}

    return {
        "path": path,
        "status": "failed",
        "stdout": process.stdout,
        "stderr": process.stderr,
    }


def build_summary(
    suite_ref: str,
    broiler_dll: str,
    requested_paths: list[str],
    expanded_paths: list[str],
    results: list[dict[str, object]],
) -> dict[str, object]:
    passed = sum(1 for result in results if result["status"] == "passed")
    failed = sum(1 for result in results if result["status"] == "failed")
    skipped = sum(1 for result in results if result["status"] == "skipped")
    executed = passed + failed
    return {
        "suiteRef": suite_ref,
        "broilerDll": broiler_dll,
        "requestedPaths": requested_paths,
        "expandedPaths": expanded_paths,
        "executed": executed,
        "passed": passed,
        "failed": failed,
        "skipped": skipped,
        "results": results,
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Run a pinned test262 subset against Broiler's script host."
    )
    parser.add_argument("paths", nargs="*", help="test262 file or directory paths")
    parser.add_argument(
        "--path-file",
        action="append",
        default=[],
        help="Text file containing additional test262 file paths, one per line",
    )
    parser.add_argument(
        "--suite-ref",
        default=DEFAULT_SUITE_REF,
        help="Pinned test262 commit SHA",
    )
    parser.add_argument(
        "--broiler-dll",
        default=DEFAULT_BROILER_DLL,
        help="Path to BroilerJS.dll",
    )
    parser.add_argument(
        "--output",
        help="Optional path for machine-readable JSON output",
    )
    args = parser.parse_args()

    repo = Test262Repository(args.suite_ref)
    requested_paths = list(args.paths)
    for path_file in args.path_file:
        for line in Path(path_file).read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if line and not line.startswith("#"):
                requested_paths.append(line)

    expanded_paths = repo.expand_paths(requested_paths)
    harness_cache: dict[str, str] = {}
    results = [
        run_test(repo, args.broiler_dll, path, harness_cache)
        for path in expanded_paths
    ]
    summary = build_summary(
        args.suite_ref,
        args.broiler_dll,
        requested_paths,
        expanded_paths,
        results,
    )

    if args.output:
        output_path = Path(args.output)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")

    print(json.dumps(summary, indent=2))
    return 1 if summary["failed"] else 0


if __name__ == "__main__":
    sys.exit(main())
