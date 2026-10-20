(function(){ var a = [1, 2, 3n];
try { var r = a[0] + a[1] + a[2]; return 'no throw'; } catch (e) { return e.constructor.name; } })()