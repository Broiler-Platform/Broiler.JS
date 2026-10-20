(function () {
    var before = q;
    switch (1) { case 1: function q() { return 7; } }
    return '' + before + ',' + q();
})()