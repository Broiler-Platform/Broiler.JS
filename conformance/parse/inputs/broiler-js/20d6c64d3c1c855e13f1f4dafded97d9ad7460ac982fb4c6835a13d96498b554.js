var proto = { x: 0 };
var o = Object.create(proto);
var before = o.x;
for (var i = 0; i <= 9; i++) proto.x = i;
before + ',' + o.x;