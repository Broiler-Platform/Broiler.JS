gg = 0;
function f() {
  var a = [10, 20, 30, 40];
  var i = gg;
  var s = a[i]; i++;
  s += a[i]; i++;
  s += a[i];
  return s;
}
f();