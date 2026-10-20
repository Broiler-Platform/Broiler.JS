function t() {
  var x = 0;
  var innerX = (function () { x = (eval('var x = 2;'), 1); return x; })();
  return innerX + '|' + x;
}
t();