(function () {
  var x = 4;
  (function () { (function () { eval('x = x * 3'); })(); })();
  return x;
})();