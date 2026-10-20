function sink(x) { return x; }
function f(a, o, b) {
  var s = 0;
  for (var i = 0; i < 100; i++) {
    var t = a[0] * b + i; var u = o.x * b + i; var v = sink(b) * b + i; var w = t * u + v;
    s = w;
  }
  return s;
}
f([2], { x: 3 }, 4);