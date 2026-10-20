function read(o) { return o.x; }
var objects = [
    { x: 1 }, { a: 0, x: 2 }, { a: 0, b: 0, x: 3 },
    { a: 0, b: 0, c: 0, x: 4 }, { a: 0, b: 0, c: 0, d: 0, x: 5 },
    { a: 0, b: 0, c: 0, d: 0, e: 0, x: 6 }
];
var s = 0;
for (var i = 0; i < 6; i++) s += read(objects[i]);
s;