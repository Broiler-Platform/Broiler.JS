(function(){ var calls = 0;
var o = { valueOf: function () { calls++; return 3; } };
var a = [o];
var r = a[0] + 0;
return r + ',' + calls; })()