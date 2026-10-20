var calls = 0;
var first = { x: 1 };
var second = { w: 0, x: 2 };
var current = first;
function make() { calls++; return current; }
function read() { var t = make().x; return t; }
var out = [read(), read(), read()];
current = second;
out.push(read(), read());
out.join('|') + '|' + calls;