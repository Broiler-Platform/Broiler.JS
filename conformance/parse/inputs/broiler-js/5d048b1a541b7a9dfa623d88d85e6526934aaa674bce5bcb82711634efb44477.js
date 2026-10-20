function makeCounter(start) { return function () { return start++; }; }
var counters = [];
for (var i = 0; i < 100; i++) counters.push(makeCounter(i));
var sum = 0;
for (var i = 0; i < 100; i++) sum += counters[i]();
sum;