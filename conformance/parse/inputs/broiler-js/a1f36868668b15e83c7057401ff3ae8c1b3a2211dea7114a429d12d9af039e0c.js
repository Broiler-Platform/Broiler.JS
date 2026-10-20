gg = 1;
function f() {
  var c = gg;
  c++; c--;
  var p = { get v() { c = 100; return 10; } };
  return c + p.v;
}
f();