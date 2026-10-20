function F() {}
var p = null;
for (var i = 0; i < 500; i++) p = F.prototype;
(p === F.prototype).toString();