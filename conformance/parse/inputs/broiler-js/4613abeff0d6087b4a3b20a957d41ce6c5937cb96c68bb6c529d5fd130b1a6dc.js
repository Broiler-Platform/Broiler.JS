(function() {
var p0, p1;
var rhs = { p0: 0, p1: 1 };
var result = ({ p0, p1 } = rhs);
return result === rhs ? 'ok' : 'returned different object';
})()