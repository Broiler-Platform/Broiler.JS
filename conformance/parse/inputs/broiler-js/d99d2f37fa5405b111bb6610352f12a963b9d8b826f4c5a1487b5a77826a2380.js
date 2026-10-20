function strict() { 'use strict'; return typeof this; }
function sloppy() { return typeof this; }
[strict(), sloppy()].join('|');