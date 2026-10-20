function read(p) { var t = p.x; return t; }
var a = { x: 1 };
var out = [read(a), read(a), read(a)];
Object.defineProperty(a, 'x', { get: function () { return 42; }, configurable: true });
out.push(read(a), read(a));
out.join('|');