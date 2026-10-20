(function(){ var a = [];
for (var i = 0; i < 1000; i++) a[i] = i;
var s = 0;
for (var i = 0; i < 1000; i++) s = s + a[i] * a[i] + a[i] * 2;
return s; })()