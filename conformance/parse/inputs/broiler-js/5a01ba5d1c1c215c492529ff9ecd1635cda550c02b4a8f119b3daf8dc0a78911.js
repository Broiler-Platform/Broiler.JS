var traps = 0;
function read(o) { return o.x; }
var a = { x: 1 };
var b = { a: 0, x: 2 };
var c = { a: 0, b: 0, x: 3 };
var d = { a: 0, b: 0, c: 0, x: 4 };
var e = { a: 0, b: 0, c: 0, d: 0, x: 5 };
var sum = 0;
for (var i = 0; i < 8; i++) sum += read(a);
sum += read(b) + read(c) + read(d) + read(e);
delete a.x;
a.x = 9;
sum += read(a);
var proxy = new Proxy({ x: 7 }, { get: function (target, key) { traps++; return target[key]; } });
sum += read(proxy) + read(proxy);
sum + '|' + traps;