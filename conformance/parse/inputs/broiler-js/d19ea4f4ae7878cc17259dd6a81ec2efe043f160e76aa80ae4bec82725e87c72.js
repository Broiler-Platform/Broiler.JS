(function () {
    var initCount = 0;
    var source = { a: null, b: undefined };
    var { a = (++initCount, 'fallback-a'), b = (++initCount, 'fallback-b') } = source;
    return String(a) + '|' + b + '|' + initCount;
})()