function* gen(n) { var s = 0; for (var i = 0; i < n; i++) { s += i; yield s; } return s; }
var out = [];
for (var k = 0; k < 8; k++) { var it = gen(4); out.push(it.next().value + ':' + it.next().value); }
out[0] + '|' + out[7];