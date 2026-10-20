(function () {
    var array = [];
    array[1] = 2;
    array[4294967294] = 7;

    var copy = { ...array };
    return [
        Object.keys(copy).join(","),
        copy[1],
        copy[4294967294],
        typeof copy.length
    ].join("|");
})()