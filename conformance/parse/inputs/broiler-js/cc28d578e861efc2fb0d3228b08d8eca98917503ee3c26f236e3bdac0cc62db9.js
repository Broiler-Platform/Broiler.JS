(function () {
    var saved = Array.prototype[Symbol.iterator];
    try {
        Array.prototype[Symbol.iterator] = {};
        for (var [x, y, z] of [[1, 2, 3]]) {
            return 'no-throw';
        }
        return 'no-throw';
    } catch (e) {
        return e.constructor.name;
    } finally {
        Array.prototype[Symbol.iterator] = saved;
    }
})()