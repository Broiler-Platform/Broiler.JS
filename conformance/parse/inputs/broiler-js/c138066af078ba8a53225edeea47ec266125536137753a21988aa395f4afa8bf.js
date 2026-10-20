gg = 1;
function f() {
  var n = 0;
  var a = [10, 20, 30];
  var i = gg;
  i = gg;
  var o = { get p() { n++; return a; } };
  var v = o.p[i];
  return v + ":" + n;
}
f();