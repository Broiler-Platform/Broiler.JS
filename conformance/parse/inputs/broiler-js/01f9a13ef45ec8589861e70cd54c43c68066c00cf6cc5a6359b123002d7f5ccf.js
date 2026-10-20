function outer() {
  var q = 1;
  var middle = function () {
    var deep = function () { return q; };
    return deep();
  };
  return middle();
}
outer();