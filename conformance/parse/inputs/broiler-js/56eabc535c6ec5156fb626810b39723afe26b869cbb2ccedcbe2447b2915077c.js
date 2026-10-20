var a = [];
a[4] = 1;
a.tag = 7;
for (var i = 0; i < 300; i++) a.tag = 7;
[a.length, 2 in a, 4 in a, a.tag].join('|');