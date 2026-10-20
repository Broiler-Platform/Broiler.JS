(function () {
    var calls = 0;
    var cooked = '';
    var raw = '';

    (function(parts) {
        calls += 1;
        cooked = parts[0];
        raw = parts.raw[0];
    })`\x41`;

    return [calls, cooked, raw].join('|');
})()