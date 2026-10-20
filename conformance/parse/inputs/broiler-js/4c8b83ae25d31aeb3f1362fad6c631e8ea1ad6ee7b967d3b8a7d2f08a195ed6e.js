function o() {
  var rowSize = 10;
  var mid = function () {
    var inner = function () { var c = 2 * rowSize; c++; return c; };
    return inner();
  };
  return mid();
}
o();