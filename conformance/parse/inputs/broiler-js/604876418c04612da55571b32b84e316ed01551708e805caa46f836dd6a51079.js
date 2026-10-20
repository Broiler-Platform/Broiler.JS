function o() {
  var rowSize = 10;
  var f = function (rowSize) { var c = 2 * rowSize; c++; return c; };
  return f("wide");
}
o();