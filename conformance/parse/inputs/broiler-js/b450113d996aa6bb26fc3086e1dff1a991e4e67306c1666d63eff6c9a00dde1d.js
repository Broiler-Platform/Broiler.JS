(function () {
    try {
        for (const x = 0; x < 1; x++) {}
        return 'no-throw';
    } catch (e) {
        return e instanceof TypeError;
    }
})()