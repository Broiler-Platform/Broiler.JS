var o = {};
o['3'] = 7;
var r = [];
for (var i = 0; i < 200; i++) r = [o[3], o['3'], o[3.0]];
r.join(',');