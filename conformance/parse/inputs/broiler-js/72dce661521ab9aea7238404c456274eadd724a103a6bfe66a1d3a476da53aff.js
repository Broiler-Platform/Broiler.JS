var o = { a: 0, b: 0 };
for (var i = 0; i <= 9; i++) { o.a = i; o.b = i * 2; }
var s = '';
for (var k in o) s += k + o[k];
JSON.stringify(o) + '|' + s;