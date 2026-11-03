// SJL-14.1, part J seed: Annex B.3.3 and B.3.4 block-level functions (the bridge synthesises the Annex B.3.4 block
// and AnnexBFunctionNames from the library's side tables; decision D44).
var log = [];
if (true) function f() { return 1; }
if (false) ; else function g() { return 2; }
{ function h() { return 3; } }
switch (1) { case 1: function k() { return 4; } }
try { throw 0; } catch (e) { function m() { return 5; } }
log.push(typeof f, typeof g, typeof h, typeof k, typeof m);
(function () { { function inner() { return 6; } } log.push(typeof inner); })();
log.join(',');
