var o = { x: 0, y: 1 };
for (var i = 0; i <= 9; i++) o.x = i;
delete o.x;
o.x = 99;
o.x + ',' + Object.keys(o).join(',');