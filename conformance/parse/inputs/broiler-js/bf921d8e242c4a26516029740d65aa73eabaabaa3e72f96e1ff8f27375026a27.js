(function () {
  var base = 5;
  var seen = 0;
  (async function () { seen = base + 1; })();
  return seen;
})();