var a = (function () { var x = 1; eval('x = 2'); return x; })();
var c = (function () { var x = 4; with ({}) { x = x; } return x; })();
var d = (function () { var x = 1; var ignored = eval('x = 2'); return x; })();
[a, c, d].join('|');