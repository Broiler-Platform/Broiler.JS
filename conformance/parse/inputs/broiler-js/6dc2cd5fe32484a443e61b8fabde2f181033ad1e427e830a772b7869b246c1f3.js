function read(p) { var t = p.x; return t; }
var a = { x: 1 };
var out = [read(a), read(a), read(a)];
a.z = 5;
out.push(read(a), read(a));
out.join('|');