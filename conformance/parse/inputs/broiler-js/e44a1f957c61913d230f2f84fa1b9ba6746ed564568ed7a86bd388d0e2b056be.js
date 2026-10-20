function a() { return 1; }
var b = function () { return 2; };
var c = () => 3;
var o = { m: function () { return 4; }, get g() { return 5; } };
a(); b(); c(); o.m(); o.g;