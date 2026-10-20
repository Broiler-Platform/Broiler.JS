var o = {};
for (var i = 0; i < 40; i++) o['k' + i] = i;
var valuesOk = true;
for (var i = 0; i < 40; i++) if (o['k' + i] !== i) valuesOk = false;
var keys = Object.keys(o);
var orderOk = true;
for (var i = 0; i < 40; i++) if (keys[i] !== 'k' + i) orderOk = false;
String(valuesOk) + ',' + keys.length + ',' + String(orderOk);