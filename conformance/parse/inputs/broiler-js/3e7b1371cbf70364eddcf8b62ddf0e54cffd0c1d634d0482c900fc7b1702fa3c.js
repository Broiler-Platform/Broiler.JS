function P(v) { this.v = v; }
P.prototype.get = function () { return this.v; };
var p = new P(7);
var s = 0;
for (var i = 0; i < 500; i++) s += p.get();
s;