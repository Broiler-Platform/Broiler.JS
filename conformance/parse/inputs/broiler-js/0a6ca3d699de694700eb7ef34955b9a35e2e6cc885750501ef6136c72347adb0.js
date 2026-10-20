(function () {
    var before = f;
    switch (1) { case 1: function f() { return 7; } }
    return '' + before + ',' + f();
})()