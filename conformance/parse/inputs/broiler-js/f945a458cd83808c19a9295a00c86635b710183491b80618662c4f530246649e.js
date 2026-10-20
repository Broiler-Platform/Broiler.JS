var calls = 0;
var proto = { get k() { calls++; return 5; } };
var o = { __proto__: proto, own: 1 };
var s = 0;
for (var i = 0; i < 400; i++) s += o.k;
s + '|' + calls;