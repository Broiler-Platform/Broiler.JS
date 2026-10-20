(function () {
    function errorName(callback) {
        try {
            callback();
            return 'no-error';
        } catch (error) {
            return error.constructor.name;
        }
    }

    var executed = false;
    var superCallInFunction = errorName(function () {
        eval('executed = true; super();');
    });

    var indirectContinue = errorName(function () {
        (0, eval)('continue;');
    });

    var indirectSuperCall = errorName(function () {
        (0, eval)('executed = true; super();');
    });

    class Base {
        get test262() {
            return 262;
        }
    }
    class Derived extends Base {
        method() {
            return eval('super.test262;');
        }
    }
    var superPropInMethod = new Derived().method();

    return [superCallInFunction, indirectContinue, indirectSuperCall, executed, superPropInMethod].join('|');
})();