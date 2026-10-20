(function () {
    var obj = {
        method() { return super.tag; }
    };

    Object.setPrototypeOf(obj, { tag: 7 });
    return obj.method();
})()