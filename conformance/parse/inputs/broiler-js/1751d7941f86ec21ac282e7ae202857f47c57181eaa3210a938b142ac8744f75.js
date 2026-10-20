var tally = function named(n) { var t = 0; for (var i = 0; i < n; i++) t += named.step; return t; };
tally.step = 2;
[tally(3), tally(3), tally(3), tally(3)].join('|');