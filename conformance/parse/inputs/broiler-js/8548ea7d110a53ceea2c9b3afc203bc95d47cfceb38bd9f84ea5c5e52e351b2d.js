function f(n) { var t = n <= 1 ? 1 : f.step; return t; }
f.step = 6;
[f(5), f(5), f(5), f(5)].join('|');