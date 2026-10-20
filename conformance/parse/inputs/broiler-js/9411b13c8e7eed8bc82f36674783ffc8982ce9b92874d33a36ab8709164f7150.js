function o() {
  var rowSize = 10;
  var f = function () {
    var r = rowSize;
    var c = 2 * r;
    c++;
    return c + r;
  };
  return f();
}
o();