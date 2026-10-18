// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// temporalref.mjs: the Temporal area's reference computation, `ref:temporalref@1` (corpus revision 24,
// milestone SJL-10.2). It is the proposal's own polyfill, `polyfill/lib` of tc39/proposal-temporal at
// e8cc03fc970a65a3359e8870e3b35e687ac94e55 (decision D10 of bundle sjl-10-001), driven by this file under
// Node 22.22.0. It is neither engine's code. generate_temporal_vectors.py writes the requests, runs this
// driver, checks the polyfill's files and its dependencies' versions against its own header, and reads
// the answers; it is the only caller.
//
//   node --js-regexp-duplicate-named-groups temporalref.mjs <polyfill dir> <prelude.js> <requests.json> \
//     <answers.json> [--text-nudge | --text-window]
//
// <polyfill dir> holds lib/ (the proposal's polyfill/lib at the pinned revision) and node_modules/ (its
// three runtime dependencies and theirs, as package-lock.json beside this file pins them). The driver
// imports lib/shim.mjs, which defines globalThis.Temporal, evaluates the corpus's prelude.js as a global
// script, then evaluates each request's script as a separate global script, sloppy, by
// vm.runInThisContext, and renders its outcome by the corpus's rendering rule (SCHEMA.md section 7):
//
//   value: <typed value>   a normal completion: a Number by its bit pattern, a String by its code units,
//                          a BigInt, a Boolean, undefined, null, `symbol` or `object`;
//   error: <Name>          a thrown object with an [[ErrorData]] slot, by its constructor's name;
//   throw: <typed value>   any other thrown value.
//
// Each script runs in the one realm the polyfill was installed in, not in a fresh realm per vector as an
// engine-level runner does: the polyfill is a set of modules and installs itself once. So the generator
// writes every script as one expression that declares no global, and this driver checks after every
// script that the global object's own properties are the ones it had before the first, and refuses the
// run otherwise.
//
// Two flags evaluate the same requests with one statement of the polyfill replaced by the pinned specification
// text's, each where the text and the polyfill are known to disagree; the generator takes no expected value from
// either run, but uses them to find every vector a disagreement reaches.
//
//   --text-nudge   NudgeToCalendarUnit's nudged instant (spec/duration.html at the pinned revision: "Else, Let
//                  resultDuration be startDuration. Let nudgedEpochNs be startEpochNs"), where the polyfill takes
//                  the window's end whenever the window was shifted (lib/ecmascript.mjs: `nudgedEpochNs:
//                  didExpandCalendarUnit ? endEpochNs : startEpochNs`). It is decision D7's case, row D8 of the
//                  correspondence, where the text rules: the generator requires every vector whose answer this
//                  changes to be one of its listed overrides.
//   --text-window  ComputeNudgeWindow's start ("If r1 = 0, then Let startEpochNs be originEpochNs"), where the
//                  polyfill tests whether the start duration is zero (`if (DateDurationSign(startDuration) === 0)`).
//                  Broiler.VM's JSD-0054 section 4 records that the text does not do what it means here and follows
//                  the polyfill, as Broiler.JS does; the generator records in each such vector's note what the
//                  literal text answers, and keeps the polyfill's answer.
//
// The answers file is JSON: { driver, versions, text, answers: [{ id, outcome }] }, text naming the flag or null.

import { readFileSync, writeFileSync } from 'node:fs';
import { register } from 'node:module';
import { pathToFileURL } from 'node:url';
import { types } from 'node:util';
import vm from 'node:vm';

const DRIVER = 'temporalref 1';
const PATCHES = {
  '--text-nudge': [
    'nudgedEpochNs: didExpandCalendarUnit ? endEpochNs : startEpochNs,',
    'nudgedEpochNs: roundedUnit === MathAbs(r2) ? endEpochNs : startEpochNs,'
  ],
  '--text-window': ['if (DateDurationSign(startDuration) === 0) {', 'if (r1 === 0) {']
};

