function f(a) {
  var s = 0;
  for (var i = 0; i < 100; i++) { var t = a[0] * 2 + i; s = t + i; }
  return s;
}
f([2]);