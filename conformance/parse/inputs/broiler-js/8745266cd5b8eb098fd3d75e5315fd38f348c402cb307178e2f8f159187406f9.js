// JSClass derives from JSFunction, so it inherits the same cached field and the same
// exclusion. A class's `prototype` is non-writable and non-configurable, so every one
// of these writes must be refused — including the ones after the site is warm, which
// is the case a cached store could have got wrong.
//
// Scoped deliberately to the observable PROPERTY. What `new C()` produces after a
// refused write is wrong on this engine at the pinned pointer too — the indexer syncs
// JSFunction's cached prototype field before the write, so a refusal still redirects
// [[Construct]] — and asserting it here would either encode that defect as correct or
// fail for a reason this item does not own. It is reported separately in
// docs/performance-roadmap.md item 2-8.
class C {}
var original = C.prototype;
var kept = 0;
for (var i = 0; i < 300; i++) {
    try { C.prototype = { n: i }; } catch (e) { /* sloppy: silently refused */ }
    if (C.prototype === original) kept++;
}
var d = Object.getOwnPropertyDescriptor(C, 'prototype');
kept + '|' + (d.writable === false) + '|' + (d.configurable === false);