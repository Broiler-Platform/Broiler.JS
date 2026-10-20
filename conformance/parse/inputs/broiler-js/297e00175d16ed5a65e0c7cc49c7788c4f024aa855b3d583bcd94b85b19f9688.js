var taken = 0;
var plain = {};
var trapped = {};
Object.defineProperty(trapped, 'x', { set: function (v) { taken++; } });
function add(o) { o.x = 1; return o.hasOwnProperty('x'); }
var own = [];
for (var i = 0; i < 400; i++) {
    var o = {};
    if (i >= 200) Object.setPrototypeOf(o, trapped);
    own.push(add(o));
}
[own[0], own[399], taken].join('|');