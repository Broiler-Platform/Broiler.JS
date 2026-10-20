function P() { this.x = 0; }
P.prototype.set = function (v) { this.x = v; };
var p = new P();
for (var i = 0; i < 500; i++) p.set(i);
p.x;