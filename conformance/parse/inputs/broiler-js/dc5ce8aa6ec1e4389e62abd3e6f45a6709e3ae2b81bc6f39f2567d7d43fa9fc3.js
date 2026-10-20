(function () {
    var v = 0;
    var gen = function* () { yield v; v = v + 1; yield v; };
    var it = gen();
    var a = it.next().value, b = it.next().value;
    return a + ',' + b + ',' + v;
})()