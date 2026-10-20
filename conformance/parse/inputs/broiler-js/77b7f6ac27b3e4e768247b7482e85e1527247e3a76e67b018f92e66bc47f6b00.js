function outer() {
  function fact(n) { return n < 2 ? 1 : n * fact(n - 1); }
  var g = function h(n) { return n < 1 ? 0 : h(n - 1); };
  return fact(4) + g(3);
}
outer();