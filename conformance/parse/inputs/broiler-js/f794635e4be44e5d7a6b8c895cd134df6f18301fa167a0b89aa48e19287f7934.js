// Every iteration adds a new key, so the shape the store cache recorded on the way
// in is never the shape it finds on the way out.
var o = { x: 1 };
var k = 0;
function rhs() { o['f' + (k++)] = 1; return 10; }
for (var i = 0; i < 300; i++) { o.x = 1; o.x += rhs(); }
o.x;