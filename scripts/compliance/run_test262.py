#!/usr/bin/env python3

from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import socket
import subprocess
import sys
import tarfile
import tempfile
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path


DEFAULT_SUITE_REF = "ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e"
REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_BROILER_DLL = str(
    REPO_ROOT / "Broiler.JS/Broiler.JavaScript/bin/Debug/net8.0/BroilerJS.dll"
)
TEMP_DIRECTORY = Path(tempfile.gettempdir()) / "broiler-test262"
UNSUPPORTED_FLAGS = {"module", "raw"}
USER_AGENT = "Broiler.JS compliance runner"
DOWNLOAD_TIMEOUT_SECONDS = 120
MAX_ARCHIVE_SIZE_BYTES = 256 * 1024 * 1024
HOST_HARNESS_INCLUDE_BLOCKERS = {"doneprintHandle.js"}
HOST_HARNESS_REFERENCE_PATTERN = re.compile(r"\$262(?:\b|\.)")
HOST_HARNESS_BLOCKER_NAME = "$262"


class Test262Repository:
    def __init__(self, suite_ref: str, suite_root: str | None = None):
        self.suite_ref = suite_ref
        self.suite_root = (
            Path(suite_root).resolve()
            if suite_root is not None and suite_root.strip()
            else None
        )
        self.contents_cache: dict[str, list[dict[str, object]]] = {}
        self.text_cache: dict[str, str] = {}
        self.tree_cache: list[str] | None = None

    def ensure_local_suite_root(self) -> Path:
        if self.suite_root is not None:
            return self.suite_root

        TEMP_DIRECTORY.mkdir(parents=True, exist_ok=True)
        cache_root = TEMP_DIRECTORY / "suite-cache"
        cache_root.mkdir(parents=True, exist_ok=True)

        extracted_root = cache_root / self.suite_ref
        if (extracted_root / "test").is_dir():
            self.suite_root = extracted_root
            return self.suite_root

        temporary_extract_root = cache_root / f"{self.suite_ref}.tmp"
        if temporary_extract_root.exists():
            shutil.rmtree(temporary_extract_root)
        temporary_extract_root.mkdir(parents=True)

        archive_path = TEMP_DIRECTORY / f"test262-{self.suite_ref}.tar.gz"
        if not archive_path.exists():
            url = f"https://codeload.github.com/tc39/test262/tar.gz/{self.suite_ref}"
            request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
            try:
                with urllib.request.urlopen(request, timeout=DOWNLOAD_TIMEOUT_SECONDS) as response:
                    content_length = response.headers.get("Content-Length")
                    if content_length is not None and int(content_length) > MAX_ARCHIVE_SIZE_BYTES:
                        raise RuntimeError(
                            f"test262 archive is too large to cache safely ({content_length} bytes)"
                        )
                    total_bytes = 0
                    with archive_path.open("wb") as handle:
                        while True:
                            chunk = response.read(1024 * 1024)
                            if not chunk:
                                break
                            total_bytes += len(chunk)
                            if total_bytes > MAX_ARCHIVE_SIZE_BYTES:
                                raise RuntimeError(
                                    "test262 archive exceeded the maximum cached size"
                                )
                            handle.write(chunk)
            except urllib.error.URLError as exc:
                if isinstance(exc.reason, socket.timeout):
                    raise RuntimeError(
                        f"Timed out downloading pinned test262 archive from {url}"
                    ) from exc
                raise RuntimeError(
                    f"Failed to download pinned test262 archive from {url}"
                ) from exc

        try:
            try:
                with tarfile.open(archive_path, "r:gz") as archive:
                    members = archive.getmembers()
                    resolved_extract_root = temporary_extract_root.resolve()
                    for member in members:
                        member_path = (temporary_extract_root / member.name).resolve()
                        try:
                            # Validate that the archive member stays within the extraction root.
                            member_path.relative_to(resolved_extract_root)
                        except ValueError as exc:
                            raise RuntimeError(
                                f"Unsafe path in test262 archive: {member.name}"
                            ) from exc
                    archive.extractall(temporary_extract_root)
            except tarfile.TarError as exc:
                raise RuntimeError(
                    f"Failed to extract cached test262 archive at {archive_path}"
                ) from exc

            extracted_children = [
                child for child in temporary_extract_root.iterdir() if child.is_dir()
            ]
            if len(extracted_children) != 1:
                raise RuntimeError(
                    f"Expected one top-level directory in test262 archive, found {len(extracted_children)}"
                )
            if extracted_root.exists():
                shutil.rmtree(extracted_root)
            extracted_children[0].rename(extracted_root)
        finally:
            if temporary_extract_root.exists():
                shutil.rmtree(temporary_extract_root)

        self.suite_root = extracted_root
        return self.suite_root

    def _resolve_local_path(self, path: str) -> Path:
        if self.suite_root is None:
            raise RuntimeError("No local suite root configured")

        resolved = (self.suite_root / path.strip("/")).resolve()
        try:
            resolved.relative_to(self.suite_root)
        except ValueError as exc:
            raise ValueError(f"Path escapes suite root: {path}") from exc
        return resolved

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
                "User-Agent": USER_AGENT,
            },
        )
        with urllib.request.urlopen(request) as response:
            return json.load(response)

    def list_paths(self, prefix: str = "", suffix: str = "") -> list[str]:
        if self.suite_root is not None:
            root = self._resolve_local_path(prefix or ".")
            if root.is_file():
                candidates = [root.relative_to(self.suite_root).as_posix()]
            elif root.is_dir():
                candidates = [
                    child.relative_to(self.suite_root).as_posix()
                    for child in root.rglob("*")
                    if child.is_file()
                ]
            else:
                raise FileNotFoundError(prefix)
        else:
            if prefix:
                local_suite_root = self.ensure_local_suite_root()
                root = (local_suite_root / prefix.strip("/")).resolve()
                if root.is_file():
                    candidates = [root.relative_to(local_suite_root).as_posix()]
                elif root.is_dir():
                    candidates = [
                        child.relative_to(local_suite_root).as_posix()
                        for child in root.rglob("*")
                        if child.is_file()
                    ]
                else:
                    raise FileNotFoundError(f"Path not found: {prefix}")
            else:
                if self.tree_cache is None:
                    url = (
                        f"https://api.github.com/repos/tc39/test262/git/trees/"
                        f"{urllib.parse.quote(self.suite_ref)}?recursive=1"
                    )
                    request = urllib.request.Request(
                        url,
                        headers={
                            "Accept": "application/vnd.github+json",
                            "User-Agent": USER_AGENT,
                        },
                    )
                    with urllib.request.urlopen(request) as response:
                        payload = json.load(response)
                    if payload.get("truncated"):
                        raise RuntimeError("Recursive test262 tree listing was truncated")
                    self.tree_cache = [
                        str(entry["path"])
                        for entry in payload.get("tree", [])
                        if entry.get("type") == "blob"
                    ]
                candidates = self.tree_cache

        return sorted(
            path for path in candidates if path.startswith(prefix) and path.endswith(suffix)
        )

    def read_text(self, path: str) -> str:
        if path not in self.text_cache:
            if self.suite_root is not None:
                self.text_cache[path] = self._resolve_local_path(path).read_text(
                    encoding="utf-8"
                )
            else:
                url = (
                    f"https://raw.githubusercontent.com/tc39/test262/"
                    f"{self.suite_ref}/{path}"
                )
                request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
                with urllib.request.urlopen(request) as response:
                    self.text_cache[path] = response.read().decode("utf-8")
        return self.text_cache[path]

    def expand_paths(self, paths: list[str]) -> list[str]:
        files: list[str] = []
        for path in paths:
            files.extend(self._expand_path(path.strip("/")))
        return sorted(dict.fromkeys(files))

    def _expand_path(self, path: str) -> list[str]:
        if self.suite_root is not None:
            local_path = self._resolve_local_path(path)
            if local_path.is_file():
                return [path]

            if not local_path.is_dir():
                raise FileNotFoundError(
                    f"Path not found: {path} (resolved to {local_path})"
                )

            return sorted(
                child.relative_to(self.suite_root).as_posix()
                for child in local_path.rglob("*.js")
                if child.is_file()
            )

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


