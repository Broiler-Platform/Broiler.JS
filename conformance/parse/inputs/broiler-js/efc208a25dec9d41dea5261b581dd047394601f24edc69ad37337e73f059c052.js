(function () {
    var functionCalls = 0;
    function run() {
        var eval = function (value) {
            functionCalls++;
            return value + 1;
        };

        return eval(1);
    }

    var withCalls = 0;
    var scope = {
        eval: function (value) {
            withCalls++;
            return value + 2;
        }
    };
    var withResult;
    with (scope) {
        withResult = eval(1);
    }

    var globalCalls = 0;
    function globalReplacement(value) {
        globalCalls++;
        return value + 3;
    }

    var originalEval = eval;
    eval = globalReplacement;
    var globalResult = eval(1);
    eval = originalEval;

    return [run(), functionCalls, withResult, withCalls, globalResult, globalCalls].join('|');
})();