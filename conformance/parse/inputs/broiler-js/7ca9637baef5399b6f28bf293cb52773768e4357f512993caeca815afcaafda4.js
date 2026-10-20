// This is why `&&=`, `||=` and `??=` are excluded: whether the setter runs at all
// depends on the value read. A cached store would run it every time.
var g1 = 0, s1 = 0, g2 = 0, s2 = 0;
var o = {
    get a() { g1++; return 0; }, set a(v) { s1++; },
    get b() { g2++; return 0; }, set b(v) { s2++; }
};
for (var i = 0; i < 300; i++) { o.a &&= 1; o.b ||= 1; }
[g1, s1, g2, s2].join('|');