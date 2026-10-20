(function () {
    var array = [];
    array[0] = "skip";
    array[5] = "keep";
    array[4294967294] = "last";

    var { 0: first, ...rest } = array;
    return [
        first,
        Object.keys(rest).join(","),
        rest[5],
        rest[4294967294],
        typeof rest[0]
    ].join("|");
})()