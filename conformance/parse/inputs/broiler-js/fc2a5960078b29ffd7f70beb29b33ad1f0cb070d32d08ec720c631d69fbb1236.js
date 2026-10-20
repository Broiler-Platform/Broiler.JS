function sum(n) { var s = 0; for (var i = 0; i < n; i++) { s += i; } return s; }
var kinds = [];
for (var k = 0; k < 8; k++) { kinds.push(typeof sum(10)); }
kinds.join(',') + '|' + sum(10);