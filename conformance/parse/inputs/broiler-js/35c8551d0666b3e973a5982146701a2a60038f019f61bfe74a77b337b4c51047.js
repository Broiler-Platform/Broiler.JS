function sum(n) { var s = 0; for (var i = 0; i < n; i++) { s += i; } return s; }
for (var k = 0; k < 6; k++) { sum(10); }
var caught = 'none';
try { sum({ valueOf: function () { throw new Error('boom'); } }); }
catch (e) { caught = e.message; }
caught + '|' + sum(10);