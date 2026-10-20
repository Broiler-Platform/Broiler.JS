// The property and [[Construct]] must never disagree — that disagreement WAS the bug.
function F() {}
var agreed = 0, constructed = 0;
for (var i = 0; i < 400; i++) {
    var proto = { n: i };
    F.prototype = proto;
    if (F.prototype === proto) agreed++;
    if (Object.getPrototypeOf(new F()) === proto) constructed++;
}
agreed + '|' + constructed;