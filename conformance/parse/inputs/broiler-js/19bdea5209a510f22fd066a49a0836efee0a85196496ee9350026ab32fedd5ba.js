function read(p) { var t = p.x; return t; }
var a = { x: 1 };
var out = [read(a), read(a), read(a)];
try { read(null); out.push('no throw'); } catch (e) { out.push(e.name); }
out.join('|');