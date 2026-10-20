var seen = 0;
var o = {};
Object.defineProperty(o, 2, { set: function (v) { seen = v; }, configurable: true });
var i = 2;
o[i] = 5;
String(seen);