function S() {}
S.k = 1;
var s = 0;
for (var i = 0; i < 400; i++) {
    if (i === 200) Object.defineProperty(S, 'k', { get: function () { return 3; } });
    s += S.k;
}
s;