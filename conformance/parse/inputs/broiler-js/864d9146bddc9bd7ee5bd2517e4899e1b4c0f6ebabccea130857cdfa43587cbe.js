var box = { valueOf: function () { throw 'boom'; } };
function probe(v) { var i = 1; return i < v; }
try { probe(box); 'no throw'; } catch (e) { e; }