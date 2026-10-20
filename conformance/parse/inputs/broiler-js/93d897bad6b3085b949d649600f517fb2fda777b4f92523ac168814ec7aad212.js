var o = { a: 1, b: 0, c: null };
for (var i = 0; i < 300; i++) { o.a &&= 2; o.b &&= 99; }
for (var i = 0; i < 300; i++) { o.a ||= 99; o.b ||= 7; }
for (var i = 0; i < 300; i++) { o.c ??= 5; }
[o.a, o.b, o.c].join('|');