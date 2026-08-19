#!/usr/bin/env python3

from __future__ import annotations

import argparse
import concurrent.futures
import fnmatch
import json
import math
import os
import platform
import queue
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
import time
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
    REPO_ROOT / "Broiler.JS/Broiler.JavaScript/bin/Debug/net10.0/BroilerJS.dll"
)
ALL_SHARDS = -1
TEMP_DIRECTORY = Path(tempfile.gettempdir()) / "broiler-test262"
UNSUPPORTED_FLAGS = {"module", "raw"}
# Broiler's single agent can block: Atomics.wait reports a value mismatch or a timeout
# instead of suspending (there is no second agent, and notify wakes no one), i.e. its
# [[CanBlock]] is true. Tests guarded by the CanBlockIsFalse flag describe a host whose
# agent cannot block — AgentCanSuspend() is false, so Atomics.wait must throw TypeError —
# which is not this agent, so those tests do not apply and are skipped. The complementary
# CanBlockIsTrue tests, which exercise the actual wait behaviour, do apply and run.
HOST_AGENT_CAN_BLOCK = True
INAPPLICABLE_FLAGS = {"CanBlockIsFalse"} if HOST_AGENT_CAN_BLOCK else {"CanBlockIsTrue"}
SKIPPED_FLAGS = UNSUPPORTED_FLAGS | INAPPLICABLE_FLAGS
TEST_FIXTURE_SUFFIX = "_FIXTURE.js"
# No test262 feature is excluded: every feature is treated as either already
# supported or something that must be implemented rather than skipped, so feature
# metadata never causes a test to be filtered out or marked "skipped". (Kept as an
# empty set so the shared classification contract — also consumed by
# audit_test262.py — stays intact.)
UNSUPPORTED_FEATURES: set[str] = set()
# Individual test262 files that are demonstrably incorrect AT THE PINNED SUITE REF
# (DEFAULT_SUITE_REF) and have since been corrected upstream. These are skipped — not
# because the feature is unsupported (it is implemented and spec-correct here), but
# because the test itself asserts something impossible, so no conformant engine can
# pass it at this ref. Each entry must cite the upstream fix; revisit when the pinned
# ref is bumped past the fix (the entry then becomes dead and should be removed).
KNOWN_INCORRECT_TESTS: dict[str, str] = {
    # The (start, end) coercion table asserts `dest.byteLength === intLength` where
    # intLength = end - start WITHOUT the spec's `max(final - first, 0)` clamp (step 9
    # of ArrayBuffer.prototype.sliceToImmutable), so e.g. sliceToImmutable(1, 0) expects
    # byteLength === -1 — impossible, and self-contradictory with the test's own
    # `compareArray(new Uint8Array(dest), [])` (0 bytes) assertion in the same iteration.
    # Fixed upstream by wrapping intLength in `Math.max(0, ...)` (tc39/test262; the version
    # at HEAD clamps). Broiler returns the spec-correct 0.
    "test/built-ins/ArrayBuffer/prototype/sliceToImmutable/argument-coercion.js": (
        "buggy at pinned ref: asserts byteLength === end - start without the spec "
        "max(final - first, 0) clamp (negative for start > end); fixed upstream via "
        "Math.max(0, ...). Engine is spec-correct."
    ),
}
USER_AGENT = "Broiler.JS compliance runner"
DOWNLOAD_TIMEOUT_SECONDS = 120
MAX_ARCHIVE_SIZE_BYTES = 256 * 1024 * 1024
HOST_HARNESS_INCLUDE_BLOCKERS = {"doneprintHandle.js"}
HOST_HARNESS_REFERENCE_PATTERN = re.compile(r"\$262(?:\b|\.)")
HOST_HARNESS_BLOCKER_NAME = "$262"
DEFAULT_TEST_TIMEOUT_SECONDS = 30.0
DEFAULT_MINIFIER_TIMEOUT_SECONDS = 30.0
TERSER_PROFILE = "test262-safe-mangle-v2"
TERSER_VARIANT = "terser"
CLOSURE_PROFILE = "test262-closure-advanced-v1"
CLOSURE_VARIANT = "closure"
ORIGINAL_VARIANT = "original"
TERSER_WORKER = Path(__file__).with_name("terser_worker.cjs")
CLOSURE_WORKER = Path(__file__).with_name("closure_worker.cjs")
MINIFIER_VARIANTS = (TERSER_VARIANT, CLOSURE_VARIANT)
# The transformation each variant applies, recorded on every report so a merge can refuse
# to combine shards that ran different profiles and a reader can tell what a failure was
# produced by. Terser's profile is a conservative mangle; Closure's is the full
# ADVANCED_OPTIMIZATIONS package (variable AND property renaming, dead-code removal,
# inlining) against the ES2026 draft standard, which Closure spells ECMASCRIPT_NEXT — see
# closure_worker.cjs.
MINIFIER_OPTIONS: dict[str, dict[str, object]] = {
    TERSER_VARIANT: {
        "compress": False,
        "mangle": True,
        "mangleProperties": False,
        "mangleEval": False,
        "reserveQuotedNames": True,
        "toplevel": False,
        "keepFunctionNames": True,
        "keepClassNames": True,
        "module": False,
        "asciiOnly": False,
        "comments": False,
        "semicolons": True,
    },
    CLOSURE_VARIANT: {
        "compilationLevel": "ADVANCED_OPTIMIZATIONS",
        "languageIn": "ECMASCRIPT_NEXT",
        "languageOut": "ECMASCRIPT_NEXT",
        "ecmaScriptYear": 2026,
        "mangle": True,
        "mangleProperties": True,
        "harnessExterns": True,
        "hostExterns": True,
        "reserveQuotedNames": True,
        "emitUseStrict": False,
        "isolationMode": "NONE",
        "processCommonJsModules": False,
        "rewritePolyfills": False,
        "warningLevel": "QUIET",
    },
}
MINIFIER_SOURCE_SENSITIVE_PREFIXES = (
    "test/built-ins/Function/prototype/toString/",
)
FUNCTION_TOSTRING_REFERENCE = re.compile(
    r"\bFunction\s*\.\s*prototype\s*\.\s*toString\b"
)
QUOTED_STRING = re.compile(r"(['\"])((?:\\.|(?!\1)[^\\\r\n])*)\1")
WHITESPACE_RUN = re.compile(r"\s+")
# A quoted function or class definition, matched against a literal whose whitespace has
# been collapsed: `function* g() { … }`, `async function (a, b) { … }`, `class C extends D
# { … }`. See quoted_source_text.
QUOTED_DEFINITION = re.compile(r"(?:async)?(?:function\*?|class)[A-Za-z0-9_$]*[({]")
# The shortest quoted code fragment worth reading as a copy of the source: below this a
# collapsed literal is common punctuation that says nothing about what the test measures.
MINIMUM_QUOTED_SOURCE_LENGTH = 8
# How far either side of such a literal a `toString` still counts as standing next to it:
# wide enough for the assertion that compares the two — `assert.sameValue(f.toString(),`
# and a line break — and no wider, so a `toString` in a neighbouring statement does not
# make an assertion message that happens to quote a definition read as source text.
QUOTED_SOURCE_NEIGHBOURHOOD = 48
POST_TERMINATION_TIMEOUT_SECONDS = 5.0
INTERNAL_EXEC_WITH_LIMITS = "--_exec-with-test262-limits"
# The engine consulted about a FAILING minified case before it is attributed to Broiler.
# Node is already a hard requirement of the Terser variant (it runs the minifier), so this
# adds a second opinion without adding a dependency. See ReferenceEngine.
REFERENCE_ENGINE = "node"
# The reference engine is handed each program through this launcher rather than being
# pointed at the file: it compiles the program as the SCRIPT test262 wrote, in the real
# global scope, instead of inside Node's CommonJS module wrapper. See reference_host.cjs.
REFERENCE_HOST = Path(__file__).with_name("reference_host.cjs")
DEFAULT_REFERENCE_ENGINE_TIMEOUT_SECONDS = 30.0
# Closure's ADVANCED renames every property name it has no externs for, and the externs it
# ships describe the standard surface the compiler's own release knows about. A host that
# implements more than that — Temporal, iterator helpers, the newest ArrayBuffer methods —
# is invisible to it, so `Temporal.Duration` compiles to `Temporal.h` and the program that
# comes out no longer reaches the engine's own API. A production ADVANCED build answers
# that with externs for its host; so does this variant, and it asks the engine under test
# what to put in them rather than keeping a list that would rot against the engine.
HOST_SURFACE_BEGIN = "__broilerHostSurfaceBegin__"
HOST_SURFACE_END = "__broilerHostSurfaceEnd__"
HOST_EXTERNS_HOLDER = "__broilerTest262HostNames"
HOST_EXTERNS_NAME = "test262-host-surface.js"
DEFAULT_HOST_SURFACE_TIMEOUT_SECONDS = 120.0
HOST_SURFACE_IDENTIFIER = re.compile(r"[A-Za-z_$][A-Za-z0-9_$]*")
# Walks everything reachable from `globalThis` through data properties and prototypes,
# reporting every own property name it passes. An accessor is named but never invoked: what
# it would return is not the harness's business, and several of them throw when the receiver
# is the prototype that declares them.
HOST_SURFACE_SCRIPT = """
(function () {
  var seenObjects = new Set();
  var names = new Set();
  var pending = [globalThis];
  seenObjects.add(globalThis);
  while (pending.length > 0) {
    var target = pending.pop();
    var keys;
    try {
      keys = Object.getOwnPropertyNames(target);
    } catch (error) {
      keys = [];
    }
    for (var index = 0; index < keys.length; index++) {
      var key = keys[index];
      names.add(key);
      var descriptor;
      try {
        descriptor = Object.getOwnPropertyDescriptor(target, key);
      } catch (error) {
        continue;
      }
      if (!descriptor || !("value" in descriptor)) {
        continue;
      }
      var value = descriptor.value;
      var kind = typeof value;
      if (value === null || (kind !== "object" && kind !== "function")) {
        continue;
      }
      if (!seenObjects.has(value)) {
        seenObjects.add(value);
        pending.push(value);
      }
    }
    var prototype;
    try {
      prototype = Object.getPrototypeOf(target);
    } catch (error) {
      continue;
    }
    if (prototype !== null && !seenObjects.has(prototype)) {
      seenObjects.add(prototype);
      pending.push(prototype);
    }
  }
  print("__broilerHostSurfaceBegin__");
  print(Array.from(names).join("\\n"));
  print("__broilerHostSurfaceEnd__");
})();
""".strip()
# The minified program is not valid JavaScript: the reference engine's parser rejects what
# the minifier emitted. Terser accepts `if (false) let \n {}` and prints `if(false){let{}}`,
# unescapes `for (async of [7])` into the `for (async of …)` the grammar forbids, and
# drops the parentheses from `import((0, "./m.js"))` leaving a two-argument call. No engine
# runs any of those, so there is no minified variant to hold Broiler to.
MINIFIER_INVALID_OUTPUT = "minifier-invalid-output"
# The minified program runs but is no longer the test: mangling renamed something the test
# MEASURES. `assert.sameValue(cls.name, 'cls')` is checking the name a binding gave an
# anonymous class, and the binding is now `a`; the Annex B early-error cases in
# block-decl-func-skip-early-err-* turn on a body function sharing a name with a lexical
# declaration, and the two are renamed apart. Every engine fails these, which is how they
# are told from an engine defect.
MINIFIER_CHANGED_SEMANTICS = "minifier-changed-semantics"
ASYNC_DONE_PRELUDE = """
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
# Node-only tail for the reference run: fail the process when the async test rejects, and
# when it never settles at all (nothing keeps the loop alive, so Node exits at that point —
# `settled` is what tells the two apart).
ASYNC_REFERENCE_EPILOGUE = """
;(function () {
  let settled = false;
  process.exitCode = 1;
  Promise.resolve(__broilerDonePromise).then(
    () => { settled = true; process.exitCode = 0; },
    (error) => {
      settled = true;
      process.exitCode = 1;
      console.error(String((error && error.stack) || error));
    });
  process.on("exit", () => {
    if (!settled)
      console.error("async test did not call $DONE");
  });
})();
""".strip()


class MinifierError(RuntimeError):
    """Raised when the configured JavaScript minifier cannot produce output."""


class ReferenceEngineError(RuntimeError):
    """Raised when the reference engine cannot be started or invoked."""


class MinifierTimeoutError(MinifierError):
    """Raised when the minifier does not answer within its per-source timeout."""


class MinifierSyntaxError(MinifierError):
    """Raised when the minifier's own parser rejects a test's source.

    This says something about the minifier, never about the engine under test. test262
    exercises corners a production minifier has no reason to accept — an escaped keyword
    used as an IdentifierName (``br\\u0065ak(){}``), ``let`` as a sloppy-mode identifier,
    a redeclared ``arguments``, an Annex B CallExpression assignment target, decorators,
    import attributes with a trailing comma — and Terser refuses all of them even though
    the engine (and every browser) runs them. Closure declines a further set of its own
    (decorators, ``using``, auto-accessors, private ``#x in o``, the RegExp ``v`` flag).
    Nothing was minified, so there is nothing the engine could have got wrong: the case is
    reported as not applicable rather than counted against the engine.
    """


class MinifierSession:
    """A persistent JSON-lines bridge to one minifier worker for one Python thread.

    Terser and Closure speak the same protocol — ``hello`` returns the profile the worker
    implements and the minifier's own version, ``minify`` returns transformed source — so
    the process plumbing, the per-request timeout, and the crash diagnostics are shared and
    only the argv and the labels differ.
    """

    def __init__(
        self,
        worker_path: Path,
        worker_arguments: list[str],
        timeout_seconds: float,
        expected_profile: str,
        label: str,
    ) -> None:
        node = shutil.which("node")
        if node is None:
            raise MinifierError(f"Node.js is required for the {label} variant")
        if not worker_path.is_file():
            raise MinifierError(f"{label} worker was not found: {worker_path}")

        self.label = label
        self.expected_profile = expected_profile
        self.timeout_seconds = timeout_seconds
        self._next_request_id = 1
        self._responses: queue.Queue[str | None] = queue.Queue()
        self._stderr_lines: list[str] = []
        self._stderr_lock = threading.Lock()
        try:
            self._process = subprocess.Popen(
                [node, str(worker_path), *worker_arguments],
                stdin=subprocess.PIPE,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                encoding="utf-8",
                errors="replace",
                bufsize=1,
            )
        except OSError as exc:
            raise MinifierError(
                f"Could not start the {label} worker: {exc}"
            ) from exc

        assert self._process.stdout is not None
        assert self._process.stderr is not None
        self._stdout_thread = threading.Thread(
            target=self._read_stdout,
            args=(self._process.stdout,),
            daemon=True,
        )
        self._stderr_thread = threading.Thread(
            target=self._read_stderr,
            args=(self._process.stderr,),
            daemon=True,
        )
        self._stdout_thread.start()
        self._stderr_thread.start()

        hello = self._request({"operation": "hello"})
        if hello.get("profile") != expected_profile:
            self.close()
            raise MinifierError(
                f"{label} worker profile mismatch: "
                f"expected {expected_profile}, got {hello.get('profile')!r}"
            )
        self.version = str(hello.get("version") or "")
        if not self.version:
            self.close()
            raise MinifierError(
                f"{label} worker did not report its package version"
            )

    def _read_stdout(self, stream: object) -> None:
        try:
            for line in stream:  # type: ignore[union-attr]
                self._responses.put(line)
        finally:
            self._responses.put(None)

    def _read_stderr(self, stream: object) -> None:
        for line in stream:  # type: ignore[union-attr]
            with self._stderr_lock:
                self._stderr_lines.append(line.rstrip())
                if len(self._stderr_lines) > 20:
                    del self._stderr_lines[:-20]

    def _stderr_tail(self) -> str:
        with self._stderr_lock:
            return "\n".join(self._stderr_lines).strip()

    def _request(self, payload: dict[str, object]) -> dict[str, object]:
        if self._process.poll() is not None:
            detail = self._stderr_tail()
            suffix = f": {detail}" if detail else ""
            raise MinifierError(
                f"{self.label} worker exited with code "
                f"{self._process.returncode}{suffix}"
            )

        request_id = self._next_request_id
        self._next_request_id += 1
        request = dict(payload)
        request["id"] = request_id
        try:
            assert self._process.stdin is not None
            self._process.stdin.write(
                json.dumps(request, ensure_ascii=True, separators=(",", ":")) + "\n"
            )
            self._process.stdin.flush()
        except (BrokenPipeError, OSError) as exc:
            detail = self._stderr_tail()
            suffix = f": {detail}" if detail else ""
            raise MinifierError(
                f"Could not write to the {self.label} worker{suffix}"
            ) from exc

        try:
            line = self._responses.get(timeout=self.timeout_seconds)
        except queue.Empty as exc:
            self.close()
            raise MinifierTimeoutError(
                f"{self.label} exceeded {self.timeout_seconds:g}s"
            ) from exc
        if line is None:
            detail = self._stderr_tail()
            suffix = f": {detail}" if detail else ""
            raise MinifierError(
                f"{self.label} worker closed its output unexpectedly{suffix}"
            )
        try:
            response = json.loads(line)
        except json.JSONDecodeError as exc:
            raise MinifierError(
                f"{self.label} worker returned malformed JSON"
            ) from exc
        if not isinstance(response, dict) or response.get("id") != request_id:
            raise MinifierError(
                f"{self.label} worker returned an out-of-sequence response"
            )
        if not response.get("ok"):
            error_name = str(response.get("errorName") or "Error")
            error_message = str(response.get("errorMessage") or "unknown minifier error")
            location = ""
            if response.get("line") is not None:
                location = f" at line {response['line']}"
                if response.get("column") is not None:
                    location += f", column {response['column']}"
            # A SyntaxError is the minifier's parser declining the source; anything
            # else (a protocol fault, an internal minifier error) is a real
            # infrastructure problem and keeps failing the run.
            error_type = (
                MinifierSyntaxError if error_name == "SyntaxError" else MinifierError
            )
            raise error_type(
                f"{self.label} {error_name}{location}: {error_message}"
            )
        return response

    def minify(
        self,
        source: str,
        path: str,
        includes: list[str] | None = None,
    ) -> dict[str, object]:
        response = self._request(
            {
                "operation": "minify",
                "source": source,
                "path": path,
                # Only Closure reads this: the harness files a test includes become the
                # externs that keep ADVANCED from renaming the API the test calls. The
                # Terser worker ignores the field.
                "includes": list(includes or []),
            }
        )
        code = response.get("code")
        if not isinstance(code, str):
            raise MinifierError(
                f"{self.label} worker returned no JavaScript output"
            )
        result: dict[str, object] = {
            "code": code,
            "originalBytes": len(source.encode("utf-8")),
            "minifiedBytes": len(code.encode("utf-8")),
        }
        unsupported_syntax = response.get("unsupportedSyntax")
        if isinstance(unsupported_syntax, str) and unsupported_syntax:
            result["unsupportedSyntax"] = unsupported_syntax
        return result

    @property
    def is_alive(self) -> bool:
        return self._process.poll() is None

    def close(self) -> None:
        process = getattr(self, "_process", None)
        if process is None or process.poll() is not None:
            return
        try:
            if process.stdin is not None:
                process.stdin.close()
            process.terminate()
            process.wait(timeout=2)
        except (OSError, subprocess.TimeoutExpired):
            process.kill()
            try:
                process.wait(timeout=2)
            except subprocess.TimeoutExpired:
                pass


class WorkerMinifier:
    """Thread-local persistent minifier-worker sessions shared by one shard run.

    Subclasses name the variant, the profile its worker implements, and the argv that
    starts it. Everything else — one live session per Python worker thread, a version that
    may not change under the run, and replacing a session only once it has actually died —
    is identical for every minifier.
    """

    name = ""
    profile = ""
    label = ""
    worker_path = TERSER_WORKER

    def __init__(self, timeout_seconds: float) -> None:
        if not math.isfinite(timeout_seconds) or timeout_seconds <= 0:
            raise ValueError("minifier timeout must be finite and positive")
        self.timeout_seconds = timeout_seconds
        self.version = ""
        self._local = threading.local()
        self._sessions: list[MinifierSession] = []
        self._lock = threading.Lock()

    def _worker_arguments(self) -> list[str]:
        raise NotImplementedError

    def _read_package_version(self, module_root: Path, option: str) -> str:
        package_json = module_root / "package.json"
        if not package_json.is_file():
            raise MinifierError(
                f"{option} must name the installed {self.label} package directory: "
                f"{module_root}"
            )
        try:
            package = json.loads(package_json.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as exc:
            raise MinifierError(f"Could not read {package_json}: {exc}") from exc
        version = str(package.get("version") or "")
        if not version:
            raise MinifierError(
                f"{self.label} package has no version: {package_json}"
            )
        return version

    def _new_session(self) -> MinifierSession:
        return MinifierSession(
            self.worker_path,
            self._worker_arguments(),
            self.timeout_seconds,
            self.profile,
            self.label,
        )

    def _session(self) -> MinifierSession:
        session = getattr(self._local, "session", None)
        if session is None:
            session = self._new_session()
            if session.version != self.version:
                session.close()
                raise MinifierError(
                    f"{self.label} package version changed during the run "
                    f"({self.version} -> {session.version})"
                )
            self._local.session = session
            with self._lock:
                self._sessions.append(session)
        return session

    def probe(self) -> None:
        """Fail once, before shard execution, when Node or the minifier is unusable."""
        session = self._new_session()
        try:
            if session.version != self.version:
                raise MinifierError(
                    f"{self.label} package version mismatch "
                    f"({self.version} metadata, {session.version} runtime)"
                )
        finally:
            session.close()

    def minify(
        self,
        source: str,
        path: str,
        includes: list[str] | None = None,
    ) -> dict[str, object]:
        session = self._session()
        try:
            return session.minify(source, path, includes)
        except MinifierError:
            # A source-level minifier diagnostic leaves the worker usable. A
            # timeout/broken process is replaced for the next independent test
            # so one bad transformation cannot poison the rest of the shard.
            if not session.is_alive:
                self._local.session = None
            raise

    def close(self) -> None:
        with self._lock:
            sessions = self._sessions[:]
            self._sessions.clear()
        for session in sessions:
            session.close()


class TerserMinifier(WorkerMinifier):
    """Terser under the conservative test262-safe mangle profile."""

    name = TERSER_VARIANT
    profile = TERSER_PROFILE
    label = "Terser"
    worker_path = TERSER_WORKER

    def __init__(self, module_root: str, timeout_seconds: float) -> None:
        super().__init__(timeout_seconds)
        self.module_root = Path(module_root).resolve()
        self.version = self._read_package_version(
            self.module_root, "--terser-module"
        )

    def _worker_arguments(self) -> list[str]:
        return [str(self.module_root)]


class ClosureMinifier(WorkerMinifier):
    """Google Closure Compiler at ADVANCED_OPTIMIZATIONS against the ES2026 standard.

    ADVANCED renames properties as well as variables, removes what it proves unreachable,
    and inlines aggressively. That is a far larger transformation than Terser's, and a
    correspondingly larger share of test262 stops being the test it was once it has been
    applied — a test that reads a property name back as a string, or asserts the ``name`` a
    binding gave a function, now measures the compiled program instead. Those cases are not
    engine defects and are not counted as such: the reference-engine cross-check moves each
    one to a ``minifier-changed-semantics`` skip. What survives is the real question this
    variant asks — whether Broiler runs the compact, whole-program-optimized JavaScript an
    ADVANCED-compiled production bundle actually ships.
    """

    name = CLOSURE_VARIANT
    profile = CLOSURE_PROFILE
    label = "Closure Compiler"
    worker_path = CLOSURE_WORKER

    def __init__(
        self,
        module_root: str,
        harness_root: str | Path,
        timeout_seconds: float,
        host_externs: str | Path,
    ) -> None:
        super().__init__(timeout_seconds)
        self.module_root = Path(module_root).resolve()
        self.harness_root = Path(harness_root).resolve()
        if not self.harness_root.is_dir():
            raise MinifierError(
                "The Closure variant needs the pinned test262 harness directory for its "
                f"externs, and it is not there: {self.harness_root}"
            )
        self.host_externs = Path(host_externs).resolve()
        if not self.host_externs.is_file():
            raise MinifierError(
                "The Closure variant needs the engine's own global surface as externs, "
                f"and that file is not there: {self.host_externs}"
            )
        self.version = self._read_package_version(
            self.module_root, "--closure-module"
        )

    def _worker_arguments(self) -> list[str]:
        return [
            str(self.module_root),
            str(self.harness_root),
            # The worker's own ceiling on one compiler process. Kept above the Python
            # timeout so the request times out here, where a timeout is a reported
            # per-test outcome, rather than there, where it is an opaque worker error.
            str(int(self.timeout_seconds * 1000) + 5000),
            str(self.host_externs),
        ]


def collect_host_property_names(
    broiler_dll: str,
    timeout_seconds: float = DEFAULT_HOST_SURFACE_TIMEOUT_SECONDS,
) -> list[str]:
    """Ask the engine under test which property names its own globals own.

    The answer is the engine's, not a list kept here, for the same reason the harness is
    handed to Closure as externs rather than transcribed: a transcription is wrong the day
    the engine implements one more builtin, and wrong in the direction that reports the new
    builtin as an engine failure.
    """

    TEMP_DIRECTORY.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(
        "w",
        suffix=".js",
        delete=False,
        dir=TEMP_DIRECTORY,
        encoding="utf-8",
        newline="",
    ) as handle:
        handle.write(HOST_SURFACE_SCRIPT)
        script_path = handle.name

    try:
        process = subprocess.run(
            ["dotnet", broiler_dll, "--script-host", script_path],
            capture_output=True,
            text=True,
            timeout=timeout_seconds,
        )
    except subprocess.TimeoutExpired as exc:
        raise MinifierError(
            f"The engine did not report its global surface within {timeout_seconds:g}s"
        ) from exc
    except (OSError, subprocess.SubprocessError) as exc:
        raise MinifierError(
            f"Could not run the engine at {broiler_dll} to read its global surface: {exc}"
        ) from exc
    finally:
        os.unlink(script_path)

    lines = (process.stdout or "").splitlines()
    bounded = (
        process.returncode == 0
        and HOST_SURFACE_BEGIN in lines
        and HOST_SURFACE_END in lines
        and lines.index(HOST_SURFACE_BEGIN) < lines.index(HOST_SURFACE_END)
    )
    if not bounded:
        detail = (process.stderr or "").strip().splitlines()
        raise MinifierError(
            f"The engine at {broiler_dll} did not report its global surface "
            f"(exit {process.returncode})"
            + (f": {detail[0]}" if detail else "")
        )

    reported = lines[lines.index(HOST_SURFACE_BEGIN) + 1 : lines.index(HOST_SURFACE_END)]
    # A property name that is not identifier-shaped — an array index, a name with a space
    # in it — cannot be written as a `.name` access and ADVANCED does not rename it either,
    # so there is nothing to hold back.
    names = sorted(
        {
            name
            for name in (line.strip() for line in reported)
            if HOST_SURFACE_IDENTIFIER.fullmatch(name)
        }
    )
    if not names:
        raise MinifierError(
            f"The engine at {broiler_dll} reported an empty global surface"
        )
    return names


def write_host_externs(names: list[str], destination: Path) -> Path:
    """Write the engine's property names as an externs file ADVANCED will not rename.

    A property name that appears anywhere in externs is exempt from Closure's property
    renaming, and the holder is typed ``?`` so the compiler asks nothing about the object
    itself — the same shape closure_worker.cjs uses for the names a test quotes.
    """

    lines = [
        "/** @fileoverview Property names the Broiler script host owns. @externs */",
        f"/** @type {{?}} */ var {HOST_EXTERNS_HOLDER};",
    ]
    lines.extend(f"{HOST_EXTERNS_HOLDER}.{name};" for name in names)
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text("\n".join(lines) + "\n", encoding="utf-8")
    return destination


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

    def list_paths(
        self,
        prefix: str = "",
        suffix: str = "",
        include_fixtures: bool = True,
    ) -> list[str]:
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
            path
            for path in candidates
            if path.startswith(prefix)
            and path.endswith(suffix)
            and (include_fixtures or not path.endswith(TEST_FIXTURE_SUFFIX))
        )

    def read_text(self, path: str) -> str:
        if path not in self.text_cache:
            if self.suite_root is not None:
                # Read with newline="" so the original line terminators (CR / CRLF / LS / PS) are
                # preserved verbatim rather than universally translated to LF. Some tests — e.g. the
                # Function.prototype.toString line-terminator-normalisation cases — assert on the exact
                # source bytes, and the script host must see the same content a remote (byte) read would.
                with open(
                    self._resolve_local_path(path), encoding="utf-8", newline=""
                ) as handle:
                    self.text_cache[path] = handle.read()
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

            # _FIXTURE.js files are not tests (test262 INTERPRETING.md) — they are modules
            # other tests import, and some are deliberately un-runnable alone (a bare syntax
            # error). list_paths already drops them via include_fixtures=False; directory
            # expansion for --path-file manifests did not, so every manifest naming a
            # directory that contains one reported it as a failure.
            return sorted(
                child.relative_to(self.suite_root).as_posix()
                for child in local_path.rglob("*.js")
                if child.is_file() and not child.name.endswith(TEST_FIXTURE_SUFFIX)
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
            if (
                entry_type == "file"
                and entry_path.endswith(".js")
                and not entry_path.endswith(TEST_FIXTURE_SUFFIX)
            ):
                files.append(entry_path)
            elif entry_type == "dir":
                files.extend(self._expand_path(entry_path))
        return files


# The metadata block is delimited by `/*---` ... `---*/`, each on its own line; the line terminator
# after `---` and before `---*/` may be CR, CRLF, LF, LS, or PS. Match any of them so a test that uses
# non-LF terminators (e.g. the Function.prototype.toString line-terminator-normalisation cases, whose
# source is read with its terminators preserved) still has its metadata recognised.
METADATA_BLOCK_PATTERN = re.compile(r"/\*---[\r\n  ](.*?)[\r\n  ]---\*/", re.S)


def _normalize_newlines(text: str) -> str:
    return (
        text.replace("\r\n", "\n")
        .replace("\r", "\n")
        .replace(" ", "\n")
        .replace(" ", "\n")
    )


def parse_metadata(source: str) -> tuple[dict[str, list[str]], str]:
    match = METADATA_BLOCK_PATTERN.search(source)
    if match is None:
        return {"features": [], "includes": [], "flags": []}, source

    # Normalise only the metadata block for the line-oriented field regexes below; the returned body
    # retains its original line terminators so the script host sees the test's exact source.
    block = _normalize_newlines(match.group(1))
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
    metadata_match = METADATA_BLOCK_PATTERN.search(source)
    if metadata_match is None:
        return None

    block = _normalize_newlines(metadata_match.group(1))
    negative_match = re.search(r"(?m)^negative:\s*\n((?:[ \t]+.*\n?)*)", block)
    if negative_match is None:
        return None

    negative_block = negative_match.group(1)
    summary: dict[str, str] = {}
    for key in ("phase", "type"):
        value_match = re.search(rf"(?m)^[ \t]+{key}:\s*(.+?)\s*$", negative_block)
        if value_match is not None:
            summary[key] = value_match.group(1)

    return summary if summary else None


def quoted_source_text(body: str) -> str | None:
    """The first quoted function or class in the body that the same file also declares.

    `test/staging/sm/generators/runtime.js` asserts
    `assert.sameValue("function* g() { yield 1; }", g.toString())` about the `function* g`
    it declares seventy lines earlier: the literal is a transcript of the source text, and
    rewriting source text is the whole of what a minifier does, so the assertion measures
    the formatter and not the engine. The test names no `Function.prototype.toString` for
    a prefix or a pattern to key on — it just calls `.toString()` — but the relationship
    it depends on is in the file: a quoted definition that reappears as code.

    All three conditions are needed to find those two tests (this one and
    `staging/sm/async-functions/toString.js`) and nothing else in the suite. The literal
    has to READ as a definition rather than as an assertion message quoting an expression,
    which hundreds of tests do; that definition has to occur outside its own quotes —
    compared with whitespace collapsed, so the match holds however the file is laid out,
    and against a body whose own string literals are blanked, so a message quoted twice
    cannot pass for code; and a `toString` has to stand next to it, which is what tells
    the expected source text of a function apart from a definition the test merely evals.
    """
    code_only = QUOTED_STRING.sub(lambda match: match.group(1) * 2, body)
    if "toString" not in code_only:
        return None
    collapsed_code = WHITESPACE_RUN.sub("", code_only)
    for match in QUOTED_STRING.finditer(body):
        literal = match.group(2)
        if "\\" in literal or not WHITESPACE_RUN.search(literal):
            continue
        collapsed = WHITESPACE_RUN.sub("", literal)
        if len(collapsed) < MINIMUM_QUOTED_SOURCE_LENGTH:
            continue
        if not QUOTED_DEFINITION.match(collapsed):
            continue
        if collapsed not in collapsed_code:
            continue
        neighbourhood = body[
            max(0, match.start() - QUOTED_SOURCE_NEIGHBOURHOOD) :
            match.end() + QUOTED_SOURCE_NEIGHBOURHOOD
        ]
        if "toString" in neighbourhood:
            return literal
    return None


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
    unsupported_flags = sorted(SKIPPED_FLAGS & set(flags))
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


def normalize_test_path(path: str) -> str:
    """Return the canonical forward-slash form used by filters and sharding."""
    normalized = path.strip().replace("\\", "/")
    while normalized.startswith("./"):
        normalized = normalized[2:]
    return normalized.lstrip("/")


def parse_semicolon_patterns(
    value: str | None,
    *,
    normalize_paths: bool = False,
) -> list[str]:
    """Split a semicolon filter while preserving order and removing duplicates."""
    patterns: list[str] = []
    seen: set[str] = set()
    for raw_pattern in (value or "").split(";"):
        pattern = raw_pattern.strip()
        if normalize_paths:
            pattern = normalize_test_path(pattern)
        if not pattern or pattern in seen:
            continue
        seen.add(pattern)
        patterns.append(pattern)
    return patterns


def path_matches_subset(path: str, patterns: list[str]) -> bool:
    """Match a test path with the main WPT runner's prefix-glob semantics.

    ``*`` and ``?`` never cross a path separator. A wildcard pattern matches
    the path prefix it describes plus descendants of that prefix; a plain
    pattern is an exact path or recursive directory prefix.
    """
    if not patterns:
        return True

    normalized_path = normalize_test_path(path)
    for raw_pattern in patterns:
        pattern = normalize_test_path(raw_pattern).rstrip("/")
        if not pattern:
            return True
        if not any(character in pattern for character in "*?"):
            if normalized_path.casefold() == pattern.casefold() or (
                normalized_path.casefold().startswith(pattern.casefold() + "/")
            ):
                return True
            continue

        pieces = ["^"]
        for character in pattern:
            if character == "*":
                pieces.append("[^/]*")
            elif character == "?":
                pieces.append("[^/]")
            else:
                pieces.append(re.escape(character))
        pieces.append("(?:/.*)?$")
        if re.match("".join(pieces), normalized_path, flags=re.IGNORECASE):
            return True
    return False


def features_match_patterns(
    features: list[str],
    patterns: list[str],
    match_mode: str,
) -> bool:
    """Return whether feature metadata satisfies an any/all pattern filter."""
    if not patterns:
        return True
    if match_mode not in {"any", "all"}:
        raise ValueError(f"feature_match must be 'any' or 'all', got {match_mode!r}")

    pattern_matches = [
        any(fnmatch.fnmatchcase(feature, pattern) for feature in features)
        for pattern in patterns
    ]
    return any(pattern_matches) if match_mode == "any" else all(pattern_matches)


def positive_float(value: str) -> float:
    parsed = float(value)
    if not math.isfinite(parsed) or parsed <= 0:
        raise argparse.ArgumentTypeError(f"value must be positive, got {value}")
    return parsed


def positive_int(value: str) -> int:
    parsed = int(value)
    if parsed <= 0:
        raise argparse.ArgumentTypeError(f"value must be a positive integer, got {value}")
    return parsed


def non_negative_int(value: str) -> int:
    parsed = int(value)
    if parsed < 0:
        raise argparse.ArgumentTypeError(f"value must be non-negative, got {value}")
    return parsed


def shard_index_for_path(path: str, shard_count: int) -> int:
    """Return the stable 32-bit FNV-1a shard for a normalized UTF-8 path."""
    if shard_count <= 0:
        raise ValueError(f"shard_count must be greater than 0, got {shard_count}")

    hash_value = 2166136261
    for byte in normalize_test_path(path).encode("utf-8"):
        hash_value ^= byte
        hash_value = (hash_value * 16777619) & 0xFFFFFFFF
    return hash_value % shard_count


def apply_shard(paths: list[str], shard_count: int, shard_index: int) -> list[str]:
    """Return one content-independent FNV-1a shard from an ordered path list."""
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

    return [
        path
        for path in paths
        if shard_index_for_path(path, shard_count) == shard_index
    ]


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


def build_test_process_command(
    broiler_dll: str,
    script_path: str,
    timeout_seconds: float,
    memory_limit_mb: int,
) -> list[str]:
    """Build a test command without using unsafe ``preexec_fn`` callbacks.

    On POSIX, a fresh single-threaded Python helper applies resource limits and
    then replaces itself with ``dotnet``. The parent runner may therefore launch
    tests from worker threads without running Python code between fork and exec.
    """
    target = ["dotnet", broiler_dll, "--script-host", script_path]
    if os.name != "posix" or resource is None:
        return target
    return [
        sys.executable,
        str(Path(__file__).resolve()),
        INTERNAL_EXEC_WITH_LIMITS,
        format(timeout_seconds, ".17g"),
        str(memory_limit_mb),
        "--",
        *target,
    ]


def exec_with_test_process_limits(arguments: list[str]) -> int:
    """Internal single-threaded launcher used by :func:`run_test` on POSIX."""
    if len(arguments) < 4 or arguments[2] != "--":
        raise ValueError(
            f"{INTERNAL_EXEC_WITH_LIMITS} expects TIMEOUT MEMORY_MB -- COMMAND [ARG ...]"
        )

    timeout_seconds = positive_float(arguments[0])
    memory_limit_mb = non_negative_int(arguments[1])
    command = arguments[3:]
    limit_setup = create_process_limit_setup(timeout_seconds, memory_limit_mb)
    if limit_setup is not None:
        limit_setup()
    os.execvp(command[0], command)
    return 127  # pragma: no cover - os.execvp only returns by raising


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
    subset_patterns: list[str] | str | None = None,
    feature_patterns: list[str] | str | None = None,
    feature_match: str = "any",
) -> tuple[list[str], dict[str, object]]:
    """Select test paths plus metadata for requested or full script-host runs."""
    subset_filter_text = (
        subset_patterns
        if isinstance(subset_patterns, str)
        else ";".join(subset_patterns or [])
    )
    feature_filter_text = (
        feature_patterns
        if isinstance(feature_patterns, str)
        else ";".join(feature_patterns or [])
    )
    subset_patterns = parse_semicolon_patterns(
        subset_filter_text,
        normalize_paths=True,
    )
    feature_patterns = parse_semicolon_patterns(feature_filter_text)
    if feature_match not in {"any", "all"}:
        raise ValueError(
            f"feature_match must be 'any' or 'all', got {feature_match!r}"
        )

    selection_mode = "requested"
    if all_script_host_verifiable:
        candidate_paths = repo.list_paths(
            prefix="test/", suffix=".js", include_fixtures=False
        )
        harness_dependency_cache: dict[str, bool] = {}
        expanded_paths = []
        for path in candidate_paths:
            if path in KNOWN_INCORRECT_TESTS or not path_matches_subset(
                path, subset_patterns
            ):
                continue
            source = repo.read_text(path)
            metadata, _ = parse_metadata(source)
            if not features_match_patterns(
                metadata["features"], feature_patterns, feature_match
            ):
                continue
            if _is_selectable(
                source,
                repo,
                harness_dependency_cache,
                include_negative,
            ):
                expanded_paths.append(path)
        selection_mode = "all-script-host-verifiable"
    else:
        candidate_paths = repo.expand_paths(requested_paths)
        expanded_paths = [
            path
            for path in candidate_paths
            if path_matches_subset(path, subset_patterns)
            and features_match_patterns(
                parse_metadata(repo.read_text(path))[0]["features"],
                feature_patterns,
                feature_match,
            )
        ]

    promoted_count = 0
    if prioritize_fragile:
        fragile_paths = _load_fragile_paths()
        changed_prefixes = _detect_recently_changed_directories()
        expanded_paths, promoted_count = _prioritize_paths(
            expanded_paths, fragile_paths, changed_prefixes,
        )

    if shuffle_seed is not None:
        randomizer = random.Random(shuffle_seed)
        if promoted_count > 0:
            priority_paths = expanded_paths[:promoted_count]
            remaining_paths = expanded_paths[promoted_count:]
            randomizer.shuffle(priority_paths)
            randomizer.shuffle(remaining_paths)
            expanded_paths = priority_paths + remaining_paths
        else:
            randomizer.shuffle(expanded_paths)

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
        "subsetPatterns": subset_patterns,
        "featurePatterns": feature_patterns,
        "featureMatch": feature_match,
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


def assemble_test_program(
    repo: Test262Repository,
    harness_cache: dict[str, str],
    metadata: dict[str, list[str]],
    body: str,
    *,
    body_includes_strict: bool = False,
) -> str:
    """Build the complete script a host is handed for one test: harness, then body.

    Shared by the engine under test and the reference engine so a cross-check compares
    the same program rather than two assemblies that happen to look alike.
    """

    def harness_text(name: str) -> str:
        if name not in harness_cache:
            harness_cache[name] = repo.read_text(f"harness/{name}")
        return harness_cache[name]

    is_async = "async" in metadata["flags"]
    is_only_strict = "onlyStrict" in metadata["flags"]

    parts = []
    if is_only_strict and not body_includes_strict:
        parts.append('"use strict";')
    parts.extend([harness_text("assert.js"), harness_text("sta.js")])
    for include in metadata["includes"]:
        parts.append(harness_text(include))
    if is_async:
        parts.append(ASYNC_DONE_PRELUDE)
    parts.append(body)
    if is_async:
        # The leading semicolon is an explicit token boundary for a minified
        # body, independent of its final token and automatic-semicolon rules.
        parts.append(";__broilerDonePromise")

    return "\n".join(parts)


class ReferenceEngine:
    """A second engine, consulted only about whether a MINIFIED program is still the test.

    A minified variant is evidence about the engine only while the transformation preserved
    what the test measures, and Terser's contract does not include that: it renames the very
    bindings whose names a `fn-name-*` test asserts, and it occasionally emits source no
    engine accepts. Both produce a failure that says nothing about Broiler, and
    docs/compliance/process.md already answers them the same way — check the case against a
    second engine before filing it. This runs that check instead of asking a reader to.

    It is asked only when a minified case FAILED, so a passing run costs nothing, and its
    answer can only ever move a case OUT of the engine's column: a divergence the reference
    engine does not reproduce stays a Broiler failure, exactly as before.

    Every program reaches it through reference_host.cjs, which compiles it as a script in
    the global scope rather than inside Node's module wrapper. What that buys is the
    reference engine's opinion on the tests whose subject IS global scope: without it Node
    fails the Annex B eval-code originals — a `var` the enclosing file declares is not the
    one the eval'd code assigns — and every one of them comes back "cannot run the original
    either", which is the answer that leaves the case attributed to Broiler.
    """

    name = REFERENCE_ENGINE

    def __init__(self, executable: str, timeout_seconds: float) -> None:
        if not math.isfinite(timeout_seconds) or timeout_seconds <= 0:
            raise ValueError("reference engine timeout must be finite and positive")
        resolved = shutil.which(executable)
        if resolved is None:
            raise ReferenceEngineError(
                f"Reference engine executable was not found: {executable}"
            )
        if not REFERENCE_HOST.is_file():
            raise ReferenceEngineError(
                f"Reference engine launcher was not found: {REFERENCE_HOST}"
            )
        self.executable = resolved
        self.timeout_seconds = timeout_seconds
        try:
            probe = subprocess.run(
                [self.executable, "--version"],
                capture_output=True,
                text=True,
                timeout=timeout_seconds,
            )
        except (OSError, subprocess.SubprocessError) as exc:
            raise ReferenceEngineError(
                f"Could not run the reference engine at {self.executable}: {exc}"
            ) from exc
        self.version = (probe.stdout or "").strip()
        if not self.version:
            raise ReferenceEngineError(
                f"Reference engine at {self.executable} did not report a version"
            )

    def _write_program(self, program: str) -> str:
        TEMP_DIRECTORY.mkdir(parents=True, exist_ok=True)
        # The suffix labels the file and no longer decides how it is read: reference_host.cjs
        # reads the program itself and compiles it as a script, so no package.json above the
        # temp directory can make Node treat it as a module.
        with tempfile.NamedTemporaryFile(
            "w",
            suffix=".cjs",
            delete=False,
            dir=TEMP_DIRECTORY,
            encoding="utf-8",
            newline="",
        ) as handle:
            handle.write(program)
            return handle.name

    def _invoke(self, arguments: list[str], script_path: str) -> str:
        try:
            process = subprocess.run(
                [self.executable, str(REFERENCE_HOST), *arguments, script_path],
                capture_output=True,
                text=True,
                timeout=self.timeout_seconds,
            )
        except subprocess.TimeoutExpired:
            return "timedOut"
        except (OSError, subprocess.SubprocessError) as exc:
            raise ReferenceEngineError(
                f"Reference engine invocation failed: {exc}"
            ) from exc
        return "passed" if process.returncode == 0 else "failed"

    def parses(self, program: str) -> bool:
        """Whether the reference engine's parser accepts the program as a script."""
        script_path = self._write_program(program)
        try:
            return self._invoke(["--check"], script_path) == "passed"
        finally:
            os.unlink(script_path)

    def run(self, program: str, is_async: bool) -> str:
        """Run one assembled program: "passed", "failed", or "timedOut"."""
        if is_async:
            # The shared assembly ends in the $DONE promise as the script's completion
            # value, which a host that awaits it observes. Node does not, so settle it
            # here — an async test that rejects or never calls $DONE must not read as a
            # pass on the side of the comparison that decides attribution.
            program = f"{program}\n{ASYNC_REFERENCE_EPILOGUE}"
        script_path = self._write_program(program)
        try:
            return self._invoke([], script_path)
        finally:
            os.unlink(script_path)


def run_test(
    repo: Test262Repository,
    broiler_dll: str,
    path: str,
    harness_cache: dict[str, str],
    timeout_seconds: float,
    memory_limit_mb: int,
    include_negative: bool = False,
    *,
    variant: str | None = None,
    body_override: str | None = None,
    body_override_includes_strict: bool = False,
) -> dict[str, object]:
    def outcome(status: str, **details: object) -> dict[str, object]:
        result: dict[str, object] = {
            "path": path,
            "status": status,
            **details,
        }
        if variant is not None:
            result["variant"] = variant
        return result

    if path in KNOWN_INCORRECT_TESTS:
        return outcome(
            "skipped",
            reason=f"known-incorrect upstream test: {KNOWN_INCORRECT_TESTS[path]}",
        )

    source = repo.read_text(path)
    metadata, body = parse_metadata(source)

    unsupported = sorted(SKIPPED_FLAGS & set(metadata["flags"]))
    unsupported_features = sorted(UNSUPPORTED_FEATURES & set(metadata["features"]))
    if unsupported or unsupported_features:
        reasons: list[str] = []
        if unsupported:
            reasons.append(f"unsupported flags: {', '.join(unsupported)}")
        if unsupported_features:
            reasons.append(
                f"unsupported features: {', '.join(unsupported_features)}"
            )
        return outcome("skipped", reason="; ".join(reasons))

    negative = parse_negative_metadata(source)
    if negative is not None and not include_negative:
        return outcome(
            "skipped",
            reason="negative metadata test (use --include-negative to run)",
        )

    program = assemble_test_program(
        repo,
        harness_cache,
        metadata,
        body if body_override is None else body_override,
        body_includes_strict=body_override_includes_strict,
    )

    TEMP_DIRECTORY.mkdir(parents=True, exist_ok=True)
    # newline="" for the same reason read_text uses it: the assembled script must reach the
    # host with the test's original line terminators. Without it Python translates every
    # "\n" to os.linesep on write, which on Windows turns an LF test into CRLF and a CRLF
    # test into CR-CRLF — failing the Function.prototype.toString line-terminator tests
    # (and only those; a CR-only test has no "\n" to translate, so it passed either way).
    with tempfile.NamedTemporaryFile(
        "w",
        suffix=".js",
        delete=False,
        dir=TEMP_DIRECTORY,
        encoding="utf-8",
        newline="",
    ) as handle:
        handle.write(program)
        script_path = handle.name

    try:
        process = subprocess.Popen(
            build_test_process_command(
                broiler_dll,
                script_path,
                timeout_seconds,
                memory_limit_mb,
            ),
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            start_new_session=os.name == "posix",
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
            return outcome(
                "timedOut",
                reason=f"timed out after {timeout_seconds:g}s",
                stdout=stdout or "",
                stderr=stderr or "",
            )
    finally:
        os.unlink(script_path)

    if negative is not None:
        # Negative tests expect an error.  If the engine exited with a
        # non-zero status, check whether stderr mentions the expected
        # error type; if it does, the test passes.
        expected_type = negative.get("type", "")
        if process.returncode != 0 and expected_type and expected_type in (stderr or ""):
            return outcome("passed", negative=True)
        if process.returncode == 0:
            return outcome(
                "failed",
                reason=f"negative test expected {expected_type} but succeeded",
                stdout=stdout,
                stderr=stderr,
            )
        return outcome(
            "failed",
            reason=f"negative test expected {expected_type} but got different error",
            stdout=stdout,
            stderr=stderr,
        )

    if process.returncode == 0:
        return outcome("passed")

    return outcome("failed", stdout=stdout, stderr=stderr)


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
    selection_label: str = "",
    runner_os: str = "",
    runner_arch: str = "",
    dotnet_version: str = "",
    minifier: str = "none",
    reference_engine: str = "",
    minifier_version: str = "",
    minifier_profile: str = "",
    minifier_timeout_seconds: float = 0.0,
    minifier_host_extern_names: int = 0,
) -> dict[str, object]:
    passed = sum(1 for result in results if result["status"] == "passed")
    failed = sum(1 for result in results if result["status"] == "failed")
    skipped = sum(1 for result in results if result["status"] == "skipped")
    timed_out = sum(1 for result in results if result["status"] == "timedOut")
    failed_paths = sorted(
        {str(result["path"]) for result in results if result["status"] == "failed"}
    )
    timed_out_paths = sorted(
        {
            str(result["path"])
            for result in results
            if result["status"] == "timedOut"
        }
    )
    executed = passed + failed + timed_out
    variants = [ORIGINAL_VARIANT]
    if minifier != "none":
        variants.append(minifier)
    variant_summary: dict[str, dict[str, int]] = {}
    for variant in variants:
        variant_results = [
            result
            for result in results
            if str(result.get("variant", ORIGINAL_VARIANT)) == variant
        ]
        variant_passed = sum(
            1 for result in variant_results if result["status"] == "passed"
        )
        variant_failed = sum(
            1 for result in variant_results if result["status"] == "failed"
        )
        variant_skipped = sum(
            1 for result in variant_results if result["status"] == "skipped"
        )
        variant_timed_out = sum(
            1 for result in variant_results if result["status"] == "timedOut"
        )
        variant_summary[variant] = {
            "total": len(variant_results),
            "executed": variant_passed + variant_failed + variant_timed_out,
            "passed": variant_passed,
            "failed": variant_failed,
            "skipped": variant_skipped,
            "timedOut": variant_timed_out,
            "notApplicable": sum(
                1 for result in variant_results if result.get("notApplicable") is True
            ),
        }

    path_results: dict[str, list[dict[str, object]]] = {}
    for result in results:
        path_results.setdefault(str(result["path"]), []).append(result)

    def aggregate_path_status(path_cases: list[dict[str, object]]) -> str:
        statuses = {str(result["status"]) for result in path_cases}
        if "timedOut" in statuses:
            return "timedOut"
        if "failed" in statuses:
            return "failed"
        if "passed" in statuses:
            return "passed"
        return "skipped"

    path_statuses = {
        path: aggregate_path_status(path_cases)
        for path, path_cases in path_results.items()
    }
    path_summary = {
        "total": len(path_statuses),
        "executed": sum(status != "skipped" for status in path_statuses.values()),
        "passed": sum(status == "passed" for status in path_statuses.values()),
        "failed": sum(status == "failed" for status in path_statuses.values()),
        "skipped": sum(status == "skipped" for status in path_statuses.values()),
        "timedOut": sum(status == "timedOut" for status in path_statuses.values()),
    }
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
        "selectionLabel": selection_label,
        "runnerOs": runner_os,
        "runnerArch": runner_arch,
        "dotnetVersion": dotnet_version,
        "minifier": minifier,
        # The engine name only. Its VERSION rides on the individual cross-checked cases
        # instead: the merge demands cross-shard equality of every run-configuration field,
        # and nothing pins a runner's Node to the patch level the way the lockfile pins
        # Terser's, so recording it here would turn a mid-run image bump into a merge failure.
        "referenceEngine": reference_engine,
        "minifierVersion": minifier_version,
        "minifierProfile": minifier_profile,
        "minifierTimeoutSeconds": minifier_timeout_seconds,
        "minifierOptions": dict(MINIFIER_OPTIONS.get(minifier, {})),
        # How much of itself the engine declared to Closure. An observation of this shard's
        # engine rather than a setting of the run, so — like the reference engine's version
        # — it is reported without being one of the fields the merge demands agreement on.
        "minifierHostExternNames": minifier_host_extern_names,
        "variants": variants,
        "expectedVariantCountPerPath": len(variants),
        "variantSummary": variant_summary,
        "pathSummary": path_summary,
        "candidateCount": selection["candidateCount"],
        "selectedCountBeforeSharding": selection["selectedCountBeforeSharding"],
        "shardCount": selection["shardCount"],
        "shardIndex": selection["shardIndex"],
        "subsetPatterns": selection.get("subsetPatterns", []),
        "featurePatterns": selection.get("featurePatterns", []),
        "featureMatch": selection.get("featureMatch", "any"),
        "total": len(results),
        "executed": executed,
        "passed": passed,
        "failed": failed,
        "failedPaths": failed_paths,
        "skipped": skipped,
        "timedOut": timed_out,
        "timedOutPaths": timed_out_paths,
        "notApplicable": sum(
            1 for result in results if result.get("notApplicable") is True
        ),
        "results": results,
    }


