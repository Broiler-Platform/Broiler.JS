function add(o) { 'use strict'; o.x = 1; }
var made = 0, thrown = 0;
for (var i = 0; i < 400; i++) {
    var o = {};
    if (i >= 200) Object.preventExtensions(o);
    try { add(o); made++; } catch (e) { if (e instanceof TypeError) thrown++; }
}
made + '|' + thrown;