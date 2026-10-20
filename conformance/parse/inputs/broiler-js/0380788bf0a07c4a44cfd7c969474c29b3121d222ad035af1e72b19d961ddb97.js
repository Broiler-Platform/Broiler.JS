function o(size) {
  var rowSize = size.width;
  var f = function () { var c = 2 * rowSize; c++; return c; };
  return f();
}
o({ width: 10 });