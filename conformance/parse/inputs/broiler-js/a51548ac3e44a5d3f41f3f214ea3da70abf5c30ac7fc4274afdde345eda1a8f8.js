(function () {
    var rest;
    ({ a: ignored, ...rest } = { a: 1, b: 2, 1: 3 });
    return [Object.keys(rest).join(","), rest[1], rest.b].join("|");
})()