var a = [];
var i = 2;
a[i] = (function () { i = 9; return 'set'; })();
'at2:' + a[2] + '|at9:' + a[9];