(function () {
    function parsesSuccessfully(code) {
        try {
            Function(code);
            return true;
        } catch (_error) {
            return false;
        }
    }

    function parseRaisesException(exception) {
        return function (code) {
            try {
                Function(code);
                return false;
            } catch (actual) {
                return exception.prototype.isPrototypeOf(actual);
            }
        };
    }

    function completesNormally(code) {
        try {
            eval(code);
            return true;
        } catch (_error) {
            return false;
        }
    }

    function raisesException(exception) {
        return function (code) {
            try {
                eval(code);
                return false;
            } catch (actual) {
                return actual instanceof exception;
            }
        };
    }

    function testLenientAndStrict(code, lenient_pred, strict_pred) {
        return strict_pred('"use strict"; ' + code) && lenient_pred(code);
    }

    var evalDelete = (function () {
        var outerVar = eval("var x; (function() { return delete x; })");
        var outerFunction = eval("function x() {} (function() { return delete x; })");
        var argumentShadow = eval("var x; (function(x) { return delete x; })");
        var functionLocal = eval("(function() { var x; return delete x; })");
        return [outerVar(), outerVar(), outerFunction(), outerFunction(), argumentShadow(), functionLocal()].join('|');
    })();

    return [
        testLenientAndStrict('arguments++', parsesSuccessfully, parseRaisesException(SyntaxError)),
        testLenientAndStrict('arguments--', parsesSuccessfully, parseRaisesException(SyntaxError)),
        testLenientAndStrict('++arguments', parsesSuccessfully, parseRaisesException(SyntaxError)),
        testLenientAndStrict('--arguments', parsesSuccessfully, parseRaisesException(SyntaxError)),
        testLenientAndStrict('delete x;', parsesSuccessfully, parseRaisesException(SyntaxError)),
        testLenientAndStrict('delete (x);', parsesSuccessfully, parseRaisesException(SyntaxError)),
        testLenientAndStrict('with (1) {}', completesNormally, raisesException(SyntaxError)),
        parsesSuccessfully('function f() { "use strict"; }; with (1) {}'),
        evalDelete
    ].join('|');
})();