function outer() {
  var q = 1;
  return (function () {
    return (function () {
      return (function () { return q; })();
    })();
  })();
}
outer();