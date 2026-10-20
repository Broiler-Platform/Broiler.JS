var a = [1, 2, 3];
function viaLength() { var s = 0; for (var i = 0; i < a.length; i++) s = s + a[i]; return s; }
function viaLiteral() { var s = 0; for (var i = 0; i < 3; i++) s = s + a[i]; return s; }
[viaLength(), viaLiteral()].join('|');