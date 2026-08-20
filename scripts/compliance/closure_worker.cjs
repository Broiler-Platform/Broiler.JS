#!/usr/bin/env node

"use strict";

const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const readline = require("node:readline");
const { spawnSync } = require("node:child_process");

const PROFILE = "test262-closure-advanced-v1";
// Closure's ADVANCED_OPTIMIZATIONS is the whole point of this variant: it renames
// variables AND properties, drops code it proves unreachable, inlines functions, and
// collapses namespaces. Terser's profile is deliberately conservative; this one is
// deliberately not, so the engine is measured against the shape real ADVANCED-compiled
// production bundles have.
const COMPILATION_LEVEL = "ADVANCED_OPTIMIZATIONS";
// The requested standard is ECMAScript 2026. Closure has no ECMASCRIPT_2026 language
// mode — its flag parser accepts year names only up to ECMASCRIPT_2022 (and the compiler
// then throws `Unexpected language mode` on it), and names the current draft standard
// ECMASCRIPT_NEXT: "latest features supported". ECMASCRIPT_NEXT is therefore the ES2026
// setting, in and out, and the handshake reports the year separately so a future release
// that grows a real ECMASCRIPT_2026 mode is a one-line change here.
const LANGUAGE = "ECMASCRIPT_NEXT";
const LANGUAGE_YEAR = 2026;
// Harness globals (`assert`, `verifyProperty`, `compareArray`, …) are concatenated ahead
// of the body by the runner, not compiled with it, so within this compilation unit they
// are free names whose properties ADVANCED would happily rename. The harness sources are
// fed in as externs, which declares exactly that surface — every global the test may call
// and every property it may read off one — without a hand-maintained list that would rot
// against the pinned suite.
const ALWAYS_INCLUDED_HARNESS = ["assert.js", "sta.js"];
// Everything else the test names — a host builtin the compiler has no externs for, the
// `$DONE` the runner's own async prelude defines — is simply an undeclared global. Under
// ADVANCED that is an error, not a warning, and `undefinedVars` is the check that says so.
// The engine runs those tests, so refusing to compile them would report a Closure gap as a
// Broiler failure; the name itself is left alone either way.
const SUPPRESSED_DIAGNOSTIC_GROUPS = ["undefinedVars"];

const moduleRoot = path.resolve(process.argv[2] || "");
const harnessRoot = path.resolve(process.argv[3] || "");
// A ceiling for one compiler process. The Python side times each request out on its own
// and replaces the worker when it does; this only stops an unkillable compiler from
// holding a worker thread forever, so it is deliberately the looser of the two.
const PROCESS_TIMEOUT_MS = Number.parseInt(process.argv[4] || "", 10) || 300000;
// Externs for the host the compiled body will run on, written by the Python side from what
// the engine under test says it implements. Closure's bundled externs stop at the standard
// surface its own release knows, so without this ADVANCED renames `Temporal.Duration` to
// `Temporal.h` and every test of a newer builtin measures a program that can no longer
// reach it. Required rather than optional: a Closure run without it reports the engine's
// own API as the engine's own failures, which is worse than not running at all.
const hostExterns = path.resolve(process.argv[5] || "");

