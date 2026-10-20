var log = 0;
var target = {};
var p = new Proxy(target, { set: function (t, k, v) { log++; t[k] = v; return true; } });
for (var i = 0; i <= 9; i++) p.x = i;
log + ',' + target.x;