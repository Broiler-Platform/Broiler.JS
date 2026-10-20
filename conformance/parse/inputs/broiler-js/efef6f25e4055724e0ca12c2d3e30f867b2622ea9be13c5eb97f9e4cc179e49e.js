function sum(n) {
  var total = 0;
  for (var i = 0; i < n; i++) total += i;
  return total;
}
[sum(10), sum(10), sum(10), sum(3.5), Object.is(sum(-0), 0), sum('10')].join('|');