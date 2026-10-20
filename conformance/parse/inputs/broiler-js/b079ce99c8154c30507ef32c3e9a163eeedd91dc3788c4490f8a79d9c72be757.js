(function () {
    function run(Ctor, valueFactory) {
        var iterable = {
            closed: false,
            [Symbol.iterator]: function () {
                var first = true;
                return {
                    next: function () {
                        if (first) {
                            first = false;
                            return { value: valueFactory(), done: false };
                        }

                        return { value: undefined, done: true };
                    },
                    return: function () {
                        iterable.closed = true;
                        return {};
                    }
                };
            }
        };

        try {
            new Ctor(iterable);
            return 'no-throw';
        } catch (e) {
            return String(iterable.closed) + '|' + e;
        }
    }

    class MyMap extends Map { set(_k, _v) { throw 'setter throws'; } }
    class MyWeakMap extends WeakMap { set(_k, _v) { throw 'setter throws'; } }
    class MySet extends Set { add(_v) { throw 'adder throws'; } }
    class MyWeakSet extends WeakSet { add(_v) { throw 'adder throws'; } }

    return [
        run(MyMap, function () { return [{}, {}]; }),
        run(MyWeakMap, function () { return [{}, {}]; }),
        run(MySet, function () { return {}; }),
        run(MyWeakSet, function () { return {}; })
    ].join('|');
})();