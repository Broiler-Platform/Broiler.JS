function outer() {
  var inner = function (s) { return eval(s); };
  return inner('1');
}
function beside() { return 1; }
outer(); beside();