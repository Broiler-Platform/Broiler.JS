function duplicate(v) {
  switch (v) {
    case 1: return 'first';
    case 1: return 'second';
    case 2: return 'two';
    case 3: return 'three';
  }
}
function fall(v) {
  var r = '';
  switch (v) {
    case 1: r += 'a';
    default: r += 'd';
    case 2: r += 'b'; break;
    case 3: r += 'c'; break;
    case 4: r += 'e';
  }
  return r;
}
function sparse(v) {
  switch (v) { case 1: return 1; case 1000: return 2; case 2000: return 3; default: return 0; }
}
[duplicate(1), fall(1), fall(9), sparse(1000), sparse('1000')].join('|');