var proto = { k: 1 };
var o = Object.create(proto);
o.own = 1;
var s = 0;
for (var i = 0; i < 400; i++) { if (i === 200) o.k = 3; s += o.k; }
s;