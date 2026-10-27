// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// The syntax area's independent check (corpus revision 29, milestone SJL-13's `syntax` vectors): runs
// every vector of the area under the reference Node.js v26.12.0 that bundle sjl-9-001's reference.txt names, and
// prints what it answers. generate_syntax_vectors.py --check-node starts it; it is no source of an expected value,
// which the generator states from the specification, and it only says where the reference agrees.
//
//   node scripts/conformance/syntaxcheck.mjs <requests.json> <prelude.js>
//
// A request is {"id", "op", "in", "script"}. For each, one JSON line on standard output:
//   {"id", "parse": "accepted" | "SyntaxError" | "<other>", "outcome": "<the schema's rendering>"}
// - "parse" is the reference's parse of `in` alone, as a Script (new vm.Script, which compiles and runs nothing).
//   PerformEval's eval code is parsed with the Script goal too (PerformEval step 11), and a top-level direct eval in a
//   fresh realm inherits no context, so the two parses answer alike for every vector of the area;
// - "outcome" is the vector's `script` evaluated as a Script in a fresh context after the prelude, rendered as the
//   corpus's SCHEMA.md section 7 renders it: a Number by its bits, a String by its code units, a thrown error object
//   by the name of its constructor.

import fs from "node:fs";
import vm from "node:vm";
import util from "node:util";

const [requestsPath, preludePath] = process.argv.slice(2);
const requests = JSON.parse(fs.readFileSync(requestsPath, "utf8"));
const prelude = fs.readFileSync(preludePath, "utf8");

function hex(n, width) {
  return n.toString(16).toUpperCase().padStart(width, "0");
}

function renderString(s) {
  let out = '"';
  for (let i = 0; i < s.length; i++) {
    const u = s.charCodeAt(i);
    if (u === 0x22) out += '\\"';
    else if (u === 0x5c) out += "\\\\";
    else if (u >= 0x20 && u <= 0x7e) out += String.fromCharCode(u);
    else out += "\\u" + hex(u, 4);
  }
  return out + '"';
}

function renderNumber(x) {
  if (Number.isNaN(x)) return "f64:0x7FF8000000000000";
  const view = new DataView(new ArrayBuffer(8));
  view.setFloat64(0, x);
  return "f64:0x" + hex(view.getUint32(0), 8) + hex(view.getUint32(4), 8);
}

function render(v) {
  if (v === undefined) return "undefined";
  if (v === null) return "null";
  switch (typeof v) {
    case "number": return renderNumber(v);
    case "string": return "str:" + renderString(v);
    case "boolean": return v ? "bool:true" : "bool:false";
    case "bigint": return "bigint:" + v.toString();
    case "symbol": return "symbol";
    default: return "object";
  }
}

function renderThrown(e) {
  if (util.types.isNativeError(e)) {
    const name = e.constructor && e.constructor.name;
    return typeof name === "string" ? "error: " + name : "fault: an error's constructor has no name";
  }
  return "throw: " + render(e);
}

for (const request of requests) {
  let parse;
  try {
    new vm.Script(request.in);
    parse = "accepted";
  } catch (e) {
    parse = util.types.isNativeError(e) ? e.constructor.name : "throw";
  }
  let outcome;
  try {
    const context = vm.createContext({});
    vm.runInContext(prelude, context, { filename: "prelude.js" });
    outcome = "value: " + render(vm.runInContext(request.script, context, { filename: "vector.js", timeout: 5000 }));
  } catch (e) {
    outcome = renderThrown(e);
  }
  process.stdout.write(JSON.stringify({ id: request.id, parse, outcome }) + "\n");
}
