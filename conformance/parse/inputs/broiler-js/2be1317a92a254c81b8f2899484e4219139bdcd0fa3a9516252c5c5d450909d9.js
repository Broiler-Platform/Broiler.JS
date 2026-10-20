(function () {
    class Test262Error extends Error {}

    function thrownCtor(fn) {
        try {
            fn();
            return 'no-throw';
        } catch (e) {
            return e.constructor.name;
        }
    }

    return [
        thrownCtor(function () {
            var base = null;
            var prop = function () {
                throw new RangeError();
            };
            var expr = function () {
                throw new Test262Error("right-hand side expression evaluated");
            };

            base[prop()] *= expr();
        }),
        thrownCtor(function () {
            var base = null;
            var prop = {
                toString() {
                    throw new Test262Error("property key evaluated");
                }
            };
            var expr = function () {
                throw new Test262Error("right-hand side expression evaluated");
            };

            base[prop] *= expr();
        }),
        thrownCtor(function () {
            var base = undefined;
            var prop = {
                toString() {
                    throw new Test262Error("property key evaluated");
                }
            };
            var expr = function () {
                throw new Test262Error("right-hand side expression evaluated");
            };

            base[prop] |= expr();
        })
    ].join('|');
})()