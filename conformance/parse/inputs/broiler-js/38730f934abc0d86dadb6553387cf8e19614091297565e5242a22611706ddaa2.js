var o = { x: 0 };
for (var i = 0; i < 300; i++) { o.x += 1; o.x = o.x + 1; o.x++; }
o.x;