function strict() { 'use strict'; return 1; }
function sloppy() { strict(); undeclaredAfter = 1; return 'ok'; }
sloppy();