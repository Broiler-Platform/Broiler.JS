(function () {
    var before = f;
    { function f() { return 42; } }
    return '' + before + ',' + f();
})()