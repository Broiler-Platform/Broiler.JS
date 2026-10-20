(function(){ var o = { x: Symbol('s') };
try { o.x++; return 'no throw'; } catch (e) { return e.constructor.name; } })()