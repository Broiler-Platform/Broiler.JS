(function () {
    var yield = 4;
    var x = 0;
    var vals = [];

    [x = yield] = vals;

    return x;
})()