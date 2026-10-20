function* count() { yield 1; yield 2; yield 3; }
var total = 0;
for (var v of count()) total += v;
total;