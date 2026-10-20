(function(){ var p = { v: 1 };
var o = { valueOf: function () { p.v = 20; return 4; } };
var a = [o];
return (a[0] * 2 * 3) + p.v; })()