(function () {
    try {
        new (() => {});
        return 'no-throw';
    } catch (e) {
        return e.constructor.name;
    }
})()