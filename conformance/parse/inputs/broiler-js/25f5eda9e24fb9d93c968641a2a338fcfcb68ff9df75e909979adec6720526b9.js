(function () {
    try {
        (function thunk() { new.target(); })();
        return 'no-throw';
    } catch (e) {
        return e.constructor.name;
    }
})()