def _infrastructure_failure_result(
    path: str,
    exc: Exception,
    variant: str = ORIGINAL_VARIANT,
    failure_type: str = "RunnerInfrastructure",
    failure_stage: str = "runner",
) -> dict[str, object]:
    return {
        "path": path,
        "variant": variant,
        "status": "failed",
        "reason": f"infrastructure error ({type(exc).__name__}): {exc}",
        "infrastructure": True,
        "exceptionType": type(exc).__name__,
        "failureType": failure_type,
        "failureStage": failure_stage,
    }


def _enrich_result(
    result: dict[str, object],
    source_size_bytes: int | None,
    features: list[str],
    flags: list[str],
    duration_seconds: float,
) -> dict[str, object]:
    enriched = dict(result)
    enriched["sourceSizeBytes"] = source_size_bytes
    enriched["features"] = features
    enriched["flags"] = flags
    enriched["durationMs"] = round(max(0.0, duration_seconds * 1000.0), 3)
    return enriched


def _not_applicable_minified_result(
    path: str,
    reason: str,
    skip_kind: str = "minifier-not-applicable",
    variant: str = TERSER_VARIANT,
) -> dict[str, object]:
    return {
        "path": path,
        "variant": variant,
        "status": "skipped",
        "reason": reason,
        "notApplicable": True,
        "skipKind": skip_kind,
    }


