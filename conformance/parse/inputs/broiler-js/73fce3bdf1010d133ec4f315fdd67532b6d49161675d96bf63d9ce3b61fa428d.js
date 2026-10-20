function f(n) { var t = 0; switch (n) { case 5: t = f.step; break; }
return t; }
f.step = 6;
[f(5), f(5), f(5), f(5)].join('|');