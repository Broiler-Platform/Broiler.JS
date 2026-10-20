(function () {
    var out = 'no';
    var v = 1;
    var f = async function () { v = v + 1; out = v; await 0; v = v + 10; };
    f();
    return String(out) + ',' + v;
})()