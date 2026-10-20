function f(a, b) {}
f.tag = 7;
for (var i = 0; i < 300; i++) f.tag = 7;
Object.defineProperty(f, 'length', { value: 9 });
Object.defineProperty(f, 'name', { value: 'renamed' });
[f.length, f.name, f.tag].join('|');