var taken = 0;
var plain = {};
var trapped = {};
Object.defineProperty(trapped, 'x', { set: function (v) { taken++; } });
function add(o) { o.x = 1; return o.hasOwnProperty('x') ? 1 : 0; }
var ownOnPlain = 0, ownOnTrapped = 0;
for (var i = 0; i < 400; i++) {
    ownOnPlain += add(Object.create(plain));
    ownOnTrapped += add(Object.create(trapped));
}
[taken, ownOnPlain, ownOnTrapped].join('|');