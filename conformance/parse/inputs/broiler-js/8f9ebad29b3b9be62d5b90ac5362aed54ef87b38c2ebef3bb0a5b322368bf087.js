var o = { x: 1 };
for (var i = 0; i < 300; i++) o.x = i;
var setterCalls = 0;
Object.defineProperty(o, 'x', {
    get: function () { return 55; },
    set: function (v) { setterCalls++; },
    configurable: true
});
o.x = 3;
o.x + ',' + setterCalls;