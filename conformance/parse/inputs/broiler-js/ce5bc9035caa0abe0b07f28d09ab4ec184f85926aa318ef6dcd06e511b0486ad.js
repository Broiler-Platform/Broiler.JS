(function () {
    function* f([a]) {
        yield a;
    }

    return f([42]).next().value;
})()