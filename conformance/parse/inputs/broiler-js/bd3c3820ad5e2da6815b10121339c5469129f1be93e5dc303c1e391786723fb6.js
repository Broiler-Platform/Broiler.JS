function f(x) { return x + 1; }
var s = 0;
for (var i = 0; i < 10; i++) s = s + f(i);
s;