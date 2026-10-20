(function(){ var o = { x: 5 };
var a = o.x++;
var b = 5;
a.tag = 'x';
return (a === b) + ',' + (a == b) + ',' + Object.is(a, b) + ',' + a.tag; })()