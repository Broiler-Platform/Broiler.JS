var seen = null;
var p = new Proxy({}, {
    set: function (t, k, v) { seen = k; t[k] = v; return true; }
});
var i = 4;
p[i] = 9;
p[4] + '|' + seen;