(function () {
    var initCount = 0;
    return (({ a = (++initCount, 'fallback') }) => String(a) + '|' + initCount)({ a: null });
})()