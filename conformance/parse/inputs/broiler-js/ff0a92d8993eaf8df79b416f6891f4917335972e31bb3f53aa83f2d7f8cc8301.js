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
            var prop = {
                toString() {
                    throw new Test262Error("property key evaluated");
                }
            };

            base[prop]++;
        }),
        thrownCtor(function () {
            var base = null;
            var prop = {
                toString() {
                    throw new Test262Error("property key evaluated");
                }
            };

            base[prop]--;
        }),
        thrownCtor(function () {
            var base = undefined;
            var prop = {
                toString() {
                    throw new Test262Error("property key evaluated");
                }
            };

            ++base[prop];
        }),
        thrownCtor(function () {
            var base = undefined;
            var prop = {
                toString() {
                    throw new Test262Error("property key evaluated");
                }
            };

            --base[prop];
        })
    ].join('|');
})()