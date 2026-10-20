(function () {
    var src = { z: 1, a: 2, 1: 3 };
    var obj = { ...src };
    return Object.keys(obj).join(",");
})()