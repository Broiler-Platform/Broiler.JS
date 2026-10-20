(function () {
    var g = function* () {
        if (true) {
            yield 42;
        }
    };
    var iterator = g();
    return iterator.next().value + "|" + iterator.next().done;
})();