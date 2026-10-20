(function () {
    var out;
    try { out = String(g()); } catch (e) { out = e.constructor.name; }
    var s = 0;
    var g = function () { return s; };
    return out;
})()