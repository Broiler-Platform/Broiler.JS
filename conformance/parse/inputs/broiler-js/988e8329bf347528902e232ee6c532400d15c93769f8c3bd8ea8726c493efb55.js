(function() {
var p0, p1, p2;
var rhs = { p0: 0, p1: 1, p2: 2 };
var result = ({ p0, p1, p2 } = rhs);
return result === rhs ? 'ok' : 'returned different object';
})()