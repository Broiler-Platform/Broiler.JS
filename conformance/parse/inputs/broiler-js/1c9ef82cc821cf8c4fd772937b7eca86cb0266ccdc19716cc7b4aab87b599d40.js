function outer() {
  var q = 1;
  var inner = function () { var r = q; var q = 2; return r; };
  return inner();
}
outer();