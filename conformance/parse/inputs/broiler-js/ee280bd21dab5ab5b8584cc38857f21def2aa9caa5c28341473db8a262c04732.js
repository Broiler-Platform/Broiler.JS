var traps = 0;
var p = new Proxy({}, { set: function (t, k, v, r) { traps++; return true; } });
function add(o) { o.x = 1; return o.hasOwnProperty('x'); }
var own = false;
for (var i = 0; i < 400; i++) { if (add(Object.create(p))) own = true; }
traps + '|' + own;