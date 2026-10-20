function probe(v) {
  var i = 0/0;
  return [i < v, i > v, v < i, v > i].join(',');
}
probe(1);