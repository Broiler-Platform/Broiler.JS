gg = "1";
function f() {
  var a = [1, 2, 3];
  var i = gg;
  i = gg;
  var v = (a[i] = 99);
  return a.join(",") + ":" + v;
}
f();