def classify_test(
    source: str,
    repo: Test262Repository | None = None,
    harness_dependency_cache: dict[str, bool] | None = None,
) -> dict[str, object]:
    """Classify one test262 source file for raw script-host execution.

    Returns flags, unsupported flag names, optional negative metadata,
    optional host-harness blockers, and a boolean isScriptHostVerifiable flag.
    """
    metadata, _ = parse_metadata(source)
    flags = list(metadata["flags"])
    unsupported_flags = sorted(UNSUPPORTED_FLAGS & set(flags))
    negative = parse_negative_metadata(source)
    host_harness_blockers: list[str] = []
    if HOST_HARNESS_REFERENCE_PATTERN.search(source):
        host_harness_blockers.append(HOST_HARNESS_BLOCKER_NAME)
    if repo is not None and harness_dependency_cache is not None:
        for include in metadata["includes"]:
            if include not in harness_dependency_cache:
                if include in HOST_HARNESS_INCLUDE_BLOCKERS:
                    harness_dependency_cache[include] = True
                else:
                    harness_dependency_cache[include] = HOST_HARNESS_REFERENCE_PATTERN.search(
                        repo.read_text(f"harness/{include}")
                    ) is not None
            if harness_dependency_cache[include]:
                host_harness_blockers.append(f"include:{include}")

    return {
        "flags": flags,
        "unsupportedFlags": unsupported_flags,
        "negative": negative,
        "hostHarnessBlockers": sorted(set(host_harness_blockers)),
        "isScriptHostVerifiable": (
            not unsupported_flags
            and negative is None
            and not host_harness_blockers
        ),
    }


