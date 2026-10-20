(function () {
    for (var key in { a: 1 }) {
        if (true) function f() { return key; }
        return f();
    }
})()