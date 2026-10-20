var calls = 0;
function base() { calls++; return a; }
var a = [1];
var i = 0;
base()[i] += 2;
a[0] + '|' + calls;