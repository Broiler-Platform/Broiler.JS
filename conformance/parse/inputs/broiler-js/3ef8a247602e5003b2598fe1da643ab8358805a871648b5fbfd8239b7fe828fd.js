function f(o, b, c) { for (var i = 0; i < 100; i++) { o.x = b * c + i; } return o.x; }
f({ x: 0 }, 2, 3);