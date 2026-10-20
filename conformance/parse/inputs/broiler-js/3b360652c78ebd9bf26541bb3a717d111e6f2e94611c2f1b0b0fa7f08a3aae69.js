function C() { this.own = 1; }
var a = { k: 1 };
var b = { k: 3 };
var o = Object.create(a);
o.own = 1;
var s = 0;
for (var i = 0; i < 400; i++) { if (i === 200) Object.setPrototypeOf(o, b); s += o.k; new C(); }
s;