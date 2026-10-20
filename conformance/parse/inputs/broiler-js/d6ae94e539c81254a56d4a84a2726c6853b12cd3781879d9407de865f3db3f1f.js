(function(){ var a = [1n, 2];
try { return a[0] + a[1], 'no throw'; } catch (e) { return e.constructor.name; } })()