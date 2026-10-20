(function () {
  var base = 10;
  function* g() { yield base + 1; yield base + 2; }
  var out = [];
  for (const v of g()) out.push(v);
  return out.join(',');
})();