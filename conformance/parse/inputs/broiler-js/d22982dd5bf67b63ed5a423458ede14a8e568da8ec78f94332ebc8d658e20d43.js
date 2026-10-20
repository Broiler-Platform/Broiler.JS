(function(){ var a = [1];
var o = { valueOf: function () { a[0] = 99; return 2; } };
var b = [o];
return b[0] * 3 + 2 + a[0]; })()