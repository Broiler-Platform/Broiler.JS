(function () {
    var o = {
        get a() {
            return 42;
        }
    };

    var obj = {...o, c: 4, d: 5};
    var desc = Object.getOwnPropertyDescriptor(obj, "a");
    return [obj.a, desc.value, desc.writable, desc.enumerable, desc.configurable, Object.keys(obj).join(",")].join("|");
})()