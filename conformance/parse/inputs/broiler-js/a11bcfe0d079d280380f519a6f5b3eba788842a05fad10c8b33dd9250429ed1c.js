function o() {
  var rowSize = 10;
  var f = function () { var c = 2 * rowSize; c++; return c; };
  return f();
}
o();