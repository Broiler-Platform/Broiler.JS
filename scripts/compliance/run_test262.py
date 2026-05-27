#!/usr/bin/env python3

from __future__ import annotations

import argparse
import concurrent.futures
import json
import math
import os
import random
import re
import shutil
import signal
import socket
import subprocess
import sys
import tarfile
import tempfile
import threading
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

try:
    import resource
except ImportError:  # pragma: no cover - only exercised on non-POSIX platforms
    resource = None


DEFAULT_SUITE_REF = "ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e"
REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_BROILER_DLL = str(
    REPO_ROOT / "Broiler.JS/Broiler.JavaScript/bin/Debug/net8.0/BroilerJS.dll"
)
ALL_SHARDS = -1
TEMP_DIRECTORY = Path(tempfile.gettempdir()) / "broiler-test262"
UNSUPPORTED_FLAGS = {"module", "raw"}
UNSUPPORTED_FEATURES = {
    "Atomics",
    "SharedArrayBuffer",
    "Temporal",
    "dynamic-import",
    "explicit-resource-management",
    "resizable-arraybuffer",
}
USER_AGENT = "Broiler.JS compliance runner"
DOWNLOAD_TIMEOUT_SECONDS = 120
MAX_ARCHIVE_SIZE_BYTES = 256 * 1024 * 1024
HOST_HARNESS_INCLUDE_BLOCKERS = {"doneprintHandle.js"}
HOST_HARNESS_REFERENCE_PATTERN = re.compile(r"\$262(?:\b|\.)")
HOST_HARNESS_BLOCKER_NAME = "$262"
DEFAULT_TEST_TIMEOUT_SECONDS = 30.0
POST_TERMINATION_TIMEOUT_SECONDS = 5.0


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
        return {"features": [], "includes": [], "flags": []}, source

    block = match.group(1)
    body = source[: match.start()] + source[match.end() :]

    def parse_list(name: str) -> list[str]:
        inline_match = re.search(rf"(?m)^{name}:\s*\[(.*?)\]\s*$", block, re.S)
        if inline_match is not None:
            values = inline_match.group(1)
            return [
                item.strip().strip("'\"")
                for item in values.split(",")
                if item.strip()
            ]

        # Match YAML-style list fields like:
        # name:
        #   - value
        #   - other
        block_match = re.search(rf"(?m)^{name}:\s*\n((?:[ \t]*-[ \t]*.*(?:\n|$))*)", block)
        if block_match is None:
            return []

        return [
            line.strip()[2:].strip().strip("'\"")
            for line in block_match.group(1).splitlines()
            if line.strip().startswith("- ")
        ]

    return {
        "features": parse_list("features"),
        "includes": parse_list("includes"),
        "flags": parse_list("flags"),
    }, body


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
    features = list(metadata["features"])
    unsupported_flags = sorted(UNSUPPORTED_FLAGS & set(flags))
    unsupported_features = sorted(UNSUPPORTED_FEATURES & set(features))
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
        "features": features,
        "unsupportedFlags": unsupported_flags,
        "unsupportedFeatures": unsupported_features,
        "negative": negative,
        "hostHarnessBlockers": sorted(set(host_harness_blockers)),
        "isScriptHostVerifiable": (
            not unsupported_flags
            and not unsupported_features
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


def positive_float(value: str) -> float:
    parsed = float(value)
    if parsed <= 0:
        raise argparse.ArgumentTypeError(f"value must be positive, got {value}")
    return parsed


def non_negative_int(value: str) -> int:
    parsed = int(value)
    if parsed < 0:
        raise argparse.ArgumentTypeError(f"value must be non-negative, got {value}")
    return parsed


def apply_shard(paths: list[str], shard_count: int, shard_index: int) -> list[str]:
    """Return one deterministic modulo-based shard from an ordered path list."""
    if shard_count <= 0:
        raise ValueError(f"shard_count must be greater than 0, got {shard_count}")
    if shard_index == ALL_SHARDS:
        return list(paths)
    if shard_index < ALL_SHARDS or shard_index >= shard_count:
        raise ValueError(
            f"shard_index must be -1 or between 0 and {shard_count - 1}, got {shard_index}"
        )

    if shard_count == 1:
        return list(paths)

    return [path for index, path in enumerate(paths) if index % shard_count == shard_index]


def log_progress(message: str) -> None:
    print(f"[test262] {message}", file=sys.stderr, flush=True)


def calculate_progress_log_interval(total: int) -> int | None:
    if total <= 0:
        return None
    if total < 25:
        return total
    return max(25, total // 10)


def format_shard_label(shard_index: int, shard_count: int) -> str:
    if shard_index == ALL_SHARDS:
        return f"all/{shard_count}"
    return f"{shard_index + 1}/{shard_count}"


def create_process_limit_setup(
    timeout_seconds: float,
    memory_limit_mb: int,
):
    if os.name != "posix" or resource is None:
        return None

    # RLIMIT_CPU is whole-second based, so sub-second wall-clock timeouts rely only
    # on communicate(timeout=...) for enforcement.
    cpu_limit_seconds = int(math.ceil(timeout_seconds)) if timeout_seconds >= 1 else None
    cpu_hard_limit_seconds = (
        cpu_limit_seconds + 5 if cpu_limit_seconds is not None else None
    )
    memory_limit_bytes = memory_limit_mb * 1024 * 1024 if memory_limit_mb > 0 else None

    def limit_process_resources() -> None:
        if hasattr(resource, "RLIMIT_CORE"):
            resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
        if cpu_limit_seconds is not None and hasattr(resource, "RLIMIT_CPU"):
            resource.setrlimit(
                resource.RLIMIT_CPU,
                (cpu_limit_seconds, cpu_hard_limit_seconds),
            )
        if memory_limit_bytes is not None:
            for limit_name in ("RLIMIT_AS", "RLIMIT_DATA"):
                if hasattr(resource, limit_name):
                    limit = getattr(resource, limit_name)
                    resource.setrlimit(limit, (memory_limit_bytes, memory_limit_bytes))

    return limit_process_resources


def terminate_process_tree(process: subprocess.Popen[str]) -> None:
    if os.name == "posix":
        try:
            os.killpg(process.pid, signal.SIGKILL)
            return
        except ProcessLookupError:
            return
    process.kill()


def _load_fragile_paths(failure_file: Path | None = None) -> set[str]:
    """Return the set of test paths recorded as historically fragile.

    Reads the saved failure manifest at *failure_file* (defaulting to the
    repository-local ``test262-failures.txt``).  Lines
    starting with ``#`` and blank lines are ignored.
    """
    if failure_file is None:
        failure_file = REPO_ROOT / "scripts" / "compliance" / "test262-failures.txt"
    if not failure_file.is_file():
        return set()
    paths: set[str] = set()
    for line in failure_file.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if stripped and not stripped.startswith("#"):
            paths.add(stripped)
    return paths


def _detect_recently_changed_directories() -> set[str]:
    """Return test262 directory prefixes that map to recently changed Broiler source areas.

    Runs ``git diff --name-only HEAD~1`` (or falls back to an empty set if
    git is unavailable or there is no previous commit) and maps changed
    ``Broiler.JavaScript.*`` project directories to likely test262 prefixes.
    """
    area_to_test_dirs: dict[str, list[str]] = {
        "Broiler.JavaScript.Parser": [
            "test/language/",
            "test/staging/sm/syntax/",
        ],
        "Broiler.JavaScript.Compiler": [
            "test/language/",
        ],
        "Broiler.JavaScript.Runtime": [
            "test/language/",
            "test/built-ins/",
        ],
        "Broiler.JavaScript.BuiltIns": [
            "test/built-ins/",
            "test/annexB/built-ins/",
        ],
        "Broiler.JavaScript.Modules": [
            "test/language/",
        ],
        "Broiler.JavaScript.Engine": [
            "test/language/",
            "test/built-ins/",
        ],
    }

    try:
        result = subprocess.run(
            ["git", "diff", "--name-only", "HEAD~1"],
            capture_output=True,
            text=True,
            timeout=10,
            cwd=REPO_ROOT,
        )
        if result.returncode != 0:
            return set()
    except (FileNotFoundError, subprocess.TimeoutExpired, OSError):
        return set()

    changed_files = result.stdout.strip().splitlines()
    prefixes: set[str] = set()
    for changed in changed_files:
        for area, dirs in area_to_test_dirs.items():
            if area in changed:
                prefixes.update(dirs)
    return prefixes


def _prioritize_paths(
    paths: list[str],
    fragile_paths: set[str],
    changed_prefixes: set[str],
) -> tuple[list[str], int]:
    """Reorder *paths* so historically fragile and recently-changed-area tests come first.

    Returns the reordered list and the count of paths that were promoted.
    The relative order within the priority group and within the remainder
    is preserved.
    """
    priority: list[str] = []
    rest: list[str] = []
    for path in paths:
        if path in fragile_paths or any(path.startswith(p) for p in changed_prefixes):
            priority.append(path)
        else:
            rest.append(path)
    return priority + rest, len(priority)


def select_paths(
    repo: Test262Repository,
    requested_paths: list[str],
    all_script_host_verifiable: bool,
    shard_count: int,
    shard_index: int,
    shuffle_seed: int | None = None,
    include_negative: bool = False,
    prioritize_fragile: bool = False,
) -> tuple[list[str], dict[str, object]]:
    """Select test paths plus metadata for requested or full script-host runs."""
    selection_mode = "requested"
    if all_script_host_verifiable:
        candidate_paths = repo.list_paths(prefix="test/", suffix=".js")
        harness_dependency_cache: dict[str, bool] = {}
        expanded_paths = [
            path
            for path in candidate_paths
            if _is_selectable(
                repo.read_text(path),
                repo,
                harness_dependency_cache,
                include_negative,
            )
        ]
        selection_mode = "all-script-host-verifiable"
    else:
        candidate_paths = repo.expand_paths(requested_paths)
        expanded_paths = list(candidate_paths)

    promoted_count = 0
    if prioritize_fragile:
        fragile_paths = _load_fragile_paths()
        changed_prefixes = _detect_recently_changed_directories()
        expanded_paths, promoted_count = _prioritize_paths(
            expanded_paths, fragile_paths, changed_prefixes,
        )

    if shuffle_seed is not None:
        random.Random(shuffle_seed).shuffle(expanded_paths)

    sharded_paths = apply_shard(expanded_paths, shard_count, shard_index)
    selection = {
        "selectionMode": selection_mode,
        "candidateCount": len(candidate_paths),
        "selectedCountBeforeSharding": len(expanded_paths),
        "shardCount": shard_count,
        "shardIndex": shard_index,
        "shuffleSeed": shuffle_seed,
        "includeNegative": include_negative,
        "prioritizeFragile": prioritize_fragile,
        "promotedCount": promoted_count,
    }
    return sharded_paths, selection


def _is_selectable(
    source: str,
    repo: Test262Repository | None,
    harness_dependency_cache: dict[str, bool] | None,
    include_negative: bool,
) -> bool:
    """Return True when the test file is eligible for the current selection."""
    classification = classify_test(source, repo, harness_dependency_cache)
    if classification["isScriptHostVerifiable"]:
        return True
    if include_negative and classification["negative"] is not None:
        # Accept negative tests that are blocked only by negative metadata (not
        # by unsupported flags/features or host-harness blockers).
        return (
            not classification["unsupportedFlags"]
            and not classification["unsupportedFeatures"]
            and not classification["hostHarnessBlockers"]
        )
    return False


def run_test(
    repo: Test262Repository,
    broiler_dll: str,
    path: str,
    harness_cache: dict[str, str],
    timeout_seconds: float,
    memory_limit_mb: int,
    include_negative: bool = False,
) -> dict[str, object]:
    source = repo.read_text(path)
    metadata, body = parse_metadata(source)

    unsupported = sorted(UNSUPPORTED_FLAGS & set(metadata["flags"]))
    unsupported_features = sorted(UNSUPPORTED_FEATURES & set(metadata["features"]))
    if unsupported or unsupported_features:
        reasons: list[str] = []
        if unsupported:
            reasons.append(f"unsupported flags: {', '.join(unsupported)}")
        if unsupported_features:
            reasons.append(
                f"unsupported features: {', '.join(unsupported_features)}"
            )
        return {
            "path": path,
            "status": "skipped",
            "reason": "; ".join(reasons),
        }

    negative = parse_negative_metadata(source)
    if negative is not None and not include_negative:
        return {
            "path": path,
            "status": "skipped",
            "reason": "negative metadata test (use --include-negative to run)",
        }

    is_async = "async" in metadata["flags"]
    is_only_strict = "onlyStrict" in metadata["flags"]

    def harness_text(name: str) -> str:
        if name not in harness_cache:
            harness_cache[name] = repo.read_text(f"harness/{name}")
        return harness_cache[name]

    parts = []
    if is_only_strict:
        parts.append('"use strict";')
    parts.extend([harness_text("assert.js"), harness_text("sta.js")])
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
        process = subprocess.Popen(
            ["dotnet", broiler_dll, "--script-host", script_path],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            start_new_session=os.name == "posix",
            preexec_fn=create_process_limit_setup(timeout_seconds, memory_limit_mb),
        )
        try:
            stdout, stderr = process.communicate(timeout=timeout_seconds)
        except subprocess.TimeoutExpired:
            terminate_process_tree(process)
            try:
                stdout, stderr = process.communicate(
                    timeout=POST_TERMINATION_TIMEOUT_SECONDS
                )
            except subprocess.TimeoutExpired:
                process.kill()
                stdout, stderr = process.communicate()
            return {
                "path": path,
                "status": "timedOut",
                "reason": f"timed out after {timeout_seconds:g}s",
                "stdout": stdout or "",
                "stderr": stderr or "",
            }
    finally:
        os.unlink(script_path)

    if negative is not None:
        # Negative tests expect an error.  If the engine exited with a
        # non-zero status, check whether stderr mentions the expected
        # error type; if it does, the test passes.
        expected_type = negative.get("type", "")
        if process.returncode != 0 and expected_type and expected_type in (stderr or ""):
            return {"path": path, "status": "passed", "negative": True}
        if process.returncode == 0:
            return {
                "path": path,
                "status": "failed",
                "reason": f"negative test expected {expected_type} but succeeded",
                "stdout": stdout,
                "stderr": stderr,
            }
        return {
            "path": path,
            "status": "failed",
            "reason": f"negative test expected {expected_type} but got different error",
            "stdout": stdout,
            "stderr": stderr,
        }

    if process.returncode == 0:
        return {"path": path, "status": "passed"}

    return {
        "path": path,
        "status": "failed",
        "stdout": stdout,
        "stderr": stderr,
    }


def build_summary(
    suite_ref: str,
    broiler_dll: str,
    requested_paths: list[str],
    expanded_paths: list[str],
    results: list[dict[str, object]],
    selection: dict[str, object],
    test_timeout_seconds: float,
    memory_limit_mb: int,
    max_workers: int = 1,
    shuffle_seed: int | None = None,
    include_negative: bool = False,
    prioritize_fragile: bool = False,
) -> dict[str, object]:
    passed = sum(1 for result in results if result["status"] == "passed")
    failed = sum(1 for result in results if result["status"] == "failed")
    skipped = sum(1 for result in results if result["status"] == "skipped")
    timed_out = sum(1 for result in results if result["status"] == "timedOut")
    failed_paths = [result["path"] for result in results if result["status"] == "failed"]
    timed_out_paths = [result["path"] for result in results if result["status"] == "timedOut"]
    executed = passed + failed + timed_out
    return {
        "suiteRef": suite_ref,
        "broilerDll": broiler_dll,
        "testTimeoutSeconds": test_timeout_seconds,
        "memoryLimitMb": memory_limit_mb,
        "maxWorkers": max_workers,
        "shuffleSeed": shuffle_seed,
        "includeNegative": include_negative,
        "prioritizeFragile": prioritize_fragile,
        "promotedCount": selection.get("promotedCount", 0),
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
        "failedPaths": failed_paths,
        "skipped": skipped,
        "timedOut": timed_out,
        "timedOutPaths": timed_out_paths,
        "results": results,
    }


def run_selected_tests(
    repo: Test262Repository,
    broiler_dll: str,
    expanded_paths: list[str],
    selection: dict[str, object],
    timeout_seconds: float,
    memory_limit_mb: int,
    max_workers: int = 1,
    include_negative: bool = False,
) -> list[dict[str, object]]:
    total = len(expanded_paths)
    if total == 0:
        log_progress("No tests matched the current selection.")
        return []

    shard_label = format_shard_label(
        int(selection["shardIndex"]),
        int(selection["shardCount"]),
    )
    log_progress(f"Running {total} test(s) for shard {shard_label}.")
    memory_limit_label = f"{memory_limit_mb} MiB" if memory_limit_mb > 0 else "disabled"
    log_progress(
        f"Per-test timeout is {timeout_seconds:g}s; POSIX memory cap is {memory_limit_label}."
    )
    if max_workers > 1:
        log_progress(f"Using {max_workers} parallel worker(s).")

    interval = calculate_progress_log_interval(total)

    if max_workers <= 1:
        return _run_tests_serial(
            repo, broiler_dll, expanded_paths, timeout_seconds,
            memory_limit_mb, interval, total, include_negative,
        )
    return _run_tests_parallel(
        repo, broiler_dll, expanded_paths, timeout_seconds,
        memory_limit_mb, interval, total, max_workers, include_negative,
    )


def _run_tests_serial(
    repo: Test262Repository,
    broiler_dll: str,
    expanded_paths: list[str],
    timeout_seconds: float,
    memory_limit_mb: int,
    interval: int | None,
    total: int,
    include_negative: bool,
) -> list[dict[str, object]]:
    harness_cache: dict[str, str] = {}
    results: list[dict[str, object]] = []
    passed = 0
    failed = 0
    skipped = 0
    timed_out = 0

    for index, path in enumerate(expanded_paths, start=1):
        result = run_test(
            repo,
            broiler_dll,
            path,
            harness_cache,
            timeout_seconds,
            memory_limit_mb,
            include_negative,
        )
        results.append(result)

        status = str(result["status"])
        if status == "passed":
            passed += 1
        elif status == "failed":
            failed += 1
            log_progress(f"Failure {failed} at {path}")
        elif status == "timedOut":
            timed_out += 1
            log_progress(f"Timeout {timed_out} at {path} after {timeout_seconds:g}s")
        else:
            skipped += 1

        if interval is not None and (index % interval == 0 or index == total):
            log_progress(
                f"Completed {index}/{total} test(s) "
                f"(passed={passed}, failed={failed}, skipped={skipped}, timedOut={timed_out})."
            )

    return results


def _run_tests_parallel(
    repo: Test262Repository,
    broiler_dll: str,
    expanded_paths: list[str],
    timeout_seconds: float,
    memory_limit_mb: int,
    interval: int | None,
    total: int,
    max_workers: int,
    include_negative: bool,
) -> list[dict[str, object]]:
    # Each worker gets its own harness cache to avoid contention.
    results: list[dict[str, object]] = [{}] * total  # type: ignore[arg-type]
    lock = threading.Lock()
    completed = 0
    passed = 0
    failed = 0
    skipped = 0
    timed_out = 0

    def _worker(index: int, path: str) -> tuple[int, dict[str, object]]:
        harness_cache: dict[str, str] = {}
        result = run_test(
            repo, broiler_dll, path, harness_cache,
            timeout_seconds, memory_limit_mb, include_negative,
        )
        return index, result

    with concurrent.futures.ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {
            executor.submit(_worker, idx, path): idx
            for idx, path in enumerate(expanded_paths)
        }
        for future in concurrent.futures.as_completed(futures):
            idx, result = future.result()
            results[idx] = result

            with lock:
                completed += 1
                status = str(result["status"])
                if status == "passed":
                    passed += 1
                elif status == "failed":
                    failed += 1
                    log_progress(f"Failure {failed} at {result['path']}")
                elif status == "timedOut":
                    timed_out += 1
                    log_progress(
                        f"Timeout {timed_out} at {result['path']} after {timeout_seconds:g}s"
                    )
                else:
                    skipped += 1

                if interval is not None and (
                    completed % interval == 0 or completed == total
                ):
                    log_progress(
                        f"Completed {completed}/{total} test(s) "
                        f"(passed={passed}, failed={failed}, "
                        f"skipped={skipped}, timedOut={timed_out})."
                    )

    return results


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
        help="Zero-based shard index to execute from the selected test set, or -1 to run all shards",
    )
    parser.add_argument(
        "--test-timeout-seconds",
        type=positive_float,
        default=DEFAULT_TEST_TIMEOUT_SECONDS,
        help="Per-test wall-clock timeout in seconds",
    )
    parser.add_argument(
        "--memory-limit-mb",
        type=non_negative_int,
        default=0,
        help="Optional POSIX per-test address-space limit in MiB; pass 0 to disable",
    )
    parser.add_argument(
        "--max-workers",
        type=int,
        default=1,
        help="Number of parallel worker threads for running tests (default 1 = serial)",
    )
    parser.add_argument(
        "--shuffle-seed",
        type=int,
        default=None,
        help="Seed for deterministic random shuffling of test order before sharding; omit to keep upstream order",
    )
    parser.add_argument(
        "--include-negative",
        action="store_true",
        help="Include negative-metadata tests and verify expected error types",
    )
    parser.add_argument(
        "--prioritize-fragile",
        action="store_true",
        help="Prioritize historically fragile and recently changed areas to surface regressions earlier",
    )
    args = parser.parse_args()

    max_workers = max(1, args.max_workers)

    log_progress(
        f"Starting test262 run for suite ref {args.suite_ref}"
        + (f" using local suite root {args.suite_root}" if args.suite_root else "")
    )

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
            shuffle_seed=args.shuffle_seed,
            include_negative=args.include_negative,
            prioritize_fragile=args.prioritize_fragile,
        )
    except ValueError as exc:
        parser.error(str(exc))

    shard_label = format_shard_label(selection["shardIndex"], selection["shardCount"])
    log_progress(
        f"Selected {len(expanded_paths)} runnable test(s) for shard {shard_label} "
        f"from {selection['selectedCountBeforeSharding']} selected file(s) "
        f"and {selection['candidateCount']} candidate path(s) "
        f"using mode {selection['selectionMode']}."
    )
    log_progress(f"Using Broiler script host at {args.broiler_dll}")
    if selection.get("promotedCount", 0) > 0:
        log_progress(
            f"Fragile-area prioritization promoted {selection['promotedCount']} "
            f"test(s) to the front of the selection."
        )
    if args.memory_limit_mb > 0 and (os.name != "posix" or resource is None):
        log_progress(
            "POSIX resource limits are unavailable on this platform; "
            "continuing with timeout/process isolation only."
        )

    results = run_selected_tests(
        repo,
        args.broiler_dll,
        expanded_paths,
        selection,
        args.test_timeout_seconds,
        args.memory_limit_mb,
        max_workers=max_workers,
        include_negative=args.include_negative,
    )
    summary = build_summary(
        args.suite_ref,
        args.broiler_dll,
        requested_paths,
        expanded_paths,
        results,
        selection,
        args.test_timeout_seconds,
        args.memory_limit_mb,
        max_workers=max_workers,
        shuffle_seed=args.shuffle_seed,
        include_negative=args.include_negative,
        prioritize_fragile=args.prioritize_fragile,
    )

    if args.output:
        output_path = Path(args.output)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
        log_progress(f"Wrote machine-readable summary to {output_path}")

    log_progress(
        f"Finished test262 run: executed={summary['executed']}, "
        f"passed={summary['passed']}, failed={summary['failed']}, "
        f"skipped={summary['skipped']}, timedOut={summary['timedOut']}"
    )
    print(json.dumps(summary, indent=2))
    has_failures = summary["failed"] > 0 or summary["timedOut"] > 0
    return 1 if has_failures else 0


if __name__ == "__main__":
    sys.exit(main())
