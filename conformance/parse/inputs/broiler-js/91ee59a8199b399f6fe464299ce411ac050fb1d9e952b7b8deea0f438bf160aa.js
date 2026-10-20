(function () {
    var plainObject = {
        get x() {
            return this.y;
        }
    };
    var proxy = new Proxy(new Proxy(plainObject, {}), { get: undefined });
    var derived = Object.create(proxy);
    derived.y = 1;
    return derived.x;
})()