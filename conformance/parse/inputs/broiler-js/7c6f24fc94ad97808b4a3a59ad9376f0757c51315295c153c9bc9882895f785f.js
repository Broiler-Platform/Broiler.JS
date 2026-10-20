(function () {
    var proto = {
        get value() { return 40; },
        method() { return 1; }
    };

    var store = {};
    var obj = {
        run() { return super.value + super.method(); },
        get prop() { return super.value; },
        set prop(v) { store.v = super.value + v; }
    };

    Object.setPrototypeOf(obj, proto);
    obj.prop = 2;

    return obj.run() + obj.prop + store.v;
})()