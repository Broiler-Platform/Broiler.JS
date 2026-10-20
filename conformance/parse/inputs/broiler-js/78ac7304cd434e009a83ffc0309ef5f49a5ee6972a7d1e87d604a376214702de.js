(function () {
  var a = 1;
  return (function () {
    var b = 2;
    return (function () {
      var c = 4;
      return (function () {
        var d = 8;
        return (function () {
          a += 16; b += 16; c += 16; d += 16;
          return a + b + c + d;
        })();
      })();
    })();
  })() + ':' + a;
})();