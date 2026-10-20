var a = [1, 2, 3];
a.tag = 9;
for (var i = 0; i < 300; i++) a.tag = i;
var sum = 0;
a.forEach(function (v) { sum += v; });
[a.join(','), sum, a.slice().reverse().join(','), JSON.stringify(a), a.indexOf(3)].join('|');