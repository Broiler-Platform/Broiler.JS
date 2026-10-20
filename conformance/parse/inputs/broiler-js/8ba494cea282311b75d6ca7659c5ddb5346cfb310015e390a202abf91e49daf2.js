function Point(x) { this.x = x; }
Point.prototype.scale = 3;
function read(p) { var t = p.scale; return t; }
var a = new Point(1);
[read(a), read(a), read(a), read(a)].join('|');