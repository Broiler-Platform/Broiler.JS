function outer() {
  var q = 1;
  var inner = function (q) { return q; };
  return inner(2);
}
outer();