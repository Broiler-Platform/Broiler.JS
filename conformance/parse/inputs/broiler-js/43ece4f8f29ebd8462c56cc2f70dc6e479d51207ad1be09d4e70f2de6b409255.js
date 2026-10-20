var o = {};
o.x = 1;
Object.defineProperty(o, 'y', { value: 2, writable: true, enumerable: true, configurable: true, enumerable: false });
String(Object.getOwnPropertyDescriptor(o, 'y').enumerable);