(function () {
    function make() { var v = 0; return function () { v = v + 1; return v; }; }
    var a = make(), b = make();
    return a() + ',' + a() + ',' + b();
})()