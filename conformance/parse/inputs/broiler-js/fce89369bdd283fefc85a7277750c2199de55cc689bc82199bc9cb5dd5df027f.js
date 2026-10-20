function f() {}
f.tag = 1;
var n = 0, last = 'x';
for (var i = 0; i < 300; i++) { last = f.caller; n++; }
n + '|' + last;