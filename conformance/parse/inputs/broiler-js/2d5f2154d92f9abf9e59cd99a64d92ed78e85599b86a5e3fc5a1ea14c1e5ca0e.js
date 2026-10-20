// Two compound assignments in one expression, each with its own base temp; the
// inner one is the outer one's right-hand side.
var o = { x: 0, y: 0 };
var p = { z: 0 };
for (var i = 0; i < 300; i++) o.x += (o.y += 1) - (p.z += 1);
[o.y + p.z, o.y, p.z].join('|');