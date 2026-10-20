(function () {
    function f(n) {
        var v = n;
        var r = function () { return v; };
        return r() + ':' + (n > 0 ? f(n - 1) : 0);
    }
    return f(3);
})()