def cross_check_minified_failure(
    reference_engine: object,
    repo: Test262Repository,
    path: str,
    harness_cache: dict[str, str],
    metadata: dict[str, list[str]],
    body: str,
    minified_body: str,
    minified_result: dict[str, object],
    *,
    includes_strict: bool,
) -> dict[str, object]:
    """Ask the reference engine whether a failing minified case is still the test.

    Three answers, and only the first moves the case:

    * the reference engine passes the ORIGINAL and fails the MINIFIED body — the
      transformation, not the engine, is what the failure is about, so the case becomes a
      not-applicable minifier skip (invalid output when its parser refuses the body at
      all, changed semantics when it runs and fails);
    * the reference engine passes BOTH — the divergence is Broiler's, and the failure
      stands with `referenceCrossCheck: engine-divergence` recording that it was checked;
    * the reference engine cannot run the original either — a feature it does not
      implement (Temporal, `using`, decorators) or a fixture it cannot resolve — so it has
      no opinion, and the failure stands as `inconclusive`.

    An engine that cannot RUN a test can still have read it. When its parser accepts the
    original and rejects the minified body, the minifier turned a program that engine
    admits into one it does not, whatever the two would have done at run time — the
    `import((1, 0, "./m.js"))` whose parentheses Terser drops is a three-argument
    `ImportCall` no grammar has, and Node says so without ever resolving the fixture the
    test needs. That answer is about syntax alone, so it is read even when the run was
    inconclusive.

    Anything the reference engine itself cannot do (a missing binary, a spawn failure) is
    also inconclusive: this may report an engine failure it cannot explain, never suppress
    one it did not check.
    """
    is_async = "async" in metadata["flags"]
    minified_program = assemble_test_program(
        repo,
        harness_cache,
        metadata,
        minified_body,
        # The strict directive is re-added by the assembler, exactly as the Broiler run has it.
        body_includes_strict=False,
    )

    engine_name = str(getattr(reference_engine, "name", REFERENCE_ENGINE))
    engine_version = str(getattr(reference_engine, "version", ""))

    def stands(verdict: str) -> dict[str, object]:
        minified_result["referenceCrossCheck"] = verdict
        minified_result["referenceEngine"] = engine_name
        minified_result["referenceEngineVersion"] = engine_version
        return minified_result

    try:
        if reference_engine.run(minified_program, is_async) == "passed":  # type: ignore[attr-defined]
            return stands("engine-divergence")

        original_program = assemble_test_program(
            repo, harness_cache, metadata, body, body_includes_strict=includes_strict
        )
        if not reference_engine.parses(minified_program):  # type: ignore[attr-defined]
            # Reading the minified body needs no support for what the test does, only for
            # the grammar it is written in. The original is the control: an engine that
            # cannot parse THAT either is refusing the test's own syntax and has said
            # nothing about the minifier.
            if not reference_engine.parses(original_program):  # type: ignore[attr-defined]
                return stands("inconclusive")
            outcome = MINIFIER_INVALID_OUTPUT
            reason = (
                f"{engine_name} parses this test and cannot parse its minified body: the "
                "minifier emitted source no engine accepts"
            )
        # Whether the reference engine can run the test AS WRITTEN is asked before its
        # RUNTIME verdict on the minified body is read for anything: a feature it does not
        # implement fails both variants and says nothing about either.
        elif reference_engine.run(original_program, is_async) != "passed":  # type: ignore[attr-defined]
            return stands("inconclusive")
        else:
            outcome = MINIFIER_CHANGED_SEMANTICS
            reason = (
                f"{engine_name} passes this test and fails its minified body: "
                "minification changed what the test measures"
            )
    except ReferenceEngineError as exc:
        minified_result["referenceCrossCheckError"] = str(exc)
        return stands("inconclusive")

    skipped = _not_applicable_minified_result(
        path,
        reason,
        skip_kind=outcome,
        variant=str(minified_result.get("variant") or TERSER_VARIANT),
    )
    skipped["referenceCrossCheck"] = outcome
    skipped["referenceEngine"] = engine_name
    skipped["referenceEngineVersion"] = engine_version
    # What the engine actually said, kept so a skipped case can still be read back.
    engine_failure = minified_result.get("stderr") or minified_result.get("reason") or ""
    if engine_failure:
        skipped["engineFailure"] = str(engine_failure)[:2000]
    return skipped


