function callee(x) { return x + 1; }
function caller(n) { var t = 0; for (var i = 0; i < n; i++) t = t + callee(i); return t; }
[caller(4), caller(4)].join('|');