def collect_requested_paths(paths: list[str], path_files: list[str]) -> list[str]:
    requested_paths = list(paths)
    for path_file in path_files:
        for line in Path(path_file).read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if line and not line.startswith("#"):
                requested_paths.append(line)
    return requested_paths


def apply_shard(paths: list[str], shard_count: int, shard_index: int) -> list[str]:
    """Return one deterministic modulo-based shard from an ordered path list."""
    if shard_count <= 0:
        raise ValueError(f"shard_count must be greater than 0, got {shard_count}")
    if shard_index < 0 or shard_index >= shard_count:
        raise ValueError(
            f"shard_index must be between 0 and {shard_count - 1}, got {shard_index}"
        )

    if shard_count == 1:
        return list(paths)

    return [path for index, path in enumerate(paths) if index % shard_count == shard_index]


def select_paths(
    repo: Test262Repository,
    requested_paths: list[str],
    all_script_host_verifiable: bool,
    shard_count: int,
    shard_index: int,
) -> tuple[list[str], dict[str, object]]:
    """Select test paths plus metadata for requested or full script-host runs."""
    selection_mode = "requested"
    if all_script_host_verifiable:
        candidate_paths = repo.list_paths(prefix="test/", suffix=".js")
        harness_dependency_cache: dict[str, bool] = {}
        expanded_paths = [
            path
            for path in candidate_paths
            if classify_test(
                repo.read_text(path),
                repo,
                harness_dependency_cache,
            )["isScriptHostVerifiable"]
        ]
        selection_mode = "all-script-host-verifiable"
    else:
        candidate_paths = repo.expand_paths(requested_paths)
        expanded_paths = list(candidate_paths)

    sharded_paths = apply_shard(expanded_paths, shard_count, shard_index)
    selection = {
        "selectionMode": selection_mode,
        "candidateCount": len(candidate_paths),
        "selectedCountBeforeSharding": len(expanded_paths),
        "shardCount": shard_count,
        "shardIndex": shard_index,
    }
    return sharded_paths, selection


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
    is_async = "async" in metadata["flags"]
    is_only_strict = "onlyStrict" in metadata["flags"]

    def harness_text(name: str) -> str:
        if name not in harness_cache:
            harness_cache[name] = repo.read_text(f"harness/{name}")
        return harness_cache[name]

    parts = [harness_text("assert.js"), harness_text("sta.js")]
    for include in metadata["includes"]:
        parts.append(harness_text(include))
    if is_async:
        parts.append(
            """
const __broilerDonePromise = new Promise((resolve, reject) => {
  let settled = false;
  globalThis.$DONE = function(error) {
    if (settled) {
      reject(new Error("$DONE called multiple times"));
      return;
    }
    settled = true;
    if (error === undefined) {
      resolve(undefined);
      return;
    }
    reject(error);
  };
});
""".strip()
        )
    if is_only_strict:
        parts.append('"use strict";')
    parts.append(body)
    if is_async:
        parts.append("__broilerDonePromise")

    TEMP_DIRECTORY.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(
        "w",
        suffix=".js",
        delete=False,
        dir=TEMP_DIRECTORY,
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
    selection: dict[str, object],
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
        "selectionMode": selection["selectionMode"],
        "candidateCount": selection["candidateCount"],
        "selectedCountBeforeSharding": selection["selectedCountBeforeSharding"],
        "shardCount": selection["shardCount"],
        "shardIndex": selection["shardIndex"],
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
        "--suite-root",
        help="Optional path to a local test262 checkout",
    )
    parser.add_argument(
        "--output",
        help="Optional path for machine-readable JSON output",
    )
    parser.add_argument(
        "--all-script-host-verifiable",
        action="store_true",
        help="Discover and run every current script-host-verifiable test262 file",
    )
    parser.add_argument(
        "--shard-count",
        type=int,
        default=1,
        help="Total number of deterministic shards to split the selected test set into",
    )
    parser.add_argument(
        "--shard-index",
        type=int,
        default=0,
        help="Zero-based shard index to execute from the selected test set",
    )
    args = parser.parse_args()

    repo = Test262Repository(args.suite_ref, args.suite_root)
    TEMP_DIRECTORY.mkdir(parents=True, exist_ok=True)
    requested_paths = collect_requested_paths(args.paths, args.path_file)
    if args.all_script_host_verifiable and requested_paths:
        parser.error("--all-script-host-verifiable cannot be combined with explicit paths")

    try:
        expanded_paths, selection = select_paths(
            repo,
            requested_paths,
            args.all_script_host_verifiable,
            args.shard_count,
            args.shard_index,
        )
    except ValueError as exc:
        parser.error(str(exc))

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
        selection,
    )

    if args.output:
        output_path = Path(args.output)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")

    print(json.dumps(summary, indent=2))
    return 1 if summary["failed"] > 0 else 0


if __name__ == "__main__":
    sys.exit(main())
