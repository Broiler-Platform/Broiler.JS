var o = { valueOf: function () { return 2; } };
var a = [o];
var s = 0;
for (var i = 0; i < 10; i++) s = a[0] * 3;
s;