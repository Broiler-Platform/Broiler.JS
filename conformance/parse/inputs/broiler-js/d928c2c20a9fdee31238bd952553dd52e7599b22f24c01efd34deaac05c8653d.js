var log = [];
function L(x) { log.push('L'); return x; }
function R(x) { log.push('R'); return x; }
function nativeLeft(n) { var i = 1; return (i + L(0)) < R(n); }
function nativeRight(n) { var i = 1; return R(n) > (i + L(0)); }
var a = nativeLeft(2), first = log.join(',');
log = [];
var b = nativeRight(2), second = log.join(',');   // R is written first here
[a, first, b, second].join('|');