var holder = { step: 2 };
function tally(n) { var t = 0; for (var i = 0; i < n; i++) t += holder.step; return t; }
[tally(3), tally(3), tally(3), tally(3)].join('|');