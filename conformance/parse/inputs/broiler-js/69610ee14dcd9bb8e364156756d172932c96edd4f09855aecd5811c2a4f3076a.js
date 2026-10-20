(function () {
  var fact = function f(n) { return n <= 1 ? 1 : n * f(n - 1); };
  return fact(5);
})();