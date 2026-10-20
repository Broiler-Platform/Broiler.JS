var a = { k: 1 };
var b = { k: 3 };
var x = Object.create(a); x.v = 0;
var y = Object.create(b); y.v = 0;
var s = 0;
for (var i = 0; i < 300; i++) s += x.k + y.k;
s;