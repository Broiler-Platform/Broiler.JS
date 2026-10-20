(function () {
    var source = {};
    var originalSymbol = Symbol("original");
    var lateSymbol = Symbol("late");
    Object.defineProperty(source, "a", {
        enumerable: true,
        get: function () {
            source.b = "late";
            source[lateSymbol] = "late-symbol";
            delete source.c;
            return "a";
        }
    });
    source.c = "deleted";
    source[originalSymbol] = "symbol";

    var copy = { ...source };
    return [
        Object.keys(copy).join(","),
        copy.a,
        typeof copy.b,
        lateSymbol in copy,
        copy[originalSymbol]
    ].join("|");
})()