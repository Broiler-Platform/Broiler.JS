(function(){ var calls = 0;
var o = { valueOf: function () { calls++; return 7; } };
var a = [3, 4];
var r = a[0] + a[1] + o;
return r + ',' + calls; })()