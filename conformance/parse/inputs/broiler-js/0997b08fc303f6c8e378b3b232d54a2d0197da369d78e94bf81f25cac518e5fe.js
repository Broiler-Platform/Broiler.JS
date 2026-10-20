function read(p) { var t = p.x; return t; }
var a = { x: 1 };
var out = [read(a), read(a), read(a)];
var b = { w: 0, x: 9 };
out.push(read(b), read(b));
out.join('|');