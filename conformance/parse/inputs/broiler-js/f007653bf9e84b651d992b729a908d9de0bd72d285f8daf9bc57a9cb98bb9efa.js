(function () {
    var x = 0;
    var o = { [x &&= 1]: 2 };

    var y = 0;
    class C {
        [y ||= 3]() { return y; }
    }

    var z = null;
    class D {
        [z ??= 'fallback'] = 4;
    }

    return [o[0], x, new C()[3](), y, new D().fallback, z].join('|');
})()