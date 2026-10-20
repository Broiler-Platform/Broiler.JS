var x = []; x.tag = 1; x.other = 2;
var y = []; y.tag = 3; y.other = 4;
var rx = '', ry = '';
for (var i = 0; i < 300; i++) { rx = x.tag + ',' + x.other; ry = y.tag + ',' + y.other; }
rx + '|' + ry;