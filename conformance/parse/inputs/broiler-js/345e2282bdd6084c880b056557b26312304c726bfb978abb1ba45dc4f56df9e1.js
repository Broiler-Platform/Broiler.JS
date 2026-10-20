function sum(n) { var s = 0; for (var i = 0; i < n; i++) { s += i; } return s; }
for (var k = 0; k < 6; k++) { sum(10); }
sum('10');
var after = [];
for (var k = 0; k < 20; k++) { after.push(sum(10)); }
after.join(',') === new Array(20).fill(45).join(',');