function w(o, v) { o.x = v; return o.x; }
var os = [{ x: 0 }, { a: 1, x: 0 }, { b: 1, c: 2, x: 0 }, { d: 1, e: 2, f: 3, x: 0 }];
var s = 0;
for (var i = 0; i < 400; i++) s += w(os[i % 4], 1);
s;