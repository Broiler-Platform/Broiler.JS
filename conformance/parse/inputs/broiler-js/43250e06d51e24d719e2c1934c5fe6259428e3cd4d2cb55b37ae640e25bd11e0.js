function withParameter(n) { var s = 0; for (var i = 0; i < n; i++) s = s + i; return s; }
function withLiteral() { var s = 0; for (var i = 0; i < 100; i++) s = s + i; return s; }
[withParameter(100), withLiteral()].join('|');