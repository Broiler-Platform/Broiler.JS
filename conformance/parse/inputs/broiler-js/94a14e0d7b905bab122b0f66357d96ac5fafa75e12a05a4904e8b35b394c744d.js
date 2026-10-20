var taken = 0;
function T() { this.x = 1; }
var own = [];
for (var i = 0; i < 400; i++) {
    if (i === 200)
        Object.defineProperty(T.prototype, 'x', { set: function (v) { taken++; } });
    own.push(new T().hasOwnProperty('x'));
}
[own[0], own[199], own[200], own[399], taken].join('|');