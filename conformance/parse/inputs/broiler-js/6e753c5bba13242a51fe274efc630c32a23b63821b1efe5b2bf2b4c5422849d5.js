function read(p) { var t = p.x; return t; }
var a = { x: 1, y: 2 };
var out = [read(a), read(a), read(a)];
delete a.y;
out.push(read(a), read(a));
out.join('|');