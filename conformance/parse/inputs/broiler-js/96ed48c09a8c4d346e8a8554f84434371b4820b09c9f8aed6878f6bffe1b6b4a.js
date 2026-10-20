var x = 1;
function read() { return x; }
var before = read();
x = 2;
var after = read();
before + '/' + after;