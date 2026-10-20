var t = {}; t.q = 1; t.r = 2;
var p = new Proxy(t, { get: function (tt, k) { return k in tt ? tt[k] * 10 : undefined; } });
p.q + ',' + p.r + ',' + Object.keys(t).join(',');