var a = [1, 2, 3];
a.tag = 7;
for (var i = 0; i < 300; i++) a.tag = 7;
Object.defineProperty(a, 'length', { value: 3, writable: false });
var threw = false;
try { a.push(4); } catch (e) { threw = true; }
[a.length, Object.getOwnPropertyDescriptor(a, 'length').writable, a.tag, a[2]].join('|');