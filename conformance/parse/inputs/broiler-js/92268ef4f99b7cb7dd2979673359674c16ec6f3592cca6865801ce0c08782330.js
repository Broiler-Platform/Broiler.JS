function add(o) { o.x = 1; }
var o = null;
for (var i = 0; i < 400; i++) { o = {}; add(o); }
var before = o.x;
delete o.x;
var gone = o.hasOwnProperty('x');
add(o);
[before, gone, o.x].join('|');