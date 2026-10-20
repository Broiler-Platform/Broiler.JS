'use strict';
var o = { x: 0 };
for (var i = 0; i <= 9; i++) o.x = i;
Object.freeze(o);
(function () { try { o.x = 99; return 'no-throw'; } catch (e) { return e.constructor.name; } })();