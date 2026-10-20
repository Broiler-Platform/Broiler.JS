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
    try { for (var y of it) { throw "err"; } } catch (e) {}
    return String(closed);
})();