def run_test_with_metadata(
    repo: Test262Repository,
    broiler_dll: str,
    path: str,
    harness_cache: dict[str, str],
    timeout_seconds: float,
    memory_limit_mb: int,
    include_negative: bool = False,
) -> dict[str, object]:
    """Run one test and attach bounded triage metadata to its result.

    ``run_test`` intentionally retains its small historical return contract for
    direct callers. Shard execution uses this wrapper so one per-test exception
    is reported as an infrastructure failure instead of discarding the shard's
    whole machine-readable summary.
    """
    return run_test_variants_with_metadata(
        repo,
        broiler_dll,
        path,
        harness_cache,
        timeout_seconds,
        memory_limit_mb,
        include_negative,
        minifier=None,
    )[0]


def run_test_variants_with_metadata(
    repo: Test262Repository,
    broiler_dll: str,
    path: str,
    harness_cache: dict[str, str],
    timeout_seconds: float,
    memory_limit_mb: int,
    include_negative: bool = False,
    minifier: object | None = None,
    reference_engine: object | None = None,
) -> list[dict[str, object]]:
    """Run the original source and, when configured, its minified body."""
    minified_variant = str(getattr(minifier, "name", TERSER_VARIANT))
    original_started = time.perf_counter()
    source_size_bytes: int | None = None
    features: list[str] = []
    flags: list[str] = []
    source = ""
    body = ""
    metadata: dict[str, list[str]] = {"flags": [], "features": [], "includes": []}
    negative: dict[str, str] | None = None
    try:
        source = repo.read_text(path)
        source_size_bytes = len(source.encode("utf-8"))
        metadata, body = parse_metadata(source)
        features = list(metadata["features"])
        flags = list(metadata["flags"])
        negative = parse_negative_metadata(source)
        original = run_test(
            repo,
            broiler_dll,
            path,
            harness_cache,
            timeout_seconds,
            memory_limit_mb,
            include_negative,
        )
        original.setdefault("variant", ORIGINAL_VARIANT)
    except Exception as exc:
        original = _infrastructure_failure_result(path, exc)

    results = [
        _enrich_result(
            original,
            source_size_bytes,
            features,
            flags,
            time.perf_counter() - original_started,
        )
    ]
    if minifier is None:
        return results

    if original.get("status") == "skipped":
        skipped = _not_applicable_minified_result(
            path,
            f"original variant is not runnable: {original.get('reason', 'skipped')}",
            variant=minified_variant,
        )
        results.append(
            _enrich_result(skipped, source_size_bytes, features, flags, 0.0)
        )
        return results

    negative_phase = (
        negative.get("phase", "").strip().strip("'\"").lower()
        if negative is not None
        else ""
    )
    if negative is not None and negative_phase != "runtime":
        skipped = _not_applicable_minified_result(
            path,
            "parse, early, and resolution negative tests cannot be minified before execution",
            variant=minified_variant,
        )
        results.append(
            _enrich_result(skipped, source_size_bytes, features, flags, 0.0)
        )
        return results

    quoted_source = quoted_source_text(body)
    if (
        path.startswith(MINIFIER_SOURCE_SENSITIVE_PREFIXES)
        or FUNCTION_TOSTRING_REFERENCE.search(body)
        or quoted_source is not None
    ):
        detail = (
            f": the test quotes {quoted_source!r}, which it also contains as code"
            if quoted_source is not None
            else ""
        )
        skipped = _not_applicable_minified_result(
            path,
            "test observes source text changed by minification" + detail,
            variant=minified_variant,
        )
        results.append(
            _enrich_result(skipped, source_size_bytes, features, flags, 0.0)
        )
        return results

    minifier_started = time.perf_counter()
    minifier_input = body
    includes_strict = "onlyStrict" in flags
    code: object = None
    if includes_strict:
        minifier_input = f'"use strict";\n{body}'
    try:
        minified = minifier.minify(  # type: ignore[attr-defined]
            minifier_input, path, metadata["includes"]
        )
        minification_duration = time.perf_counter() - minifier_started
        code = minified.get("code")
        if not isinstance(code, str):
            raise MinifierError("minifier returned no JavaScript output")

        unsupported_syntax = minified.get("unsupportedSyntax")
        if isinstance(unsupported_syntax, str) and unsupported_syntax:
            # Syntax the minifier neither rejects nor preserves. Terser 5 reads
            # `accessor #x = 1` as two class elements and emits a class carrying a public
            # `accessor` field and a private field where the test declared an
            # auto-accessor's getter/setter pair. That output parses and runs, so nothing
            # raises — but it is a different program, exactly the situation
            # MinifierSyntaxError covers below, minus the diagnostic that announces it.
            code = None
            minified_result = _not_applicable_minified_result(
                path,
                f"minifier does not preserve this source: {unsupported_syntax} is "
                "parsed as something else and silently rewritten",
                skip_kind="minifier-unsupported-syntax",
                variant=minified_variant,
            )
        else:
            minified_result = run_test(
                repo,
                broiler_dll,
                path,
                harness_cache,
                timeout_seconds,
                memory_limit_mb,
                include_negative,
                variant=minified_variant,
                body_override=code,
                # Keep run_test's strict directive at byte zero. The minifier also
                # saw the directive so it parsed the body under the same semantics.
                body_override_includes_strict=False,
            )
    except MinifierSyntaxError as exc:
        # The minifier's parser declined the source, so no minified variant of this test
        # exists to run. That is a fact about the minifier — see MinifierSyntaxError — and
        # reporting it as a failure attributed hundreds of Terser parser gaps (escaped
        # keywords as IdentifierNames, sloppy-mode `let`, Annex B assignment targets,
        # decorators, import attributes) to the engine, which passes every one of those
        # tests as written.
        minification_duration = time.perf_counter() - minifier_started
        minified_result = _not_applicable_minified_result(
            path,
            f"minifier cannot parse this source: {exc}",
            skip_kind="minifier-unsupported-syntax",
            variant=minified_variant,
        )
    except Exception as exc:
        minification_duration = time.perf_counter() - minifier_started
        failure_type = (
            "MinifierTimeout"
            if isinstance(exc, MinifierTimeoutError)
            else "MinifierError"
        )
        minified_result = _infrastructure_failure_result(
            path,
            exc,
            variant=minified_variant,
            failure_type=failure_type,
            failure_stage="minifier",
        )

    if (
        reference_engine is not None
        and minified_result.get("status") == "failed"
        and minified_result.get("infrastructure") is not True
        # A negative test passes BY failing, which an exit code alone cannot tell from the
        # failure this is asking about. Those keep the engine's own verdict.
        and negative is None
    ):
        minified_result = cross_check_minified_failure(
            reference_engine,
            repo,
            path,
            harness_cache,
            metadata,
            body,
            str(code) if isinstance(code, str) else "",
            minified_result,
            includes_strict=includes_strict,
        )

    minified_result["minifier"] = minified_variant
    minified_result["minifierVersion"] = str(getattr(minifier, "version", ""))
    minified_result["minifierProfile"] = str(
        getattr(minifier, "profile", TERSER_PROFILE)
    )
    minified_result["minificationDurationMs"] = round(
        max(0.0, minification_duration * 1000.0), 3
    )
    if "minified" in locals() and isinstance(minified, dict):
        original_bytes = minified.get("originalBytes")
        minified_bytes = minified.get("minifiedBytes")
        if isinstance(original_bytes, int) and original_bytes >= 0:
            minified_result["minifierInputBytes"] = original_bytes
        if isinstance(minified_bytes, int) and minified_bytes >= 0:
            minified_result["minifiedSourceSizeBytes"] = minified_bytes
        if (
            isinstance(original_bytes, int)
            and original_bytes > 0
            and isinstance(minified_bytes, int)
            and minified_bytes >= 0
        ):
            minified_result["minificationRatio"] = round(
                minified_bytes / original_bytes, 6
            )
    results.append(
        _enrich_result(
            minified_result,
            source_size_bytes,
            features,
            flags,
            time.perf_counter() - minifier_started,
        )
    )
    return results


