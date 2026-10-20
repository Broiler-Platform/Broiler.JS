function f(a, b, c) { for (var i = 0; i < 100; i++) { a[0] += b * c + i; } return a[0]; }
f([0], 2, 3);