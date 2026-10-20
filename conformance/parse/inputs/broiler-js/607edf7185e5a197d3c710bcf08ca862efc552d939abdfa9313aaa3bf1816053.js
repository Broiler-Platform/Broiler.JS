(function(){ var bad = { valueOf: function () { throw new Error('coerce first'); } };
var a = [bad];
var n = null;
try { return a[0] * 2 + n.q; } catch (e) { return e.message; } })()