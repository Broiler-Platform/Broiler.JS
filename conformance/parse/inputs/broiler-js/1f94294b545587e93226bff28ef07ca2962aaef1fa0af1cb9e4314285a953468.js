var n = 0;
var o = { x: 0 };
function base() { n++; return o; }
for (var i = 0; i < 300; i++) base().x++;
n + '|' + o.x;