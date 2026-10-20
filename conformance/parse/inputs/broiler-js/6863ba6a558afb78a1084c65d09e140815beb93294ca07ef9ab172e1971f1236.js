function total(p) { var t = 0; for (var i = 0; i < 4; i++) t += p.x; return t; }
var a = { x: 1, y: 2 };
[total(a), total(a), total(a), total(a)].join('|');