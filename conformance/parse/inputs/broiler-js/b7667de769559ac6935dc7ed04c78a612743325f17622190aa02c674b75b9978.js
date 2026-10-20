function read(p) { var t = p.length; return t; }
var a = { length: 1 };
var out = [read(a), read(a), read(a)];
out.push(read('abcd'));
out.join('|');