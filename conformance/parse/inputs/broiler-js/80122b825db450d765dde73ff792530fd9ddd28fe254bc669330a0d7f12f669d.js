function read(o) { return o.x; }
var objects = [{ x: 1 }, { a: 0, x: 2 }, { a: 0, b: 0, x: 3 }, { a: 0, b: 0, c: 0, x: 4 }];
var s = 0;
for (var i = 0; i < 4; i++) s += read(objects[i]);
s;