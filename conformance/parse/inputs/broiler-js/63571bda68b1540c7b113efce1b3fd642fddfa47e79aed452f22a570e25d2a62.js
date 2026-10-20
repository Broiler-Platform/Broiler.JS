class C { #p = 0; set(v) { this.#p = v; return this.#p; } }
var c = new C();
var last = 0;
for (var i = 0; i < 200; i++) last = c.set(i);
last;