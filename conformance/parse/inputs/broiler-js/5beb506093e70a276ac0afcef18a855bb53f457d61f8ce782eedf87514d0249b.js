var n = 0;
function base() { n++; return {}; }
for (var i = 0; i <= 9; i++) base().x = i;
n;