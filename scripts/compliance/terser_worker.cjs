#!/usr/bin/env node

"use strict";

const fs = require("node:fs");
const path = require("node:path");
const readline = require("node:readline");

const PROFILE = "test262-safe-mangle-v2";
const moduleRoot = path.resolve(process.argv[2] || "");

// A quoted spelling that is also an identifier is a name the test TALKS ABOUT: every
// `fn-name-*` case asserts the name an anonymous function inherited from its binding
// (`assert.sameValue(fn.name, 'fn')`), and mangling that binding to `e` makes the
// assertion compare against a name the program no longer contains. Reserving those
// spellings keeps the test measuring what it measures while everything it does not name
// is still mangled. Over-reserving is harmless — a quoted word that happens to look like
// an identifier only costs one mangling opportunity — so this reads the source text
// rather than an AST Terser does not expose.
const STRING_LITERAL = /(['"])((?:\\.|(?!\1)[^\\\r\n])*)\1/g;
const IDENTIFIER = /^[A-Za-z_$][A-Za-z0-9_$]*$/;
// Terser 5 has no auto-accessor. It does not reject `accessor #x = 1`; it reads it as a
// field named `accessor` followed by a field `#x` and prints `accessor;#x=1`, so the
// class it emits has a public `accessor` property, no getter/setter pair, and a private
// field where the test declared an auto-accessor. That is a different program, not a
// minified one. Both halves must hold before the case is called unminifiable: the source
// declares an auto-accessor — `accessor` and the member name on one line, as the grammar
// requires, and not one of the operators that may follow the plain identifier `accessor`
// (`for (let accessor in {})`), which a test is free to use as an ordinary name — and the
// output really did split it into a bare `accessor` element, so the day Terser learns the
// syntax this stops firing on its own.
const AUTO_ACCESSOR_SOURCE =
  /(?:^|[;{}\s])(?:static[ \t]+)?accessor[ \t]+(?!(?:in|of|instanceof)\b)(?:#|[A-Za-z_$]|\[|"|')/;
const AUTO_ACCESSOR_SPLIT = /(?:^|[;{}\s])accessor[ \t]*[;}]/;

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

function droppedAutoAccessor(source, code) {
  return AUTO_ACCESSOR_SOURCE.test(source) && AUTO_ACCESSOR_SPLIT.test(code);
}

function write(response) {
  process.stdout.write(`${JSON.stringify(response)}\n`);
}

let terser;
let version;
try {
  terser = require(moduleRoot);
  const packageJson = JSON.parse(
    fs.readFileSync(path.join(moduleRoot, "package.json"), "utf8"),
  );
  version = String(packageJson.version || "");
  if (!version || typeof terser.minify !== "function") {
    throw new Error("package does not expose the expected Terser API");
  }
} catch (error) {
  process.stderr.write(
    `Could not load Terser from ${moduleRoot}: ${error.stack || error}\n`,
  );
  process.exit(2);
}

function profileOptions(reserved) {
  return {
    // Test262 deliberately observes edge-case semantics (including redefined
    // built-ins and completion values). Formatting plus identifier mangling
    // stresses the compact syntax Broiler sees on the web without letting
    // compressor assumptions turn valid conformance tests into false failures.
    compress: false,
    mangle: {
      keep_classnames: true,
      keep_fnames: true,
      eval: false,
      reserved,
    },
    keep_classnames: true,
    keep_fnames: true,
    module: false,
    toplevel: false,
    format: {
      // Preserve observable RegExp/string source spelling in conformance tests.
      ascii_only: false,
      comments: false,
      semicolons: true,
    },
  };
}

async function handle(request) {
  const id = request && request.id;
  if (request && request.operation === "hello") {
    return { id, ok: true, profile: PROFILE, version };
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
    const reserved = reservedNames(request.source);
    const result = await terser.minify(
      { [String(request.path || "test262.js")]: request.source },
      profileOptions(reserved),
    );
    if (typeof result.code !== "string") {
      throw new Error("Terser returned no code");
    }
    const response = { id, ok: true, code: result.code, reserved };
    if (droppedAutoAccessor(request.source, result.code)) {
      response.unsupportedSyntax = "auto-accessor";
    }
    return response;
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

const lines = readline.createInterface({
  input: process.stdin,
  crlfDelay: Infinity,
  terminal: false,
});

let chain = Promise.resolve();
lines.on("line", (line) => {
  chain = chain.then(async () => {
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
    write(await handle(request));
  });
});

chain.catch((error) => {
  process.stderr.write(`${error.stack || error}\n`);
  process.exitCode = 3;
});
