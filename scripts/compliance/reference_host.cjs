#!/usr/bin/env node

"use strict";

// Runs one assembled test262 program under Node as the SCRIPT it is.
//
// `node program.cjs` does not do that: Node wraps a CommonJS file in a function body, where
// a top-level `var` is a local of that function rather than a property of the global object.
// Every test whose subject is that distinction then fails under Node no matter how correct
// Node is — the Annex B eval-code cases declare a function inside `(0,eval)(...)` and read
// the binding back from the enclosing scope, and the global-object cases look their own
// `var` up on `globalThis`. run_test262.py asks this engine whether a FAILING minified case
// is still the test, and reads "could not run the original either" as "no opinion, keep the
// engine's verdict", so a Node that cannot run those tests as written turns each of them
// into a Broiler failure that Broiler did not earn.
//
// vm.runInThisContext compiles the program as a script in the real global scope, which is
// the scope test262 wrote it for, and vm.Script is the same parse without the run — the
// script-mode `--check` that `node --check` (module-mode) is not.

const fs = require("node:fs");
const vm = require("node:vm");

const checkOnly = process.argv[2] === "--check";
const scriptPath = checkOnly ? process.argv[3] : process.argv[2];

if (!scriptPath) {
  process.stderr.write("usage: reference_host.cjs [--check] PROGRAM\n");
  process.exit(2);
}

const source = fs.readFileSync(scriptPath, "utf8");

// `print` is a JavaScript-shell global that Node does not have, and test262's async
// protocol is printed: harness/doneprintHandle.js reports a test's completion by calling it.
// Without the binding every async program dies of a ReferenceError before it settles, and
// the reference engine's verdict on an async test would be "cannot run the original either"
// for reasons that have nothing to do with the test. The engine under test defines the same
// global in its script host, so both sides of the comparison print through one name.
if (typeof globalThis.print !== "function") {
  globalThis.print = function print(...values) {
    process.stdout.write(values.map(String).join(" ") + "\n");
  };
}

// Nothing is caught: a syntax error or an uncaught exception must leave the process with
// Node's own non-zero exit and Node's own diagnostic, exactly as running the file did.
if (checkOnly) {
  new vm.Script(source, { filename: scriptPath });
} else {
  vm.runInThisContext(source, { filename: scriptPath });
}
