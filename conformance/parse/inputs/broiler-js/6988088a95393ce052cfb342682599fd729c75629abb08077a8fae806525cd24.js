// The first iteration reads the prototype's 10 and creates an own 11; every one
// after that reads the own property it just made.
function P() {}
P.prototype.x = 10;
var q = new P();
for (var i = 0; i < 300; i++) q.x += 1;
q.x + '|' + P.prototype.x;