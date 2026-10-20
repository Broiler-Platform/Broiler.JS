(function () {
    var returnCount = 0;
    var iter = {
        [Symbol.iterator]() {
            var done = false;
            return {
                next() {
                    if (!done) {
                        done = true;
                        return { value: 1, done: false };
                    }
                    return { value: undefined, done: true };
                },
                return() {
                    returnCount += 1;
                    return { value: undefined, done: true };
                }
            };
        }
    };

    var [a, b] = iter;
    return returnCount + '|' + a + '|' + b;
})()