var o = {};
Object.defineProperty(o, 'ne', { value: 1, enumerable: false, writable: true, configurable: true });
o.e = 2;
Object.keys(o).join(',') + '|' + Object.getOwnPropertyNames(o).join(',');