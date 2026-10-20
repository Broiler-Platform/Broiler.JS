var proto = { k: 5 };
var o = Object.create(proto);
o.own = 1;
var s = 0;
for (var i = 0; i < 500; i++) s += o.k;
s;