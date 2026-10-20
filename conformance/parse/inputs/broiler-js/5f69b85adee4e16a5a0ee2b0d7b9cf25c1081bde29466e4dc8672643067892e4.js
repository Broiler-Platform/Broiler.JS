(function () {
    var source = { a: 1, b: 2, 1: 3 };
    var { a, ...rest } = source;
    return [a, Object.keys(rest).join(","), rest[1], rest.b].join("|");
})()