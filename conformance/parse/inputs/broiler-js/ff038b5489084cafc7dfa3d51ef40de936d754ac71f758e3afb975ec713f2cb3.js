(function () {
    var proxy = new Proxy(() => {}, {
        get() {
            throw new Error("proxy trap should not be touched");
        }
    });

    return 1;
})()