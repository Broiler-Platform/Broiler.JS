(function () {
    var first = 0;
    var second = 0;

    function* g() {
        first += 1;
        yield;
        second += 1;
    }

    var callCount = 0;
    var f = ([,]) => {
        callCount += 1;
        return first + '|' + second;
    };

    var outcome = f(g());
    return outcome + ';' + callCount;
})()