#!/usr/bin/env node

"use strict";

const fs = require("node:fs");
const path = require("node:path");
const readline = require("node:readline");

const PROFILE = "test262-safe-mangle-v1";
const moduleRoot = path.resolve(process.argv[2] || "");

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

const options = Object.freeze({
  // Test262 deliberately observes edge-case semantics (including redefined
  // built-ins and completion values). Formatting plus identifier mangling
  // stresses the compact syntax Broiler sees on the web without letting
  // compressor assumptions turn valid conformance tests into false failures.
  compress: false,
  mangle: {
    keep_classnames: true,
    keep_fnames: true,
    eval: false,
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
});

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
    const result = await terser.minify(
      { [String(request.path || "test262.js")]: request.source },
      options,
    );
    if (typeof result.code !== "string") {
      throw new Error("Terser returned no code");
    }
    return { id, ok: true, code: result.code };
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
