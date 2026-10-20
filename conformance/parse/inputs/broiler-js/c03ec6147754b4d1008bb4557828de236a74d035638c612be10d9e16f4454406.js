(function () {
    var proxy = new Proxy(() => {}, {
        get() {
            throw new Error("superclass.prototype should be unreachable");
        }
    });

    try {
        class C extends proxy {}
    } catch (e) {
        return e instanceof TypeError;
    }

    return false;
})()