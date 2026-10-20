// The base is boxed per access, so the write lands on a wrapper nobody keeps -
// silent in sloppy mode, a TypeError in strict.
var s = 'ab';
for (var i = 0; i < 300; i++) s.length += 1;

var n = 5;
var last = 0;
for (var i = 0; i < 300; i++) last = (n.foo += 1);

function add(v) { 'use strict'; v.foo += 1; }
var thrown = 0;
for (var i = 0; i < 300; i++) {
    try { add(5); } catch (e) { if (e instanceof TypeError) thrown++; }
}
[s.length, last, thrown].join('|');