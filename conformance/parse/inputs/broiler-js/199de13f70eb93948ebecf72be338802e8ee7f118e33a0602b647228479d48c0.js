var o = {};
Object.defineProperty(o, 'x', { value: 1, writable: false, configurable: true });
for (var i = 0; i < 300; i++) o.x += 5;
o.x + '|' + Object.getOwnPropertyDescriptor(o, 'x').writable;