// The RHS overwrites the very property being compounded. Reading first means the
// write lands on 1 + 10; reading after it would land on 100 + 10.
var o = { x: 1 };
var n = 0;
function rhs() { o.x = 100; n++; return 10; }
for (var i = 0; i < 300; i++) { o.x = 1; o.x += rhs(); }
o.x + '|' + n;