// SJL-14.1, part J seed: a slash read as a regular expression or as division by the token before it (rows T24a to
// T24i, K5), including after ), }, ], ++, an identifier, a keyword and a line terminator.
var a = 6, b = 2, g = 3, x = [8];
var r1 = a / b / g;
var r2 = x[0] / 2;
var r3 = (a) / b;
var r4 = /=/.test('a=b');
var r5 = typeof /x/g;
if (a) /y/.exec('y');
var r6 = a++ / 2;
var r7 = [/a/, /b/i].length;
[r1, r2, r3, r4, r5, r6, r7].join(',');
