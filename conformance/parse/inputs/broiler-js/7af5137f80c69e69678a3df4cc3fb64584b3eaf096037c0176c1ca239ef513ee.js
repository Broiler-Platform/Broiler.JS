function read(p) { var t = p.x; return t; }
var a = { x: 1 };
var out = [read(a), read(a), read(a)];
a.x = 7;
out.push(read(a), read(a));
out.join('|');