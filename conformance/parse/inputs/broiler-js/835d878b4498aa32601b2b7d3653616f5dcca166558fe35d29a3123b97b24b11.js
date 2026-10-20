var o = { x: 0 };
for (var i = 0; i <= 9; i++) o.x = i;
Object.preventExtensions(o);
o.x = 99;
o.z = 1;
o.x + ',' + String(o.z);