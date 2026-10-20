var o = { valueOf: function () { return 1.5; } }, b = 2.5, n = 0;
for (var i = 0; i < 50; i++) { if (o < b) { n = n + 1; } }
n;