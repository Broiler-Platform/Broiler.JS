function outer(x) {
  return function inner(n) { return n + x; };
}
function dynamic(n) { with ({}) { return n + 1; } }
var inner = outer(4);
[inner(3), inner(5), dynamic(1)].join('|');