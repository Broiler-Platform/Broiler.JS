function T(a, b, c) { this.a = a; this.b = b; this.c = c; }
var last = null;
for (var i = 0; i < 300; i++) last = new T(i, i + 1, i + 2);
last.a + ',' + last.b + ',' + last.c;