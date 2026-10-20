var tally = function (n) { var t = 0; for (var i = 0; i < n; i++) t += 2; return t; };
[tally(3), tally(3), tally(3), tally(3)].join('|');