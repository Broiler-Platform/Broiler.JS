var proto = { set x(v) { this.got = v; } };
var o = { __proto__: proto, run: function () { for (var i = 0; i <= 9; i++) super.x = i; return this.got; } };
o.run();