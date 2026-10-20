(function(){ var os = [ {a:1}, {b:1,a:1} ];
var s = 0;
for (var i = 0; i < 40; i++) { s += os[i % 2].a; }
return s; })()