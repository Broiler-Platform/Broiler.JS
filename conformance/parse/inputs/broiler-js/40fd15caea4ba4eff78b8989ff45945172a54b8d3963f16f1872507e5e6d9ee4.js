function add(o) { o.x = 1; return o.hasOwnProperty('x'); }
var own = [];
for (var i = 0; i < 400; i++) {
    var o = {};
    if (i >= 200) Object.preventExtensions(o);
    own.push(add(o));
}
[own[0], own[199], own[200], own[399]].join('|');