var o = {};
Object.defineProperty(o, 'x', { value: 1, writable: false, configurable: true });
var r = 0;
for (var i = 0; i < 300; i++) r = (o.x += 5);
r + '|' + o.x;