var proto = { k: 5 };
var o = Object.create(proto);
o.k = 1;
o.own = 1;
var s = 0;
for (var i = 0; i < 400; i++) { if (i === 200) delete o.k; s += o.k; }
s;