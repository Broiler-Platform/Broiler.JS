function f(a, b) {
  var s = 0;
  for (var i = 0; i < 100; i++) { var t = a[0] * b + i; s = t * b + i; }
  return s;
}
f([2], 3);