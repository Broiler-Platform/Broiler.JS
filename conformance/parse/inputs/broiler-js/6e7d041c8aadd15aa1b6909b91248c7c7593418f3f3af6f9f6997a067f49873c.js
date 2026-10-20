function g(b, c, i) { return b * c + i; }
function f(b, c) { var s = 0; for (var i = 0; i < 100; i++) { s = g(b, c, i); } return s; }
f(2, 3);