var seen = null;
var p = new Proxy({ 7: 'proxied' }, {
    get: function (t, k) { seen = k; return k in t ? t[k] + '!' : undefined; }
});
var i = 7;
p[i] + '|' + seen;