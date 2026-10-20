function f(a) { var s = 0; for (var i = 0; i < 200; i++) { s = s + a * 1.5; s = -s; s++; } return s; }
f(3);