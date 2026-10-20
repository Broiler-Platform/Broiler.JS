class MyArray extends Array {}
var a = new MyArray();
a.push(1, 2, 3);
a.tag = 9;
var s = 0;
for (var i = 0; i < 300; i++) s = a.tag;
[a.length, s, Array.isArray(a), (a.push(4), a.length)].join('|');