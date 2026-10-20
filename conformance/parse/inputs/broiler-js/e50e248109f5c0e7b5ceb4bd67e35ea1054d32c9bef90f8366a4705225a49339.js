var a = [];
var i = 0;
i = 1;    a[i] = 'one';
i = 1.5;  a[i] = 'frac';
i = -1;   a[i] = 'neg';
i = 1;    var r1 = a[i];
i = 1.5;  var r2 = a[i];
i = -1;   var r3 = a[i];
'1:' + r1 + '|1.5:' + r2 + '|-1:' + r3;