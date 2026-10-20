function T() { this.a = 1; this.b = 2; this.c = 3; }
var last = null;
for (var i = 0; i < 400; i++) last = new T();
var d = Object.getOwnPropertyDescriptor(last, 'a');
[d.writable, d.enumerable, d.configurable, d.value, Object.keys(last).join(',')].join('|');