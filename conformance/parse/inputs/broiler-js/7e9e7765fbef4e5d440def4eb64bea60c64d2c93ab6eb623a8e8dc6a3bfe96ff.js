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

    var { ...rest } = source;
    return [
        Object.keys(rest).join(","),
        rest[1],
        rest[3],
        typeof rest[2]
    ].join("|");
})()