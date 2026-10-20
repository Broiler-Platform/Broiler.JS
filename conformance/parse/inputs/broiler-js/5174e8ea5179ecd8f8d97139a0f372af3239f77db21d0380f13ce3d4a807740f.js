function P() {}
P.prototype.x = -1;
var p = new P();
for (var i = 0; i < 500; i++) p.x = i;
p.x + ',' + P.prototype.x;