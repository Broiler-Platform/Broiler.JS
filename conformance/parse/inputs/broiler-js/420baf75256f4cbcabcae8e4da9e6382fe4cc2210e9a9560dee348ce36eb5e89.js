var gets = 0;
var proto = { get x() { gets++; return 1; } };
var o = Object.create(proto);
for (var i = 0; i < 300; i++) o.x += 1;
var own = Object.getOwnPropertyNames(o).length;

function add(t) { 'use strict'; t.x += 1; }
var thrown = 0;
for (var i = 0; i < 300; i++) {
    try { add(Object.create(proto)); } catch (e) { if (e instanceof TypeError) thrown++; }
}
[gets - thrown, own, thrown].join('|');