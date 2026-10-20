var o = { a: 0, b: 0, c: 0 };
Object.defineProperty(o, 'b', { enumerable: false });
for (var i = 0; i <= 9; i++) { o.a = i; o.b = i; o.c = i; }
var d = Object.getOwnPropertyDescriptor(o, 'b');
Object.getOwnPropertyNames(o).join(',') + '|' + [d.value, d.writable, d.enumerable, d.configurable].join(',');