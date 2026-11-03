// SJL-14.1, part J seed: direct eval in each context decision D44 lists (strict and sloppy callers, functions,
// methods, derived constructors, field initialisers, static blocks), and Function with parameters and a body.
var r = [];
r.push((function () { return eval('typeof new.target'); })());
r.push((function () { 'use strict'; return eval('var x = 1; x'); })());
r.push(({ m() { return eval('typeof super.toString'); } }).m());
class B { constructor() { this.b = 1; } }
class D extends B { f = eval('this.b === undefined'); constructor() { eval('super()'); } }
r.push(new D().f);
class S { static #p = 5; static { r.push(eval('#p in S')); } }
r.push(Function('a', 'b', 'return a + b')(1, 2));
r.join(',');
