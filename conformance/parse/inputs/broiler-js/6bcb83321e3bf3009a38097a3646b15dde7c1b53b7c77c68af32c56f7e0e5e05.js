var base = 5;
function d(n, m) { var t = n + (m === undefined ? base : m); return t; }
[d(1), d(1), d(1), d(1)].join('|');