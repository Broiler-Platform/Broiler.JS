class C { #p = 3; constructor() { this.pub = 4; } read() { return this.#p; } }
var i = new C();
i.read() + ',' + i.pub + ',' + Object.keys(i).join(',');