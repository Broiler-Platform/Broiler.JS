(function shadowed(n) {
  var n = 0;
  for (var i = 0; i < n; i++) n += i;
  return n;
})