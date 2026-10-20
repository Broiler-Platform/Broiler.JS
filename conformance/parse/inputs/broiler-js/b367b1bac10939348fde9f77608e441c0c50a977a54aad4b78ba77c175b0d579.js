(function () {
    var initialized = false;
    var obj = {};
    obj[Symbol.toPrimitive] = function () { return {}; };

    try {
        class C {
            static [obj] = (initialized = true);
        }
        return 'no-throw';
    } catch (e) {
        return e.constructor.name + '|' + initialized;
    }
})()