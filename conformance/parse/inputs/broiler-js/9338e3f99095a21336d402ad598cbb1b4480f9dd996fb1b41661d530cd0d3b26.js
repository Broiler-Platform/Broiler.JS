function sum(n) { var s = 0; for (var i = 0; i < n; i++) { s += i; } return s; }
var calls = 0;
var counting = { valueOf: function () { calls++; return 10; } };
var answer = sum(counting);
answer + '|' + calls;