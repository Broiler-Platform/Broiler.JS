function f(a, b) { return a + b; }
var b = f.bind(null, 1);
[b.name, b.length, b(2)].join('|');