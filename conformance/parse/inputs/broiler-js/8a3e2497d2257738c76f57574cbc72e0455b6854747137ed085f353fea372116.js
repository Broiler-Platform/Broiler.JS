gg = 1;
function f() {
  var c = gg;
  c++; c--;
  var p = { get v() { c = "x"; return 10; } };
  return c + p.v;
}
f();