function poke(n) { var t = n; undeclaredGlobal = t; return t; }
var out = [];
for (var i = 0; i < 4; i++) { try { poke(1); out.push('ok'); } catch (e) { out.push(e.name); } }
out.join('|') + '|' + undeclaredGlobal;