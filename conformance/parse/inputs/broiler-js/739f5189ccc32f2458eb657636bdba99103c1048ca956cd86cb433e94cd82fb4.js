function sink(x) { return x; }
function g(b, c) { return b * c + 1; }
function f(a, o, b, c) {
  var s = 0;
  for (var i = 0; i < 100; i++) {
    a[0] = b * c + i; o.x = b + c * i; s = b * i + c;
    s = sink(b * c + i) + g(b, c);
  }
  return s;
}
f([0], { x: 0 }, 2, 3);