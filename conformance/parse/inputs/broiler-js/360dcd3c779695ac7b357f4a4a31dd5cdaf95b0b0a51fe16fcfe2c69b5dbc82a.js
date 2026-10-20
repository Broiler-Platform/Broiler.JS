(function () {
    function snapshot(fn) {
        var descriptor = Object.getOwnPropertyDescriptor(fn, 'name');
        return JSON.stringify([descriptor.value, fn.name, descriptor.writable, descriptor.enumerable, descriptor.configurable]);
    }

    var direct;
    var parenthesized;
    direct = function () {};
    (parenthesized) = function () {};

    return snapshot(direct) + '|' + snapshot(parenthesized);
})()