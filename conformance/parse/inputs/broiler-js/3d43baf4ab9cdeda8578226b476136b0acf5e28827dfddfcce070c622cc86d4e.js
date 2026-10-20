function inner() { return inner.caller === outer; }
function outer() { return inner(); }
var first = outer();
var second = outer();
first + '|' + second;