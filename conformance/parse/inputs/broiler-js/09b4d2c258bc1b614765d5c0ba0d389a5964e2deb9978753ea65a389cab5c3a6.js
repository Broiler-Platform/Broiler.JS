// A computed key, a private field and a super member all keep the ordinary
// reference; only their correctness is asserted here.
var k = 'x';
var computed = { x: 0 };
for (var i = 0; i < 300; i++) computed[k]++;

// `super.v++` reads from the home object's prototype and writes to the RECEIVER,
// so every iteration reads 7 and rewrites an own 8 - the prototype never moves and
// the read never sees what the write created.
class P {}
P.prototype.v = 7;
class C extends P { bumpSuper() { return super.v++; } }
var c = new C();
var superLast = 0;
for (var i = 0; i < 300; i++) superLast = c.bumpSuper();

class Priv { #n = 0; bump() { return ++this.#n; } get n() { return this.#n; } }
var p = new Priv();
for (var i = 0; i < 300; i++) p.bump();

var arr = []; arr.tag = 0;
for (var i = 0; i < 300; i++) arr.tag++;

[computed.x, superLast + ',' + c.v + ',' + P.prototype.v, p.n, arr.tag].join('|');