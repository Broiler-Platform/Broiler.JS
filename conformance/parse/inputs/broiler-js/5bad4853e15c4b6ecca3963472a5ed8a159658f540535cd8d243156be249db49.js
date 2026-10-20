function f(a, b) { var s = 0; for (var i = 0; i < 200; i++) { s = s + (a * b + i); s = -s; s++; } return s; }
f(2, 3);