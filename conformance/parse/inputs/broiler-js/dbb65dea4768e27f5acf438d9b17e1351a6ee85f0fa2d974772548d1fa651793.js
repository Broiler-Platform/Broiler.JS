(function () {
    try {
        for (var {} of [null]) {
            return 'no-throw';
        }
        return 'no-throw';
    } catch (e) {
        return e.constructor.name;
    }
})()