(function () {
    var obj = { x: 1 };
    var getter;
    with (obj) {
        getter = function () { return [x, eval(1)].join("|"); };
        obj.eval = function (value) { return value + 2; };
    }

    return getter();
})();