const [polyfillDir, preludePath, requestsPath, answersPath, flag] = process.argv.slice(2);
if (!answersPath || (flag !== undefined && !(flag in PATCHES))) {
  process.stderr.write(
    'usage: temporalref.mjs <polyfill dir> <prelude.js> <requests.json> <answers.json> [--text-nudge | --text-window]\n'
  );
  process.exit(2);
}

if (flag !== undefined) {
  // A load hook that replaces the one statement in lib/ecmascript.mjs, and refuses unless it occurs once.
  const [from, to] = PATCHES[flag];
  const hook = `
    const from = ${JSON.stringify(from)};
    const to = ${JSON.stringify(to)};
    export async function load(url, context, nextLoad) {
      const result = await nextLoad(url, context);
      if (!url.endsWith('/lib/ecmascript.mjs')) return result;
      const source = typeof result.source === 'string' ? result.source : new TextDecoder().decode(result.source);
      const parts = source.split(from);
      if (parts.length !== 2) throw new Error('temporalref: the patched statement does not occur exactly once');
      return { ...result, source: parts.join(to) };
    }`;
  register('data:text/javascript,' + encodeURIComponent(hook), import.meta.url);
}

// The polyfill writes a notice to the console when it loads; it is not an answer.
const quiet = console.log;
const quietWarn = console.warn;
console.log = () => {};
console.warn = () => {};
await import(pathToFileURL(polyfillDir.replace(/\/?$/, '/') + 'lib/shim.mjs').href);
console.log = quiet;
console.warn = quietWarn;

function hex(n, width) {
  return n.toString(16).toUpperCase().padStart(width, '0');
}

function renderNumber(x) {
  if (Number.isNaN(x)) return 'f64:0x7FF8000000000000';
  const view = new DataView(new ArrayBuffer(8));
  view.setFloat64(0, x);
  return 'f64:0x' + hex(view.getUint32(0), 8) + hex(view.getUint32(4), 8);
}

function renderString(s) {
  let out = 'str:"';
  for (let i = 0; i < s.length; i++) {
    const c = s.charCodeAt(i);
    if (c === 0x22) out += '\\"';
    else if (c === 0x5c) out += '\\\\';
    else if (c >= 0x20 && c <= 0x7e) out += String.fromCharCode(c);
    else out += '\\u' + hex(c, 4);
  }
  return out + '"';
}

function render(v) {
  switch (typeof v) {
    case 'number': return renderNumber(v);
    case 'string': return renderString(v);
    case 'bigint': return 'bigint:' + v.toString(10);
    case 'boolean': return 'bool:' + (v ? 'true' : 'false');
    case 'undefined': return 'undefined';
    case 'symbol': return 'symbol';
    default: return v === null ? 'null' : 'object';
  }
}

function outcome(script) {
  let value;
  try {
    value = vm.runInThisContext(script, { filename: 'vector.js' });
  } catch (e) {
    if (types.isNativeError(e)) return 'error: ' + Object.getPrototypeOf(e).constructor.name;
    return 'throw: ' + render(e);
  }
  return 'value: ' + render(value);
}

vm.runInThisContext(readFileSync(preludePath, 'utf8'), { filename: 'prelude.js' });
const globals = Reflect.ownKeys(globalThis).map(String).sort().join('\n');

const requests = JSON.parse(readFileSync(requestsPath, 'utf8'));
const answers = [];
for (const { id, script } of requests) {
  answers.push({ id, outcome: outcome(script) });
  if (Reflect.ownKeys(globalThis).map(String).sort().join('\n') !== globals) {
    process.stderr.write(`temporalref: ${id} changed the global object\n`);
    process.exit(3);
  }
}

const versions = {};
for (const key of ['node', 'v8', 'icu', 'cldr', 'tz', 'unicode']) versions[key] = process.versions[key];
writeFileSync(answersPath, JSON.stringify({ driver: DRIVER, versions, text: flag ?? null, answers }, null, 1) + '\n');
