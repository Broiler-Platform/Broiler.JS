(function() {
var p0, p1, p2, p3, p4, p5;
var rhs = { p0: 0, p1: 1, p2: 2, p3: 3, p4: 4, p5: 5 };
var result = ({ p0, p1, p2, p3, p4, p5 } = rhs);
return result === rhs ? 'ok' : 'returned different object';
})()