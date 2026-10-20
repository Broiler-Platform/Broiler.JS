var parent = { x: 1 };
var child = Object.create(parent);
function read() { return child.x; }
var first = read();
parent.x = 2;
var second = read();
Object.setPrototypeOf(child, { x: 3 });
[first, second, read()].join('|');