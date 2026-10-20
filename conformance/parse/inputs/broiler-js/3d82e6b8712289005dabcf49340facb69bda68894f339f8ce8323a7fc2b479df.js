var taken = 0;
function T() { this.x = 1; }
Object.defineProperty(T.prototype, 'x', {
    set: function (v) { taken++; },
    get: function () { return 99; }
});
var last = null;
for (var i = 0; i < 400; i++) last = new T();
[taken, last.hasOwnProperty('x'), last.x].join('|');