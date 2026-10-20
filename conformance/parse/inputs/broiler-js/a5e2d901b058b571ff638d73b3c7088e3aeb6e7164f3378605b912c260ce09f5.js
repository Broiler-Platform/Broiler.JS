function sink(x) { return x; }
function f() { var n = 0; var s = 0; for (var i = 0; i < 100; i++) { s = sink(n++); } return s; }
f();