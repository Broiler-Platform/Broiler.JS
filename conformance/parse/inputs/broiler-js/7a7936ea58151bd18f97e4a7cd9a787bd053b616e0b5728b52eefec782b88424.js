(function(){ var os = [ {a:1}, {b:1,a:1}, {c:1,a:1}, {d:1,a:1}, {e:1,a:1} ];
var s = 0;
for (var i = 0; i < os.length; i++) { s += os[i].a; }
return s; })()