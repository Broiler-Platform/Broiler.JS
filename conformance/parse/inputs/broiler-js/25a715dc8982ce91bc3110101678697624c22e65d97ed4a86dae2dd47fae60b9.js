var a = {}; a.x = 1;
var b = {}; b.x = 1;
Object.defineProperty(b, 'x', { writable: false });
String(Object.getOwnPropertyDescriptor(a, 'x').writable) + ',' +
    String(Object.getOwnPropertyDescriptor(b, 'x').writable);