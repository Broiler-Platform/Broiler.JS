function add(o) { o.x = 1; return o.x; }
var ok = 0, dict = 0;
for (var i = 0; i < 400; i++) {
    ok += add({});
    var d = {};
    Object.defineProperty(d, 'k', { get: function () { return 1; } });
    dict += add(d);
}
ok + '|' + dict;