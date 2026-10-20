function tally(n) { var tally = 2, t = 0; for (var i = 0; i < n; i++) t += tally; return t; }
[tally(3), tally(3), tally(3), tally(3)].join('|');