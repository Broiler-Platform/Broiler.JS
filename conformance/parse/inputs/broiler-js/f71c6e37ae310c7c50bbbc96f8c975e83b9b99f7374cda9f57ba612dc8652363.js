var v2, vNull, vHole, vUndefined, vOob;
var result;
var vals = [2, null, , undefined];
result = [v2 = 10, vNull = 11, vHole = 12, vUndefined = 13, vOob = 14] = vals;
[v2, vNull, vHole, vUndefined, vOob, result === vals].join('|');