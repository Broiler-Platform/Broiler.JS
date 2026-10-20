(function () {
    var iterable = {};
    iterable[Symbol.iterator] = function () {
        return {
            next() {
                return { value: null, done: false };
            },
            return() {
                throw new Error('iterator return should not mask destructuring error');
            }
        };
    };

    try {
        [[]] = iterable;
        return 'no-throw';
    } catch (e) {
        return e.constructor.name;
    }
})()