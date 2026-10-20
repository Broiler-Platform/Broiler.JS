var a = [];
a.tag = 9;
a.other = 1;
for (var i = 0; i < 10; i++) a[i] = i;
var named = [];
for (var k in a) { if (!/^[0-9]+$/.test(k)) named.push(k); }
[a.length, a.tag, Object.keys(a).length - a.length, named.join(',')].join('|');