var o = { x: 0 };
for (var i = 0; i <= 9; i++) o.x = i;
var seen = -1;
var last = o.x;
Object.defineProperty(o, 'x', { get: function () { return seen; }, set: function (v) { seen = v; }, configurable: true });
o.x = 99;
o.x + ',' + last;