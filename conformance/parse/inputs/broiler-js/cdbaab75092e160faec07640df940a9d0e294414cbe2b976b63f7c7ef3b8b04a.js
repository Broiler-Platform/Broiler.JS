(function () {
    var fs = [];
    for (var i = 0; i < 3; i++) { fs.push(function () { return i; }); }
    return fs[0]() + ',' + fs[1]() + ',' + fs[2]();
})()