function S() {}
Object.setPrototypeOf(S, { k: 5 });
S.k = 1;
var s = 0;
for (var i = 0; i < 400; i++) { if (i === 200) delete S.k; s += S.k; }
s;