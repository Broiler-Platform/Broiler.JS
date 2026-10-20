(function () {
    let f = 5;
    { function f() {} }
    return f;
})()