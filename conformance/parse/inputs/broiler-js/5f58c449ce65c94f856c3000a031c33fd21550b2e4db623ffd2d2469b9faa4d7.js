gg = 0;
function f() {
  var a = [1, 2, 3];
  var i = gg;
  a[i] = 11; i++;
  a[i] = 22; i++;
  a[i] = 33;
  return a.join(",");
}
f();