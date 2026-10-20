function f(a, b) {}
var d = Object.getOwnPropertyDescriptor(f, 'length');
var p = Object.getOwnPropertyDescriptor(f, 'prototype');
[
    f.length,
    f.name,
    f.prototype.constructor === f,
    [d.writable, d.enumerable, d.configurable].join(','),
    [p.writable, p.enumerable, p.configurable].join(','),
    Object.getOwnPropertyNames(f).filter(function (k) { return k !== 'caller' && k !== 'arguments'; }).join(',')
].join('|');