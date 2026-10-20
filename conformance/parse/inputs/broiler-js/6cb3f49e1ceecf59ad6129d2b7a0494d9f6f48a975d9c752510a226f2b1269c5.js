var i = 0;
var n = null;
var u;
var a = 'x', b = 'y';
try { n[i] = 1; } catch (e) { a = e.message; }
try { u[i] = 1; } catch (e) { b = e.message; }
a + '|' + b;