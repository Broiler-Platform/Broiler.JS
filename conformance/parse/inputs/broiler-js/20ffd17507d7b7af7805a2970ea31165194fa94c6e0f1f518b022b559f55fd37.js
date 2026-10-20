(function () {
    var yield = 23;
    var paramValue = 0;
    var f = (x = yield) => { paramValue = x; };

    f();

    return paramValue;
})()