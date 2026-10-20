(function () {
    function printCodePoint(codePoint) {
        var hex = codePoint
            .toString(16)
            .toUpperCase()
            .padStart(6, "0");
        return `U+${hex}`;
    }

    return printCodePoint(255);
})()