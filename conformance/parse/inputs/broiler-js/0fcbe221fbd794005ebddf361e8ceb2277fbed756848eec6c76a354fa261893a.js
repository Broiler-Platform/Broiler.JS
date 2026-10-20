(function () {
    var nextCount = 0;
    var returnCount = 0;
    var returnArgsLength = -1;
    var iterable = {};
    var iterator = {
        next: function() {
            nextCount += 1;
            return { done: nextCount > 10 };
        },
        return: function() {
            returnCount += 1;
            returnArgsLength = arguments.length;
            return {};
        }
    };
    var thrower = function() {
        throw new Error('boom');
    };
    iterable[Symbol.iterator] = function() {
        return iterator;
    };

    try {
        0, [...{}[thrower()]] = iterable;
        return 'no-throw';
    } catch (e) {
        return [e.message, nextCount, returnCount, returnArgsLength].join('|');
    }
})();