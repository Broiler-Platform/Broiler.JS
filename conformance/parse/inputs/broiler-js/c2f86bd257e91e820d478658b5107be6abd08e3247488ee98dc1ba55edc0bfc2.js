// AssignPrototypeField's other half: a function that HAS had a prototype object is a
// constructor for good, even after `prototype` is set to a primitive. Reflect.construct
// consults that, so a warmed site must not lose it either.
function F() { this.ok = 1; }
var constructed = 0, accepted = 0;
for (var i = 0; i < 300; i++) {
    F.prototype = 7;
    if (new F().ok === 1) constructed++;
    try { Reflect.construct(Object, [], F); accepted++; } catch (e) { /* would be the bug */ }
}
constructed + '|' + accepted;