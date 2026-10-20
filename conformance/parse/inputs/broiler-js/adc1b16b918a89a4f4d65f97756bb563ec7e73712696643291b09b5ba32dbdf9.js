var a = [0, , 2];
Object.prototype[1] = 'inherited';
var i = 1;
var r = a[i];
delete Object.prototype[1];
String(r);