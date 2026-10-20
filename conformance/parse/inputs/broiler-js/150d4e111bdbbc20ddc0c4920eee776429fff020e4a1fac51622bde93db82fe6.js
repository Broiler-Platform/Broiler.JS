var o = { x: 0 };
Object.defineProperty(o, 'g', { get: function () { return 1; } });
for (var i = 0; i <= 9; i++) o.x = i;
o.x = 99;
o.x;