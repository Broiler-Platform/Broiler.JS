var proto = {};
var o = Object.create(proto);
o.x = 0;
for (var i = 0; i <= 9; i++) o.x = i;
var seen = -1;
Object.defineProperty(proto, 'x', { set: function (v) { seen = v; }, configurable: true });
o.x = 99;
o.x + ',' + seen;