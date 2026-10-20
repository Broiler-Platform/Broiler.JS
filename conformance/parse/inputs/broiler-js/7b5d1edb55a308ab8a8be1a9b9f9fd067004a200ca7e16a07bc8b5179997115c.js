var a = [1];
a.tag = 7;
for (var i = 0; i < 300; i++) a.tag = 7;
Object.freeze(a);
a.tag = 99;
a[0] = 99;
[a[0], a.tag, Object.isExtensible(a), Object.getOwnPropertyDescriptor(a, 'tag').writable].join('|');