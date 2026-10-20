function W() { this.v = 0; }
for (var i = 0; i < 300; i++) { var w = new W(); w.v = i; }
var frozen = new W();
Object.freeze(frozen);
frozen.v = 99;
String(frozen.v);