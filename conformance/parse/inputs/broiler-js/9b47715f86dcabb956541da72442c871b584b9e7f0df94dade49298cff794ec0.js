function bump(o) { 'use strict'; o.x++; }
var made = 0, thrown = 0;
for (var i = 0; i < 400; i++) {
    var o = { x: 1 };
    if (i >= 200) Object.freeze(o);
    try { bump(o); made++; } catch (e) { if (e instanceof TypeError) thrown++; }
}
made + '|' + thrown;