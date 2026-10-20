function sum(n) { var s = 0; for (var i = 0; i < n; i++) { s += i; } return s; }
function deep(d) { return d === 0 ? sum('10') : deep(d - 1); }
for (var k = 0; k < 6; k++) { sum(10); }
var first = deep(200);
var second = deep(200);
first + '|' + second + '|' + sum(10);