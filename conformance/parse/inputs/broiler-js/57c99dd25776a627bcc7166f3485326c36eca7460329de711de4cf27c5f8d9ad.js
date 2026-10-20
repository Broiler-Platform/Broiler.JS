var traps = 0;
function add(o) { o.x = 1; }
for (var i = 0; i < 400; i++) add({});
var target = {};
var p = new Proxy(target, { set: function (t, k, v, r) { traps++; return Reflect.set(t, k, v); } });
for (var i = 0; i < 400; i++) add(p);
traps + '|' + target.x;