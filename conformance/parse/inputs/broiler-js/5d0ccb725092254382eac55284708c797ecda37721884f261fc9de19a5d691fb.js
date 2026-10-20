function w(o, useX) { if (useX) { o.x = 1; } else { o.y = 1; } return 1; }
var o = { x: 0, y: 0 };
var s = 0;
for (var i = 0; i < 200; i++) s += w(o, i % 2 === 0);
s + ',' + o.x + ',' + o.y;