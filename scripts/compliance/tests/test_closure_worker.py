"""Behavioural tests for the Closure worker's crash classification.

The worker decides, from the compiler's own stderr, whether a non-zero exit that reported no
error diagnostic is the compiler declining THIS PROGRAM (a not-applicable case) or this harness
being wrong about the compilation (a failure that must stay a failure). The rule is what the
crash named as the file it died in, so these drive the worker with a stub compiler that prints
recorded crash text: no Closure Compiler, no JVM, and no dependence on a compiler release
continuing to crash on the same source.
"""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

WORKER = Path(__file__).resolve().parents[1] / "closure_worker.cjs"

# The stack frames are what tells a JVM crash from ordinary stderr; the `Node(…)`/`Parent(…)`
# lines are how Compiler.throwInternalError names the node it died on. Trimmed from the real
# crash of ConvertToDottedProperties on `class C { "a"() {} }`.
THROW_INTERNAL_ERROR_CRASH = """[{"level":"info","description":"0 error(s), 0 warning(s)"}]\
java.lang.RuntimeException: INTERNAL COMPILER ERROR.
Please report this problem.

null
  Node(CLASS_MEMBERS): {source}:7:0
class C {
  Parent(CLASS): {source}:7:0
class C {

\tat com.google.javascript.jscomp.Compiler.throwInternalError(Compiler.java:3469)
\tat com.google.javascript.jscomp.NodeTraversal.traverse(NodeTraversal.java:536)
Caused by: java.lang.NullPointerException
\tat com.google.javascript.jscomp.ConvertToDottedProperties.visit(ConvertToDottedProperties.java:58)
"""

# The other spelling, from a pass that fails inside its own traversal.
SOURCE_FILE_CRASH = """java.lang.NullPointerException: null
  [source_file: {source}]

\tat com.google.javascript.jscomp.RemoveUnusedCode.traverseNode(RemoveUnusedCode.java:611)
"""


def crash(template: str, source: str) -> str:
    """The recorded crash text with the file it names substituted in."""
    return template.replace("{source}", source)


NATIVE_PACKAGES = {
    ("linux", "x64"): "google-closure-compiler-linux",
    ("linux", "arm64"): "google-closure-compiler-linux-arm64",
    ("darwin", "x64"): "google-closure-compiler-macos",
    ("darwin", "arm64"): "google-closure-compiler-macos",
}


def native_package_name() -> str | None:
    platform = "darwin" if sys.platform == "darwin" else "linux" if sys.platform.startswith("linux") else sys.platform
    machine = os.uname().machine if hasattr(os, "uname") else ""
    arch = "arm64" if machine in ("arm64", "aarch64") else "x64" if machine in ("x86_64", "amd64") else machine
    return NATIVE_PACKAGES.get((platform, arch))


@unittest.skipUnless(shutil.which("node"), "Node.js is required to run the worker")
@unittest.skipUnless(native_package_name(), "no native Closure package name for this platform")
class ClosureWorkerCrashClassificationTests(unittest.TestCase):
    def setUp(self) -> None:
        self.root = Path(tempfile.mkdtemp(prefix="broiler-closure-worker-"))
        self.addCleanup(shutil.rmtree, self.root, ignore_errors=True)

        self.harness = self.root / "harness"
        self.harness.mkdir()
        for name in ("assert.js", "sta.js"):
            (self.harness / name).write_text("var harness;\n", encoding="utf8")

        self.host_externs = self.root / "host-externs.js"
        self.host_externs.write_text("/** @fileoverview @externs */\n", encoding="utf8")

        modules = self.root / "node_modules"
        self.module = modules / "google-closure-compiler"
        self.module.mkdir(parents=True)
        (self.module / "package.json").write_text(
            json.dumps({"name": "google-closure-compiler", "version": "0.test"}),
            encoding="utf8",
        )
        native = modules / native_package_name()
        native.mkdir()
        self.compiler = native / "compiler"

    def install_stub(self, stderr: str, status: int = 254) -> None:
        self.compiler.write_text(
            "#!/bin/sh\ncat >/dev/null\ncat 1>&2 <<'BROILER_STUB_EOF'\n"
            f"{stderr}\nBROILER_STUB_EOF\nexit {status}\n",
            encoding="utf8",
        )
        self.compiler.chmod(0o755)

    def minify(self, source: str = "class C {}") -> dict:
        requests = [{"id": 1, "operation": "minify", "path": "x.js", "source": source}]
        completed = subprocess.run(
            [
                "node",
                str(WORKER),
                str(self.module),
                str(self.harness),
                "30000",
                str(self.host_externs),
            ],
            input="\n".join(json.dumps(request) for request in requests) + "\n",
            capture_output=True,
            text=True,
            timeout=60,
        )
        self.assertEqual(0, completed.returncode, completed.stderr)
        lines = [line for line in completed.stdout.splitlines() if line.strip()]
        self.assertEqual(1, len(lines), completed.stdout)
        return json.loads(lines[0])

    def test_throw_internal_error_naming_the_test_source_is_a_declined_program(self) -> None:
        # Nothing came out of the compiler, and what it died on was the test's own source, so
        # there is no minified variant whose result could be about the engine.
        self.install_stub(crash(THROW_INTERNAL_ERROR_CRASH, "test262-source.js"))
        response = self.minify()
        self.assertFalse(response["ok"])
        self.assertEqual("InternalError", response["errorName"])
        self.assertIn("INTERNAL COMPILER ERROR", response["errorMessage"])

    def test_source_file_crash_is_still_a_declined_program(self) -> None:
        self.install_stub(crash(SOURCE_FILE_CRASH, "test262-source.js"))
        self.assertEqual("InternalError", self.minify()["errorName"])

    def test_crash_naming_an_externs_file_remains_a_failure(self) -> None:
        # A crash in externs is this harness being wrong about the compilation rather than the
        # compiler being wrong about the test, so it must keep failing the run.
        self.install_stub(crash(THROW_INTERNAL_ERROR_CRASH, "host-externs.js"))
        response = self.minify()
        self.assertFalse(response["ok"])
        self.assertEqual("Error", response["errorName"])

    def test_crash_naming_no_source_at_all_remains_a_failure(self) -> None:
        self.install_stub(
            "java.lang.OutOfMemoryError: Java heap space\n"
            "\tat com.google.javascript.jscomp.Compiler.compile(Compiler.java:1)\n"
        )
        self.assertEqual("Error", self.minify()["errorName"])

    def test_a_non_zero_exit_with_no_stack_at_all_remains_a_failure(self) -> None:
        self.install_stub("killed")
        self.assertEqual("Error", self.minify()["errorName"])


if __name__ == "__main__":
    unittest.main()
