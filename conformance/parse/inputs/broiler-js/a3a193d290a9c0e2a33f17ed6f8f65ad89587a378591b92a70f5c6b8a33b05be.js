var log = 0;
var p = new Proxy({}, { set: function (t, k, v, r) { log++; return Reflect.set(t, k, v, r); } });
var o = Object.create(p);
for (var i = 0; i <= 9; i++) o.x = i;
log + ',' + o.hasOwnProperty('x') + ',' + o.x;