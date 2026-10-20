var limit = { n: 0, valueOf: function () { return this.n; } };
function collect(bound) {
  var out = [];
  for (var i = 0; i < bound; i++) { out.push(i); if (out.length > 20) break; }
  return out.join(',');
}
limit.n = 4;
collect(limit);