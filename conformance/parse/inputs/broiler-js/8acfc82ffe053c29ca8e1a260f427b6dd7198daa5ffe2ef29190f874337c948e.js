var o = {};
Object.defineProperty(o, 3, { get: function () { return 'getter'; }, configurable: true });
var i = 3;
String(o[i]);