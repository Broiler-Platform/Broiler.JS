// The store site is warm from the second iteration on; every instance after that
// must still land on the prototype its own constructor was given.
function install(f, proto) { f.prototype = proto; }
var right = 0, wrong = 0;
for (var i = 0; i < 300; i++) {
    var ctor = new Function('return function C() {};')();
    var proto = { tag: i };
    install(ctor, proto);
    var instance = new ctor();
    if (Object.getPrototypeOf(instance) === proto && instance.tag === i) right++; else wrong++;
}
right + '|' + (300 - wrong);