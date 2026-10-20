function P() {}
P.prototype.m = function () { return 1; };
var p = new P();
var s = 0;
for (var i = 0; i < 400; i++) { if (i === 200) p.m = function () { return 5; }; s += p.m(); }
s;