function add(o) { o.x = 1; return o.hasOwnProperty('x'); }
for (var i = 0; i < 400; i++) add({});
var open = {}, frozen = Object.freeze({});
add(open) + '|' + add(frozen);