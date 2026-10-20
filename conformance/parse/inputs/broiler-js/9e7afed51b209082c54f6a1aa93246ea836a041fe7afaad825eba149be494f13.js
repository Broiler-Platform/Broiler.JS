// A computed key, a private field and a super member keep the ordinary reference;
// only their correctness is asserted here.
var k = 'x';
var computed = { x: 0 };
for (var i = 0; i < 300; i++) computed[k] += 1;

// As with `super.v++`: the read comes from the home object's prototype and the
// write goes to the RECEIVER, so it reads 7 and rewrites an own 8 forever.
class P {}
P.prototype.v = 7;
class C extends P { addSuper() { return super.v += 1; } }
var c = new C();
var superLast = 0;
for (var i = 0; i < 300; i++) superLast = c.addSuper();

class Priv { #n = 0; add() { return this.#n += 1; } get n() { return this.#n; } }
var pv = new Priv();
for (var i = 0; i < 300; i++) pv.add();

var arr = []; arr.tag = 0;
for (var i = 0; i < 300; i++) arr.tag += 1;

[computed.x, superLast + ',' + c.v + ',' + P.prototype.v, pv.n, arr.tag].join('|');