// See terser_worker.cjs: a quoted spelling that is also an identifier is a name the test
// TALKS ABOUT. Terser answers that with `mangle.reserved`; Closure's equivalent is an
// externs declaration, because a property name that appears anywhere in externs is exempt
// from ADVANCED's property renaming. So `verifyProperty(obj, 'bar', …)` keeps working on
// an object whose `bar` still spells `bar`, while everything the test does not name is
// renamed as ADVANCED renames it.
const STRING_LITERAL = /(['"])((?:\\.|(?!\1)[^\\\r\n])*)\1/g;
const IDENTIFIER = /^[A-Za-z_$][A-Za-z0-9_$]*$/;
// The body is compiled as the runner will run it: strict only when the runner put a
// directive at byte zero (an `onlyStrict` test), sloppy otherwise, so Closure does not
// optimize a sloppy body under assumptions only strict mode licenses.
const LEADING_USE_STRICT = /^\s*(?:\/\/[^\n]*\n|\/\*[\s\S]*?\*\/\s*)*['"]use strict['"]\s*;/;
const SOURCE_NAME = "test262-source.js";
const RESERVED_EXTERNS_NAME = "test262-reserved-names.js";

function quotedSpellings(source) {
  const spellings = new Set();
  for (const match of source.matchAll(STRING_LITERAL)) {
    spellings.add(match[2]);
  }
  return spellings;
}

function reservedNames(source) {
  return [...quotedSpellings(source)].filter((value) => IDENTIFIER.test(value));
}

// An externs file whose only job is to make these property names known. The holder is
// typed `?` so the compiler asks nothing about the object itself, and each name appears as
// a property access, which is all the renaming pass reads.
function reservedExternsSource(reserved) {
  const lines = [
    "/** @fileoverview @externs */",
    "/** @type {?} */ var __broilerTest262ReservedNames;",
  ];
  for (const name of reserved) {
    lines.push(`__broilerTest262ReservedNames.${name};`);
  }
  return `${lines.join("\n")}\n`;
}

function write(response) {
  process.stdout.write(`${JSON.stringify(response)}\n`);
}

// The npm package ships the compiler as a platform-native image next to itself and a JAR
// inside it; prefer the native image (it starts in a fraction of the JVM's time) and fall
// back to `java -jar` so a platform without a published image still runs.
function resolveCompilerCommand(root) {
  const siblings = path.dirname(root);
  const nativePackages = {
    "linux-x64": "google-closure-compiler-linux",
    "linux-arm64": "google-closure-compiler-linux-arm64",
    "darwin-x64": "google-closure-compiler-macos",
    "darwin-arm64": "google-closure-compiler-macos",
    "win32-x64": "google-closure-compiler-windows",
  };
  const nativePackage = nativePackages[`${process.platform}-${process.arch}`];
  if (nativePackage) {
    const binary = path.join(
      siblings,
      nativePackage,
      process.platform === "win32" ? "compiler.exe" : "compiler",
    );
    if (fs.existsSync(binary)) {
      return { command: binary, prefix: [] };
    }
  }
  const jar = path.join(siblings, "google-closure-compiler-java", "compiler.jar");
  if (fs.existsSync(jar)) {
    return { command: "java", prefix: ["-jar", jar] };
  }
  throw new Error(
    `no Closure Compiler image or compiler.jar next to ${root}`,
  );
}

let version;
let compiler;
let temporaryRoot;
try {
  const packageJson = JSON.parse(
    fs.readFileSync(path.join(moduleRoot, "package.json"), "utf8"),
  );
  version = String(packageJson.version || "");
  if (!version) {
    throw new Error("package does not report a version");
  }
  if (packageJson.name !== "google-closure-compiler") {
    throw new Error(`package is ${packageJson.name}, not google-closure-compiler`);
  }
  for (const name of ALWAYS_INCLUDED_HARNESS) {
    if (!fs.existsSync(path.join(harnessRoot, name))) {
      throw new Error(`test262 harness/${name} was not found under ${harnessRoot}`);
    }
  }
  // A missing argument resolves to the working directory, which exists — so this asks for
  // the file itself rather than for something at that path.
  if (!process.argv[5] || !fs.statSync(hostExterns, { throwIfNoEntry: false })?.isFile()) {
    throw new Error(`the engine's own externs were not found at ${hostExterns}`);
  }
  compiler = resolveCompilerCommand(moduleRoot);
  temporaryRoot = fs.mkdtempSync(path.join(os.tmpdir(), "broiler-closure-"));
} catch (error) {
  process.stderr.write(
    `Could not load the Closure Compiler from ${moduleRoot}: ${error.stack || error}\n`,
  );
  process.exit(2);
}

function externsArguments(source, includes) {
  const names = [...ALWAYS_INCLUDED_HARNESS];
  for (const include of includes) {
    if (typeof include === "string" && include && !names.includes(include)) {
      names.push(include);
    }
  }

  const args = ["--externs", hostExterns];
  for (const name of names) {
    // `path.basename` keeps a metadata `includes:` entry from reaching outside the pinned
    // harness directory. A name the pinned suite does not have is skipped rather than
    // failing the compile: the runner already refuses to run a test whose includes it
    // cannot resolve, so there is nothing here to protect.
    const resolved = path.join(harnessRoot, path.basename(name));
    if (fs.existsSync(resolved)) {
      args.push("--externs", resolved);
    }
  }

  const reserved = reservedNames(source);
  if (reserved.length > 0) {
    const reservedPath = path.join(temporaryRoot, RESERVED_EXTERNS_NAME);
    fs.writeFileSync(reservedPath, reservedExternsSource(reserved), "utf8");
    args.push("--externs", reservedPath);
  }
  return { args, reserved };
}

function compilerArguments(source, includes) {
  const { args: externs, reserved } = externsArguments(source, includes);
  const args = [
    ...compiler.prefix,
    "--json_streams",
    "IN",
    "--compilation_level",
    COMPILATION_LEVEL,
    "--language_in",
    LANGUAGE,
    "--language_out",
    LANGUAGE,
    "--warning_level",
    "QUIET",
    "--error_format",
    "JSON",
    // The runner re-adds the directive at byte zero itself, exactly as it does for the
    // original variant, so the output must not carry a second one.
    "--emit_use_strict=false",
    `--strict_mode_input=${LEADING_USE_STRICT.test(source)}`,
    // The body is a script the host evaluates as-is. Wrapping it in an IIFE, or letting
    // the compiler rewrite it as a module, would change what `var` declarations and `this`
    // mean at the top level — the very things a conformance test is often measuring.
    "--isolation_mode",
    "NONE",
    "--process_common_js_modules=false",
    "--rewrite_polyfills=false",
  ];
  for (const group of SUPPRESSED_DIAGNOSTIC_GROUPS) {
    args.push(`--jscomp_off=${group}`);
  }
  args.push(...externs);
  return { args, reserved };
}

// Closure can also die on a program it accepted. `for (x.y of [23])` — a MemberExpression
// as a for-of target, which the grammar allows and every engine runs — walks
// RemoveUnusedCode into a NullPointerException, and the `import(specifier)` a script may
// contain reaches an "AST should not contain Dynamic module import" assertion. The process
// exits non-zero having reported no error diagnostic at all, so classifyDiagnostics sees
// nothing; what it does report is the AST node it died on, and the file that node came
// from. When every file it names is the test's own source, the compiler is declining THIS
// PROGRAM as surely as a parse error is, and there is no minified variant either way. A
// crash that names an externs file, or names no source at all, is this harness being wrong
// about the compilation rather than the compiler being wrong about the test, so it stays a
// failure.
const JVM_STACK_FRAME = /\n\s+at [\w$.]+\(/;
const JVM_THROWABLE = /(?:[a-z]\w*\.)+[A-Z]\w*(?:Exception|Error)\b[^\n]*/;
const SOURCE_FILE_REFERENCE = /\[source_file: ([^\]]*)\]/g;
// Closure names the node it died on in two spellings, and which one it uses is a property of
// the crash rather than of the program. A pass that fails inside its own traversal reports
// `[source_file: …]`; one that reaches Compiler.throwInternalError reports the offending node
// and its parent instead, as `Node(CLASS_MEMBERS): test262-source.js:7:0`. Both are the same
// fact — the file the compiler was reading when it died — so both are read here. Without the
// second, ConvertToDottedProperties walking a class whose members carry string-literal names
// into a NullPointerException, and `super[0] = …` reaching a "no superclass" assertion, were
// crashes that named nothing this could recognise and so stayed infrastructure failures.
const CRASH_NODE_REFERENCE = /\b(?:Node|Parent)\([A-Z_]+\): ([^\s:]+):\d+/g;
const CRASH_SUMMARY_LIMIT = 200;

function classifyCrash(stderr) {
  if (!JVM_STACK_FRAME.test(stderr)) {
    return null;
  }
  const named = [
    ...stderr.matchAll(SOURCE_FILE_REFERENCE),
    ...stderr.matchAll(CRASH_NODE_REFERENCE),
  ].map((match) => match[1]);
  if (named.length === 0 || named.some((name) => name !== SOURCE_NAME)) {
    return null;
  }
  const summary = (JVM_THROWABLE.exec(stderr) || ["compiler crashed"])[0].trim();
  return summary.length > CRASH_SUMMARY_LIMIT
    ? `${summary.slice(0, CRASH_SUMMARY_LIMIT)}…`
    : summary;
}

// A diagnostic anchored in the test's own source is Closure declining THIS PROGRAM —
// syntax it does not implement (decorators, `using`, auto-accessors, private `#x in o`,
// the RegExp `v` flag), which says nothing about the engine. A diagnostic with no source,
// or one anchored in an externs file, is this harness being wrong, and must keep failing
// the run rather than quietly becoming a skipped case.
function classifyDiagnostics(stderr) {
  let parsed;
  try {
    parsed = JSON.parse(stderr);
  } catch (error) {
    return null;
  }
  if (!Array.isArray(parsed)) {
    return null;
  }
  const errors = parsed.filter(
    (entry) => entry && typeof entry === "object" && entry.level === "error",
  );
  if (errors.length === 0) {
    return null;
  }
  const first = errors[0];
  const sourceLevel = errors.every((entry) => entry.source === SOURCE_NAME);
  return {
    sourceLevel,
    key: String(first.key || "JSC_ERROR"),
    description: String(first.description || "compilation failed"),
    line: Number.isInteger(first.line) ? first.line : undefined,
    column: Number.isInteger(first.column) ? first.column : undefined,
    count: errors.length,
  };
}

function compile(source, includes) {
  const { args, reserved } = compilerArguments(source, includes);
  const completed = spawnSync(compiler.command, args, {
    input: JSON.stringify([{ path: SOURCE_NAME, src: source }]),
    encoding: "utf8",
    timeout: PROCESS_TIMEOUT_MS,
    maxBuffer: 64 * 1024 * 1024,
  });

  if (completed.error) {
    const timedOut = completed.error.code === "ETIMEDOUT";
    throw Object.assign(
      new Error(
        timedOut
          ? `Closure Compiler exceeded ${PROCESS_TIMEOUT_MS}ms`
          : `Could not run the Closure Compiler: ${completed.error.message}`,
      ),
      { name: timedOut ? "TimeoutError" : "Error" },
    );
  }

  const stderr = String(completed.stderr || "").trim();
  if (completed.status !== 0) {
    const diagnostic = classifyDiagnostics(stderr);
    if (diagnostic === null) {
      const crash = classifyCrash(stderr);
      if (crash !== null) {
        // The Python side reads InternalError as "the minifier crashed on this source",
        // which is the same not-applicable fact as a declined parse with a different
        // diagnostic. See MinifierInternalError in run_test262.py.
        throw Object.assign(new Error(crash), { name: "InternalError" });
      }
      throw new Error(
        `Closure Compiler exited ${completed.status}${stderr ? `: ${stderr}` : ""}`,
      );
    }
    const suffix = diagnostic.count > 1 ? ` (+${diagnostic.count - 1} more)` : "";
    const error = new Error(`${diagnostic.key}: ${diagnostic.description}${suffix}`);
    // The Python side reads SyntaxError as "the minifier's parser declined this source",
    // which is exactly what a source-anchored Closure error means.
    error.name = diagnostic.sourceLevel ? "SyntaxError" : "ClosureCompilerError";
    error.line = diagnostic.line;
    error.col = diagnostic.column;
    throw error;
  }

  return { code: String(completed.stdout || ""), reserved };
}

function handle(request) {
  const id = request && request.id;
  if (request && request.operation === "hello") {
    return {
      id,
      ok: true,
      profile: PROFILE,
      version,
      compilationLevel: COMPILATION_LEVEL,
      language: LANGUAGE,
      languageYear: LANGUAGE_YEAR,
    };
  }
  if (!request || request.operation !== "minify") {
    return {
      id,
      ok: false,
      errorName: "ProtocolError",
      errorMessage: "unknown operation",
    };
  }
  if (typeof request.source !== "string") {
    return {
      id,
      ok: false,
      errorName: "ProtocolError",
      errorMessage: "source must be a string",
    };
  }

  try {
    const includes = Array.isArray(request.includes) ? request.includes : [];
    const { code, reserved } = compile(request.source, includes);
    return { id, ok: true, code, reserved };
  } catch (error) {
    return {
      id,
      ok: false,
      errorName: String(error && error.name ? error.name : "Error"),
      errorMessage: String(error && error.message ? error.message : error),
      line: Number.isInteger(error && error.line) ? error.line : undefined,
      column: Number.isInteger(error && error.col) ? error.col : undefined,
    };
  }
}

process.on("exit", () => {
  try {
    fs.rmSync(temporaryRoot, { recursive: true, force: true });
  } catch (error) {
    // A leftover empty temp directory is not worth failing a shard over.
  }
});

const lines = readline.createInterface({
  input: process.stdin,
  crlfDelay: Infinity,
  terminal: false,
});

lines.on("line", (line) => {
  let request;
  try {
    request = JSON.parse(line);
  } catch (error) {
    write({
      id: null,
      ok: false,
      errorName: "ProtocolError",
      errorMessage: `invalid JSON: ${error.message}`,
    });
    return;
  }
  write(handle(request));
});
