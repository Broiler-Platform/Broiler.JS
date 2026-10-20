(function () {
    var closed = false;
    var it = {
        [Symbol.iterator]() {
            var i = 0;
            return {
                next() { return { value: i++, done: false }; },
                return() { closed = true; return { done: true }; }
            };
        }
    };
    for (var x of it) { break; }
    return String(closed);
})();