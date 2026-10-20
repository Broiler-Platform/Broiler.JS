function T() { this.x = 1; }
Object.defineProperty(T.prototype, 'x', { value: 5, writable: false });
var last = null;
for (var i = 0; i < 400; i++) last = new T();
[last.hasOwnProperty('x'), last.x].join('|');