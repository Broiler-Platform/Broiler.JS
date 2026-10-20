function T() { 'use strict'; this.x = 1; }
var made = 0, thrown = 0;
for (var i = 0; i < 400; i++) {
    if (i === 200)
        Object.defineProperty(T.prototype, 'x', { value: 5, writable: false });
    try { new T(); made++; } catch (e) { if (e instanceof TypeError) thrown++; }
}
made + '|' + thrown;