def run_selected_tests(
    repo: Test262Repository,
    broiler_dll: str,
    expanded_paths: list[str],
    selection: dict[str, object],
    timeout_seconds: float,
    memory_limit_mb: int,
    max_workers: int = 1,
    include_negative: bool = False,
    minifier: object | None = None,
    reference_engine: object | None = None,
) -> list[dict[str, object]]:
    if not math.isfinite(timeout_seconds) or timeout_seconds <= 0:
        raise ValueError(f"timeout_seconds must be finite and positive, got {timeout_seconds}")
    if max_workers <= 0:
        raise ValueError(f"max_workers must be a positive integer, got {max_workers}")

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
    if minifier is not None:
        log_progress(
            f"Each path will run as original and {getattr(minifier, 'name', TERSER_VARIANT)} "
            f"variants ({getattr(minifier, 'profile', TERSER_PROFILE)}, "
            f"version {getattr(minifier, 'version', 'unknown')})."
        )
    if reference_engine is not None:
        log_progress(
            f"A failing minified case is cross-checked against "
            f"{getattr(reference_engine, 'name', REFERENCE_ENGINE)} "
            f"{getattr(reference_engine, 'version', 'unknown')} before it counts as an "
            "engine failure."
        )
    if max_workers > 1:
        log_progress(f"Using {max_workers} parallel worker(s).")

    interval = calculate_progress_log_interval(total)

    if max_workers <= 1:
        return _run_tests_serial(
            repo, broiler_dll, expanded_paths, timeout_seconds,
            memory_limit_mb, interval, total, include_negative, minifier,
            reference_engine,
        )
    return _run_tests_parallel(
        repo, broiler_dll, expanded_paths, timeout_seconds,
        memory_limit_mb, interval, total, max_workers, include_negative, minifier,
        reference_engine,
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
    minifier: object | None,
    reference_engine: object | None = None,
) -> list[dict[str, object]]:
    harness_cache: dict[str, str] = {}
    results: list[dict[str, object]] = []
    passed = 0
    failed = 0
    skipped = 0
    timed_out = 0

    for index, path in enumerate(expanded_paths, start=1):
        path_results = run_test_variants_with_metadata(
            repo,
            broiler_dll,
            path,
            harness_cache,
            timeout_seconds,
            memory_limit_mb,
            include_negative,
            minifier,
            reference_engine,
        )
        results.extend(path_results)

        for result in path_results:
            status = str(result["status"])
            variant = str(result.get("variant", ORIGINAL_VARIANT))
            if status == "passed":
                passed += 1
            elif status == "failed":
                failed += 1
                log_progress(f"Failure {failed} at {path} [{variant}]")
            elif status == "timedOut":
                timed_out += 1
                log_progress(
                    f"Timeout {timed_out} at {path} [{variant}] after {timeout_seconds:g}s"
                )
            else:
                skipped += 1

        if interval is not None and (index % interval == 0 or index == total):
            log_progress(
                f"Completed {index}/{total} path(s), {len(results)} variant case(s) "
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
    minifier: object | None,
    reference_engine: object | None = None,
) -> list[dict[str, object]]:
    # Each worker gets its own harness cache to avoid contention.
    results_by_path: list[list[dict[str, object]]] = [[] for _ in range(total)]
    lock = threading.Lock()
    worker_state = threading.local()
    completed = 0
    passed = 0
    failed = 0
    skipped = 0
    timed_out = 0

    def _worker(index: int, path: str) -> tuple[int, list[dict[str, object]]]:
        harness_cache = getattr(worker_state, "harness_cache", None)
        if harness_cache is None:
            harness_cache = {}
            worker_state.harness_cache = harness_cache
        path_results = run_test_variants_with_metadata(
            repo, broiler_dll, path, harness_cache,
            timeout_seconds, memory_limit_mb, include_negative, minifier,
            reference_engine,
        )
        return index, path_results

    with concurrent.futures.ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {
            executor.submit(_worker, idx, path): idx
            for idx, path in enumerate(expanded_paths)
        }
        for future in concurrent.futures.as_completed(futures):
            submitted_index = futures[future]
            try:
                idx, path_results = future.result()
            except Exception as exc:  # defensive: the wrapper normally contains these
                idx = submitted_index
                result = _infrastructure_failure_result(expanded_paths[idx], exc)
                result.update(
                    {
                        "sourceSizeBytes": None,
                        "features": [],
                        "flags": [],
                        "durationMs": 0.0,
                    }
                )
                path_results = [result]
            results_by_path[idx] = path_results

            with lock:
                completed += 1
                for result in path_results:
                    status = str(result["status"])
                    variant = str(result.get("variant", ORIGINAL_VARIANT))
                    if status == "passed":
                        passed += 1
                    elif status == "failed":
                        failed += 1
                        log_progress(
                            f"Failure {failed} at {result['path']} [{variant}]"
                        )
                    elif status == "timedOut":
                        timed_out += 1
                        log_progress(
                            f"Timeout {timed_out} at {result['path']} [{variant}] "
                            f"after {timeout_seconds:g}s"
                        )
                    else:
                        skipped += 1

                if interval is not None and (
                    completed % interval == 0 or completed == total
                ):
                    log_progress(
                        f"Completed {completed}/{total} path(s), "
                        f"{passed + failed + skipped + timed_out} variant case(s) "
                        f"(passed={passed}, failed={failed}, "
                        f"skipped={skipped}, timedOut={timed_out})."
                    )

    return [result for path_results in results_by_path for result in path_results]


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
        "--no-summary-stdout",
        action="store_true",
        help="Do not print the final JSON summary to stdout (normally paired with --output)",
    )
    parser.add_argument(
        "--all-script-host-verifiable",
        action="store_true",
        help="Discover and run every current script-host-verifiable test262 file",
    )
    parser.add_argument(
        "--subset",
        default="",
        help=(
            "Semicolon-separated test path, directory-prefix, or glob filters; "
            "empty runs the base selection unchanged"
        ),
    )
    parser.add_argument(
        "--features",
        default="",
        help=(
            "Semicolon-separated test262 feature metadata patterns (wildcards allowed); "
            "empty includes every feature set"
        ),
    )
    parser.add_argument(
        "--feature-match",
        choices=("any", "all"),
        default="any",
        help="Require any or all --features patterns to match a test's metadata",
    )
    parser.add_argument(
        "--selection-label",
        default="",
        help="Optional CI scope label recorded in the machine-readable summary",
    )
    parser.add_argument("--runner-os", default=platform.system())
    parser.add_argument("--runner-arch", default=platform.machine())
    parser.add_argument("--dotnet-version", default="")
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
        "--minifier",
        choices=("none", *MINIFIER_VARIANTS),
        default="none",
        help=(
            "Also execute each selected path after minification: terser applies the "
            "conservative test262-safe mangle, closure applies Closure Compiler's "
            "ADVANCED_OPTIMIZATIONS against the ES2026 standard"
        ),
    )
    parser.add_argument(
        "--terser-module",
        default="",
        help="Installed Terser package directory (required with --minifier terser)",
    )
    parser.add_argument(
        "--closure-module",
        default="",
        help=(
            "Installed google-closure-compiler package directory "
            "(required with --minifier closure)"
        ),
    )
    parser.add_argument(
        "--minifier-timeout-seconds",
        type=positive_float,
        default=DEFAULT_MINIFIER_TIMEOUT_SECONDS,
        help="Per-source minifier transformation timeout in seconds",
    )
    parser.add_argument(
        "--reference-engine-bin",
        default=REFERENCE_ENGINE,
        help=(
            "Second engine consulted about a FAILING minified case before it is attributed "
            "to Broiler (default: node, which every minified variant already requires)"
        ),
    )
    parser.add_argument(
        "--no-reference-cross-check",
        action="store_true",
        help=(
            "Attribute every failing minified case to the engine without a second opinion. "
            "Terser renames what fn-name tests measure and sometimes emits source no engine "
            "accepts, and Closure's ADVANCED profile renames properties on top of that, so "
            "expect minifier artefacts among the reported failures."
        ),
    )
    parser.add_argument(
        "--memory-limit-mb",
        type=non_negative_int,
        default=0,
        help="Optional POSIX per-test address-space limit in MiB; pass 0 to disable",
    )
    parser.add_argument(
        "--max-workers",
        type=positive_int,
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

    max_workers = args.max_workers
    subset_patterns = parse_semicolon_patterns(args.subset, normalize_paths=True)
    feature_patterns = parse_semicolon_patterns(args.features)

    log_progress(
        f"Starting test262 run for suite ref {args.suite_ref}"
        + (f" using local suite root {args.suite_root}" if args.suite_root else "")
    )

    repo = Test262Repository(args.suite_ref, args.suite_root)
    TEMP_DIRECTORY.mkdir(parents=True, exist_ok=True)

    # The minifier is built after the repository because the Closure variant needs the
    # pinned suite's own harness directory: those files are its externs, and they are what
    # keep ADVANCED from renaming the assertion API out from under every test.
    minifier: WorkerMinifier | None = None
    host_externs_names: list[str] = []
    if args.minifier == TERSER_VARIANT:
        if not args.terser_module.strip():
            parser.error("--terser-module is required with --minifier terser")
        try:
            minifier = TerserMinifier(
                args.terser_module,
                args.minifier_timeout_seconds,
            )
            minifier.probe()
        except (MinifierError, ValueError) as exc:
            parser.error(str(exc))
    elif args.minifier == CLOSURE_VARIANT:
        if not args.closure_module.strip():
            parser.error("--closure-module is required with --minifier closure")
        try:
            # The engine describes itself once per shard, and the description is externs:
            # without it ADVANCED renames every host API Closure's own externs do not
            # mention, and the variant reports the engine's newest builtins as its defects.
            host_externs_names = collect_host_property_names(args.broiler_dll)
            host_externs = write_host_externs(
                host_externs_names, TEMP_DIRECTORY / HOST_EXTERNS_NAME
            )
            log_progress(
                f"The engine reports {len(host_externs_names)} property name(s) of its "
                f"own; ADVANCED will not rename those."
            )
            minifier = ClosureMinifier(
                args.closure_module,
                repo.ensure_local_suite_root() / "harness",
                args.minifier_timeout_seconds,
                host_externs,
            )
            minifier.probe()
        except (MinifierError, ValueError, OSError, RuntimeError) as exc:
            parser.error(str(exc))

    reference_engine: ReferenceEngine | None = None
    if minifier is not None and not args.no_reference_cross_check:
        try:
            reference_engine = ReferenceEngine(
                args.reference_engine_bin,
                DEFAULT_REFERENCE_ENGINE_TIMEOUT_SECONDS,
            )
        except (ReferenceEngineError, ValueError) as exc:
            parser.error(
                f"{exc}. Pass --no-reference-cross-check to run without a second opinion."
            )

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
            subset_patterns=subset_patterns,
            feature_patterns=feature_patterns,
            feature_match=args.feature_match,
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

    try:
        results = run_selected_tests(
            repo,
            args.broiler_dll,
            expanded_paths,
            selection,
            args.test_timeout_seconds,
            args.memory_limit_mb,
            max_workers=max_workers,
            include_negative=args.include_negative,
            minifier=minifier,
            reference_engine=reference_engine,
        )
    finally:
        if minifier is not None:
            minifier.close()
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
        selection_label=args.selection_label,
        runner_os=args.runner_os,
        runner_arch=args.runner_arch,
        dotnet_version=args.dotnet_version,
        minifier=args.minifier,
        reference_engine=(
            str(getattr(reference_engine, "name", "")) if reference_engine is not None else ""
        ),
        minifier_version=minifier.version if minifier is not None else "",
        minifier_profile=minifier.profile if minifier is not None else "",
        minifier_timeout_seconds=(
            args.minifier_timeout_seconds if minifier is not None else 0.0
        ),
        minifier_host_extern_names=len(host_externs_names),
    )

    if args.output:
        output_path = Path(args.output)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
        log_progress(f"Wrote machine-readable summary to {output_path}")

    log_progress(
        f"Finished test262 run: variantCases={summary['total']}, "
        f"executed={summary['executed']}, "
        f"passed={summary['passed']}, failed={summary['failed']}, "
        f"skipped={summary['skipped']}, timedOut={summary['timedOut']}"
    )
    if not args.no_summary_stdout:
        print(json.dumps(summary, indent=2))
    has_failures = summary["failed"] > 0 or summary["timedOut"] > 0
    return 1 if has_failures else 0


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == INTERNAL_EXEC_WITH_LIMITS:
        sys.exit(exec_with_test_process_limits(sys.argv[2:]))
    sys.exit(main())
