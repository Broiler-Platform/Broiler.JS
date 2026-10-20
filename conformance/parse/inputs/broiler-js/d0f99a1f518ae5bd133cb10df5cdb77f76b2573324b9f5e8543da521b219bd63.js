var base = { k: 7 };
var mid = Object.create(base);
mid.k = undefined;
var o = Object.create(mid);
o.own = 1;
var r;
for (var i = 0; i < 400; i++) r = o.k;
String(r);