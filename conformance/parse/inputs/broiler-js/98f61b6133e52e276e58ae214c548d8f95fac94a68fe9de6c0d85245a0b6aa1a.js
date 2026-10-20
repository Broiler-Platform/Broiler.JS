(function () {
    var first = 0;
    var second = 0;

    function* g() {
        first += 1;
        yield 0;
        second += 1;
    }

    var parts = [];
    (([,]) => { parts.push(first + '|' + second); })(g());

    first = 0;
    second = 0;
    (([[,] = g()]) => { parts.push(first + '|' + second); })([]);

    first = 0;
    second = 0;
    (([...[,]]) => { parts.push(first + '|' + second); })(g());

    return parts.join(';');
})()