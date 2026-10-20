(function () {
    class C {
        foo = 1;
        static bar = 2;
    }

    var instanceDescriptor = Object.getOwnPropertyDescriptor(new C(), 'foo');
    var staticDescriptor = Object.getOwnPropertyDescriptor(C, 'bar');
    return [
        instanceDescriptor.value,
        instanceDescriptor.enumerable,
        instanceDescriptor.writable,
        instanceDescriptor.configurable,
        staticDescriptor.value,
        staticDescriptor.enumerable,
        staticDescriptor.writable,
        staticDescriptor.configurable
    ].join('|');
})()