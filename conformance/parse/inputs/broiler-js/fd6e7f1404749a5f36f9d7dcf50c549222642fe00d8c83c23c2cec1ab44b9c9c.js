gg = 0;
function f() {
  var log = [];
  var a = [1, 2, 3];
  var i = gg;
  i = gg;
  var o = { get p() { log.push("base"); return a; } };
  o.p[i] = (log.push("rhs"), 7);
  return log.join(",") + ":" + a[0];
}
f();