function probe(v) { var i = 1; return i < v; }
try { probe(Symbol('s')); 'no throw'; } catch (e) { 'threw'; }