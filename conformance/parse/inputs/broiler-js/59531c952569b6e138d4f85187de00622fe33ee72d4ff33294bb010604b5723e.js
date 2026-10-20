function W() { this.v = 0; }
var o = new W();
for (var i = 0; i < 300; i++) o.v = i;
Object.defineProperty(o, 'v', { writable: false });
o.v = 1234;
String(o.v);