var o = { k: 1 };
var s = 0;
for (var i = 0; i < 400; i++) {
    if (i === 200) Object.defineProperty(o, 'k', { get: function () { return 5; }, configurable: true });
    s += o.k;
}
s;