function f(n) { var o; o = { v: f.step }; return o.v; }
f.step = 6;
[f(5), f(5), f(5), f(5)].join('|');