// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// intlref.mjs: the Intl area's reference computation, `ref:intlref@1` (corpus revision 27, milestone SJL-9.2, work
// package SJL-9.2a). It is ICU, through the reference Node.js release bundle sjl-9-001's reference.txt names: the
// newest release whose process.versions.cldr begins "48", v26.12.0 (ICU 78.3, CLDR 48.0), retrieved and checked by
// its tarball's SHA-256 (decision D17 of Broiler.VM's JSD-0082). It is neither engine's code.
// generate_intl_vectors.py writes the requests, runs this driver, and reads the answers; it is the only caller.
//
//   <node> intlref.mjs <prelude.js> <requests.json> <answers.json> [--witness]
//
// The driver refuses to run under any Node.js but the reference (process.versions' node, icu, cldr and tz), or,
// with --witness, under any but Node.js 22.22.0 (ICU 77.1, CLDR 47.0), the witness below. For each request it makes
// a fresh realm (vm.createContext), evaluates the corpus's prelude.js there as a global script, then the request's
// script as a separate global script, sloppy, as an engine-level runner does (SCHEMA.md section 7), and renders its
// outcome by the corpus's rendering rule:
//
//   value: <typed value>   a normal completion: a Number by its bit pattern, a String by its code units,
//                          a BigInt, a Boolean, undefined, null, `symbol` or `object`;
//   error: <Name>          a thrown object with an [[ErrorData]] slot, by its constructor's name;
//   throw: <typed value>   any other thrown value.
//
// THE WITNESS (--witness). V8 writes U+0020 where ICU's date-time pattern has U+202F (NARROW NO-BREAK SPACE), its
// own substitution for compatibility. Decision D17 (b) reads that Node 22's `format` does so and its
// `formatToParts` keeps U+202F; the reference, v26.12.0, substitutes in `formatToParts` and the range methods as
// well (found at SJL-9.2a), so no output of the reference shows where the pattern has U+202F. Node 22.22.0's parts
// still do. With --witness, the driver replaces, in each fresh realm and before the prelude, the date-time
// formatter's string-answering members by the join of the matching parts method, which Node 22 does not
// substitute: Intl.DateTimeFormat.prototype.format (the bound function its getter answers) and formatRange, and
// Date.prototype.toLocaleString, toLocaleDateString and toLocaleTimeString (through a DateTimeFormat made with the
// same required and default components, as ECMA-402's Date.prototype clauses make it). The generator takes from
// the witness only the positions of U+202F, and only where its answer equals the reference's character for
// character once U+202F is read as U+0020; it takes no other character from it (generate_intl_vectors.py, D17
// (b)). The witness has no Temporal, so no request about a Temporal object is witnessed.
//
// The answers file is JSON: { driver, versions, witness, answers: [{ id, outcome }] }.

import { readFileSync, writeFileSync } from 'node:fs';
import { types } from 'node:util';
import vm from 'node:vm';

const DRIVER = 'intlref 1';
const REFERENCE = { node: '26.12.0', icu: '78.3', cldr: '48.0', tz: '2026e' };
const WITNESS = { node: '22.22.0', icu: '77.1', cldr: '47.0' };

// The witness's replacements, evaluated in each fresh realm before the prelude. They define no global.
const WITNESS_PATCH = `(function () {
  var DTF = Intl.DateTimeFormat;
  var proto = DTF.prototype;
  var parts = proto.formatToParts;
  var rangeParts = proto.formatRangeToParts;
  function join(list) { var s = ''; for (var i = 0; i < list.length; i++) s += list[i].value; return s; }
  Object.defineProperty(proto, 'format', { configurable: true, get: function () {
    var dtf = this;
    return function (date) { return join(parts.call(dtf, date)); };
  } });
  Object.defineProperty(proto, 'formatRange', { configurable: true, writable: true, value: function (a, b) {
    return join(rangeParts.call(this, a, b));
  } });
  var DATE = ['weekday', 'year', 'month', 'day'];
  var TIME = ['dayPeriod', 'hour', 'minute', 'second', 'fractionalSecondDigits'];
  function options(given, required, defaults) {
    var o = given === undefined ? {} : Object(given);
    var copy = {};
    for (var k in o) copy[k] = o[k];
    var need = true;
    var check = required === 'date' ? ['weekday', 'year', 'month', 'day']
      : required === 'time' ? ['dayPeriod', 'hour', 'minute', 'second', 'fractionalSecondDigits']
      : DATE.concat(TIME);
    for (var i = 0; i < check.length; i++) if (copy[check[i]] !== undefined) need = false;
    if (copy.dateStyle !== undefined || copy.timeStyle !== undefined) need = false;
    if (required === 'date' && copy.timeStyle !== undefined) throw new TypeError('timeStyle');
    if (required === 'time' && copy.dateStyle !== undefined) throw new TypeError('dateStyle');
    if (need && (defaults === 'date' || defaults === 'all')) { copy.year = 'numeric'; copy.month = 'numeric'; copy.day = 'numeric'; }
    if (need && (defaults === 'time' || defaults === 'all')) { copy.hour = 'numeric'; copy.minute = 'numeric'; copy.second = 'numeric'; }
    return copy;
  }
  function method(required, defaults) {
    return function (locales, given) {
      var t = Date.prototype.valueOf.call(this);
      if (t !== t) return 'Invalid Date';
      return join(parts.call(new DTF(locales, options(given, required, defaults)), t));
    };
  }
  Object.defineProperty(Date.prototype, 'toLocaleString', { configurable: true, writable: true, value: method('any', 'all') });
  Object.defineProperty(Date.prototype, 'toLocaleDateString', { configurable: true, writable: true, value: method('date', 'date') });
  Object.defineProperty(Date.prototype, 'toLocaleTimeString', { configurable: true, writable: true, value: method('time', 'time') });
})();`;

const [preludePath, requestsPath, answersPath, flag] = process.argv.slice(2);
if (!answersPath || (flag !== undefined && flag !== '--witness')) {
  process.stderr.write('usage: intlref.mjs <prelude.js> <requests.json> <answers.json> [--witness]\n');
  process.exit(2);
}
const witness = flag === '--witness';
const expected = witness ? WITNESS : REFERENCE;
for (const [key, value] of Object.entries(expected)) {
  if (process.versions[key] !== value) {
    process.stderr.write(`intlref: process.versions.${key} is ${process.versions[key]}, not ${value}; ` +
      `this is not the ${witness ? 'witness' : 'reference'} Node.js\n`);
    process.exit(3);
  }
}

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

const prelude = readFileSync(preludePath, 'utf8');

function outcome(script) {
  const context = vm.createContext({});
  if (witness) vm.runInContext(WITNESS_PATCH, context, { filename: 'witness.js' });
  vm.runInContext(prelude, context, { filename: 'prelude.js' });
  let value;
  try {
    value = vm.runInContext(script, context, { filename: 'vector.js', timeout: 10000 });
  } catch (e) {
    if (types.isNativeError(e)) return 'error: ' + Object.getPrototypeOf(e).constructor.name;
    return 'throw: ' + render(e);
  }
  return 'value: ' + render(value);
}

const requests = JSON.parse(readFileSync(requestsPath, 'utf8'));
const answers = requests.map(({ id, script }) => ({ id, outcome: outcome(script) }));
const versions = {};
for (const key of ['node', 'v8', 'icu', 'cldr', 'tz', 'unicode']) versions[key] = process.versions[key];
writeFileSync(answersPath, JSON.stringify({ driver: DRIVER, versions, witness, answers }, null, 1) + '\n');
