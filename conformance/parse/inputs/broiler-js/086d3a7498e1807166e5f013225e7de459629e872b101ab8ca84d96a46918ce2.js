Array.prototype.tag = 5;
var a = [];
a.tag = 1;
var s = 0;
for (var i = 0; i < 400; i++) { if (i === 200) delete a.tag; s += a.tag; }
s;