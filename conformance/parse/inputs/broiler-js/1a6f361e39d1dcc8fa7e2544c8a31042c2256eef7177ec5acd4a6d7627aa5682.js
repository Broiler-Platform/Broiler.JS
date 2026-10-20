gg = 1; hh = "2";
function f() {
  var a = [10, 20, 30];
  var i = gg;
  i = gg;
  var o = { get p() { i = hh; return a; } };
  return o.p[i];
}
f();