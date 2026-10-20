(function () {
    var fCalled = 0;
    var valueOfCalled = 0;

    function f() {
        fCalled++;
        return {
            valueOf: function () {
                valueOfCalled++;
                return 1;
            }
        };
    }

    function errorName(callback) {
        try {
            callback();
            return 'no-error';
        } catch (error) {
            return error.constructor.name;
        }
    }

    return [
        errorName(function () { f()++; }),
        fCalled,
        valueOfCalled,
        errorName(function () { ++f(); }),
        fCalled,
        valueOfCalled
    ].join('|');
})();