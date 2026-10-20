function outer() {
  var q = 1;
  var inner = function (s) { return eval(s); };
  return inner('q');
}
outer();