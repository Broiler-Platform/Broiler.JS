(function () {
  var outer = 7;
  return (function () {
    return (function () {
      return (function () { return outer; })();
    })();
  })();
})();