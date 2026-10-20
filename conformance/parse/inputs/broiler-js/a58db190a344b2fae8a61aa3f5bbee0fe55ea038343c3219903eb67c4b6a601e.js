(function () {
  var counter = 1;
  (function () {
    (function () { counter = counter + 41; })();
  })();
  return counter;
})();