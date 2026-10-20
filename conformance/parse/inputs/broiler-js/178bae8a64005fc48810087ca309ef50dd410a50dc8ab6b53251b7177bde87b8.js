(function () {
    var out = 'no';
    var v = 1;
    var f = async function () { v = v + 1; out = v; };
    f();
    return String(out) + ',' + v;
})()