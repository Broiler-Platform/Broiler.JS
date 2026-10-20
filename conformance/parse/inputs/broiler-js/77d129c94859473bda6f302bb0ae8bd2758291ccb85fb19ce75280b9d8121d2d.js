var calls = 0;
var target = { x: 1 };
function make() { calls++; return target; }
function read() { var t = make().x; return t; }
var out = [read(), read(), read(), read()];
out.join('|') + '|' + calls;