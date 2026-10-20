function C() { this.a = 1; this.b = 2; }
var last;
for (var i = 0; i < 200; i++) last = new C();
last.a + last.b;