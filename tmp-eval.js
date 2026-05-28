var originalEval = eval;
function run() {
  eval("var eval = function (value) { return value + 1; }");
  return [eval(1), eval(2), eval === originalEval].join("|");
}
console.log([run(), eval === originalEval, typeof eval, typeof originalEval].join('||'));
