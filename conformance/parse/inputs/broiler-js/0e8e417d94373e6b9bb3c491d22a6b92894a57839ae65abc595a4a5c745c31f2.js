(function (n) {
    function h(a, b, c) { return a + b + c; }
    var s = 0;
    for (var i = 0; i < n; i++) { s = s + h(1, 2, 3); }
    return s;
})