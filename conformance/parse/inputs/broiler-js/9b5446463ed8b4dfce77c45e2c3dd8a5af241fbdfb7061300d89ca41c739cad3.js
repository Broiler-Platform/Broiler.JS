(function () {
    var source = [];
    Object.defineProperty(source, "1", {
        enumerable: true,
        get: function () {
            source[2] = "late";
            return "one";
        }
    });
    source[3] = "three";

    var copy = { ...source };
    return [
        Object.keys(copy).join(","),
        copy[1],
        copy[3],
        typeof copy[2]
    ].join("|");
})()