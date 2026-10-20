(function() {
var p0, p1, p2, p3;
var rhs = { p0: 0, p1: 1, p2: 2, p3: 3 };
var result = ({ p0, p1, p2, p3 } = rhs);
return result === rhs ? 'ok' : 'returned different object';
})()