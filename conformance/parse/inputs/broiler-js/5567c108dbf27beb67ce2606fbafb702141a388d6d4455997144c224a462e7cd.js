class C { constructor(v) { this.v = v; } getV() { return this.v; } }
var c = new C(3);
var s = 0;
for (var i = 0; i < 500; i++) s += c.getV();
s;