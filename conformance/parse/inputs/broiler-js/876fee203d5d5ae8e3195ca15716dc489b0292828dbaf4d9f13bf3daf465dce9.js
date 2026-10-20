(function(){ var calls = 0;
var o = { get v() { calls++; return 6; } };
var r = o.v | 1;
return calls + ',' + r; })()