var gets = 0, sets = 0;
var target = { x: 0 };
var p = new Proxy(target, {
    get: function (t, k, r) { gets++; return Reflect.get(t, k); },
    set: function (t, k, v, r) { sets++; return Reflect.set(t, k, v); }
});
for (var i = 0; i < 300; i++) p.x++;
[gets, sets, target.x].join('|');