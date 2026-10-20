function outer() {
  var q = 1;
  var bump = function () { q = q + 1; };
  bump(); bump();
  return q;
}
outer();