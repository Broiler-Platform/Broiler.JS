// `__proto__` is an accessor on %Object.prototype%, so the chain is never free of
// the key and no transition entry may be installed for it.
var a = { marker: 1 };
function setProto(o, v) { o.__proto__ = v; }
var ownAny = false, allLinked = true, allMarked = true;
for (var i = 0; i < 400; i++) {
    var o = {};
    setProto(o, a);
    if (o.hasOwnProperty('__proto__')) ownAny = true;
    if (Object.getPrototypeOf(o) !== a) allLinked = false;
    if (o.marker !== 1) allMarked = false;
}
[ownAny, allLinked, allMarked].join('|');