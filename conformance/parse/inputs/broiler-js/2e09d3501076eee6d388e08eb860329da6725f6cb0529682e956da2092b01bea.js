function invalid(n) {
  const total = 0;
  for (var i = 0; i < n; i++) total += i;
  return total;
}
function shadowed(n) {
  var n = 0;
  for (var i = 0; i < n; i++) n += i;
  return n;
}
var errorName;
try { invalid(3); errorName = 'missing error'; }
catch (error) { errorName = error.name; }
[errorName, shadowed(3)].join('|');