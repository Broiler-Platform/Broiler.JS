class A {}
class B extends A {}
function F() {}
function G() {}
G.prototype = 1;
var r = [
    Object.getPrototypeOf(new B()) === B.prototype,
    Object.getPrototypeOf(new B()) instanceof Object === false || A.prototype.isPrototypeOf(new B()),
    Object.getPrototypeOf(Reflect.construct(F, [], B)) === B.prototype,
    Object.getPrototypeOf(new F()) === F.prototype,
    Object.getPrototypeOf(new G()) === Object.prototype
];
r.join('|');