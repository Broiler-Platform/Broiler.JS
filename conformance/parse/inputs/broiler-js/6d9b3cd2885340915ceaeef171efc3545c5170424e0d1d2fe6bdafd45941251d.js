var o = { q: 1 };
var inner = function () { with (o) { return q; } };
inner();