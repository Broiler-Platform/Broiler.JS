(function(){ var calls = 0;
var o = { x: { valueOf: function () { calls++; return 1; } } };
var y = o.x++;
return calls + ',' + y + ',' + o.x; })()