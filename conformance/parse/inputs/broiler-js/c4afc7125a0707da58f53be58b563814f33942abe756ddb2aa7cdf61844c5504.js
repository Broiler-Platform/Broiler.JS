function read(p) { var t = p.x; return t; }
var a = { x: 1 };
var b = { w: 0, x: 2 };
var out = [];
for (var i = 0; i < 5; i++) { out.push(read(a)); out.push(read(b)); }
out.join('|');