function read(p) { var t = p.a + ':' + p.b + ':' + p.c; return t; }
var o = { a: 'A', b: 'B', c: 'C' };
[read(o), read(o), read(o), read(o)].join('|');