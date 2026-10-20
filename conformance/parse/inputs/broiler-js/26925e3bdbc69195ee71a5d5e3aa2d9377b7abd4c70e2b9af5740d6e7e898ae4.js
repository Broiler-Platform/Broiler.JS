function sloppy() { undeclaredA = 1; return 'ok'; }
function strict() { 'use strict'; try { undeclaredB = 1; return 'no throw'; } catch (e) { return e.name; } }
[sloppy(), strict()].join('|');