function outer() {
  function fact(n) { return n < 2 ? 1 : n * fact(n - 1); }
  return fact(5);
}
outer();