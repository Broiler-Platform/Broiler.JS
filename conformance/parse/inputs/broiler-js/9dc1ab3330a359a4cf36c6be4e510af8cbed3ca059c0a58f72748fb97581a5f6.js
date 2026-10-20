function sink(x) { return x; }
function f(b, c) { var s = 0; for (var i = 0; i < 100; i++) { s = sink(b * c + i); } return s; }
f(2, 3);