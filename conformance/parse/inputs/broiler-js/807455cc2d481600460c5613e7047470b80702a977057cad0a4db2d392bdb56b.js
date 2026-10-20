function one(p) { var t = p.x; return t; }
function two(p) { var t = p.x; return t; }
var flat = { x: 11 };
var deep = { a: 0, b: 0, x: 22 };
var out = [];
for (var i = 0; i < 4; i++) { out.push(one(flat)); out.push(two(deep)); }
out.join('|');