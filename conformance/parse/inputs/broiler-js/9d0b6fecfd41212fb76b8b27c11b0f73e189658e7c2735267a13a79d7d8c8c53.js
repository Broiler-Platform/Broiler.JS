function sink(x) { return x; }
function f(a, b, c, d) { for (var i = 0; i < 100; i++) { a[0] = b * c + sink(d * 2 + i); } return a[0]; }
f([0], 2, 3, 4);