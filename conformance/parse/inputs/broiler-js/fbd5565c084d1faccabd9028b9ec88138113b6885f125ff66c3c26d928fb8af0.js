var proto = { set x(v) { this.seen = v; } };
var o = Object.create(proto);
for (var i = 0; i < 500; i++) o.x = i;
o.seen;