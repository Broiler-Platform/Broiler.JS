// SJL-15.1 (Broiler.JS's half): the correspondence probes' shared prelude, bundle sjl-14-001's inherited-context
// prelude (docs/evidence/sjl-14-001/inherited-context/prelude.js) unchanged but for these first lines.
// tt compiles one probe by an indirect eval, so that a probe one engine refuses stops only its own line; a probe sets
// __reached as its first statement, so "THROWS (template)" says the probe was refused before any of it ran.
// Broiler.JS's shell has print; Broiler.VM's test262 realm has none, so there the lines are collected and handed
// back in one uncaught Error the conformance root prints (epilogue.js).
var __lines = [];
var __reached = false;
var __v;
var __host = typeof __vmTest === 'undefined' && typeof print === 'function';
function out(s) { s = String(s); if (__host) print(s); else __lines.push(s); }
// A string is printed quoted, every code unit outside printable ASCII (and " and \) as \uXXXX.
function q(s) {
  var r = '';
  for (var i = 0; i < s.length; i++) {
    var c = s.charCodeAt(i);
    if (c < 0x20 || c > 0x7e || c === 0x22 || c === 0x5c) r += '\\u' + ('0000' + c.toString(16)).slice(-4);
    else r += s.charAt(i);
  }
  return '"' + r + '"';
}
function plain(s) {
  var r = '';
  for (var i = 0; i < s.length; i++) {
    var c = s.charCodeAt(i);
    r += c < 0x20 || c > 0x7e ? '\\u' + ('0000' + c.toString(16)).slice(-4) : s.charAt(i);
  }
  return r;
}
function show(v) {
  if (v === null) return 'null';
  if (v === undefined) return 'undefined';
  if (typeof v === 'string') return q(v);
  if (typeof v === 'number') return Object.is(v, -0) ? '-0' : String(v);
  if (typeof v === 'boolean') return String(v);
  return typeof v;
}
function errName(e) { return (e && e.constructor && e.constructor.name) || typeof e; }
function t(label, fn) {
  var r;
  try { r = show(fn()); }
  catch (e) { r = 'THROWS ' + errName(e) + ': ' + plain(String(e && e.message)); }
  out(label + ' => ' + r);
}
// tt: compiles the probe's program by an indirect eval (global code, sloppy unless the program says otherwise), so
// that a program one engine refuses stops only its own line. The program sets __reached just before its direct eval.
function tt(label, src) {
  var r;
  __reached = false;
  try { r = show((0, eval)(src)); }
  catch (e) { r = (__reached ? 'THROWS ' : 'THROWS (template) ') + errName(e) + ': ' + plain(String(e && e.message)); }
  out(label + ' => ' + r);
}
