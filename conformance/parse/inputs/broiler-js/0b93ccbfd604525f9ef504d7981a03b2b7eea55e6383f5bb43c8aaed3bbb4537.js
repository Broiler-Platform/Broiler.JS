var traps = 0;
var p = new Proxy({ k: 5 }, { get: function (t, key, r) { traps++; return Reflect.get(t, key, r); } });
var o = Object.create(p);
o.own = 1;
var last;
for (var i = 0; i < 400; i++) last = o.k;
traps + '|' + last;