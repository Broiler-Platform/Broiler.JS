function self(n) { var t = n; return self === globalThis.captured; }
globalThis.captured = self;
[self(1), self(1), self(1), self(1)].join('|');