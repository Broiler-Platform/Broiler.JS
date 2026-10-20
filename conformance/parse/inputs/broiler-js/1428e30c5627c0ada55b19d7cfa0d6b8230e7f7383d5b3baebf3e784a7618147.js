Array.prototype.tag = 1;
var a = [];
a.own = 1;
var s = 0;
for (var i = 0; i < 400; i++) { if (i === 200) Array.prototype.tag = 3; s += a.tag; }
s;