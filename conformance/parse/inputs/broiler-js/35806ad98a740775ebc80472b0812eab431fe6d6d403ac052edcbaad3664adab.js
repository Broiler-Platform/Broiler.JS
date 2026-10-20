function f(o, b) { var s = 0; for (var i = 0; i < 100; i++) { var t = o.x * b + i; s = t; } return s; }
f({ x: 2 }, 3);