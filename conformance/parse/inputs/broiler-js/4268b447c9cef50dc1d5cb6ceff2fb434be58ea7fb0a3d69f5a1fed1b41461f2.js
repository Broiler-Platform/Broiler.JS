var calls = 0;
var box = { valueOf: function () { calls++; return 2; } };
function probe(v) {
  var i = 1;
  return [i < v, i > v, v < i, v > i].join(',');
}
probe(box) + '|' + calls;