var a = [];
a.tag = 1;
var seen = '';
for (var i = 0; i < 5; i++) { a.push(i); seen += a.length; }
var s = 0;
for (var i = 0; i < 500; i++) s += a.tag;
seen + '|' + s;