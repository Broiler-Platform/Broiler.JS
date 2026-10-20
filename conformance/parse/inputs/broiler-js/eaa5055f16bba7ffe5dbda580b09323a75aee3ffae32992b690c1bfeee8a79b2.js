(function(){ var fs = [ function(){return 1;}, function(){return 2;}, function(){return 3;},
           function(){return 4;}, function(){return 5;} ];
var s = 0;
for (var i = 0; i < fs.length; i++) { s += fs[i](); }
return s; })()