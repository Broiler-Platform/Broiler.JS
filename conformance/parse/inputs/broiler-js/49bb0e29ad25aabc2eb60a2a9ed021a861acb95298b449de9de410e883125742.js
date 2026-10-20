(function () {
    var closed = false;
    var it = {
        [Symbol.iterator]() {
            var done = false;
            return {
                next() {
                    if (!done) { done = true; return { value: 1, done: false }; }
                    return { value: undefined, done: true };
                },
                return() { closed = true; return { done: true }; }
            };
        }
    };
    for (var z of it) {}
    return String(closed);
})();