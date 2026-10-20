var a = new Float64Array(4);
a.tag = 9;
var s = 0;
for (var i = 0; i < 300; i++) s = a.tag;
[a.length, s, a[0]].join('|');