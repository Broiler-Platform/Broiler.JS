(function () {
    var initialized = false;
    var obj = {};
    obj[Symbol.toPrimitive] = 42;

    try {
        class C {
            [obj] = (initialized = true);
        }
        return 'no-throw';
    } catch (e) {
        return e.constructor.name + '|' + initialized;
    }
})()