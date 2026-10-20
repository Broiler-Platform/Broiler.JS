function strict() { 'use strict'; try { undeclaredA = 1; return 'no throw'; } catch (e) { return e.name; } }
function sloppy() { return strict() + '|' + (function () { undeclaredB = 1; return 'ok'; })(); }
sloppy();