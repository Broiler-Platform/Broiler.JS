function outer() {
  var q = 1;
  var inner = function () { return q; };
  var tierable = function (n) { var s = 0; for (var i = 0; i < n; i++) { s = s + i; } return s; };
  return inner() + tierable(10);
}
String(outer());