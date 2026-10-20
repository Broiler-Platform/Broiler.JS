var o = {};
o.x = 1;
Object.defineProperty(o, 'y', { value: 2, writable: true, enumerable: true, configurable: true, writable: false });
String(Object.getOwnPropertyDescriptor(o, 'y').writable);