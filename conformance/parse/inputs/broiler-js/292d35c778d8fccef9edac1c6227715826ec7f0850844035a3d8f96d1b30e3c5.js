function probe(v) {
  var i = 1;
  return [i < v, i > v, v < i, v > i].join(',');
}
probe(false);