function w(o) { o.x = 1; return o.x; }
var os = [{ x: 0 }, { a: 1, x: 0 }, { b: 1, c: 2, x: 0 }, { d: 1, e: 2, f: 3, x: 0 },
          { g: 1, h: 2, i: 3, j: 4, x: 0 }];
var s = 0;
for (var k = 0; k < 100; k++) s += w(os[k % 5]);
s;