(function () {
    var out = [];
    function* g() {
        try {
            yield 1;
            throw new Error('x');
        } finally {
            out.push('f');
            yield 2;
        }
    }
    var it = g();
    out.push(it.next().value);
    out.push(it.next().value);
    try {
        it.next();
        out.push('no');
    } catch (e) {
        out.push(e.message);
    }
    return out.join('|');
})()