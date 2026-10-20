var a = [1, 2, 3];
a.tag = 7;
var s = 0;
for (var i = 0; i < 500; i++) s += a.tag;
s;