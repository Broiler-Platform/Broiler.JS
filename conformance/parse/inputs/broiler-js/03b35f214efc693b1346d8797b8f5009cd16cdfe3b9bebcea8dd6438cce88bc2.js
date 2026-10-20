gg = 1;
function f() { return 1; }
function hot() {
  var a = [1];
  var o = { x: 1 };
  var s = f();
  a[0]++;
  o.x++;
  s++;
  gg++;
  return s;
}
hot();