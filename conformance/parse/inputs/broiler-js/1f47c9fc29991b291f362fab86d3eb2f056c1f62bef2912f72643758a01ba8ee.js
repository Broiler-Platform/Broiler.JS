(function () {
    var iterCount = 0;
    for (const [[x, y, z] = [4, 5, 6], , { p } = { p: 7 }] = [, , undefined]; iterCount < 1; ) {
        if (x !== 4 || y !== 5 || z !== 6 || p !== 7) {
            return 'bad-bindings';
        }
        iterCount += 1;
    }
    return String(iterCount);
})()