(function () {
    try {
        for (const x of [1]) { x = 2; }
        return 'no-throw';
    } catch (e) {
        return e instanceof TypeError;
    }
})()