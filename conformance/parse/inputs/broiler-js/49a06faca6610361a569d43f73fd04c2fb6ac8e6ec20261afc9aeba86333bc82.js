(function () {
    var log = [];
    function source() {
        log.push("source");
        var iterator = {
            next: function () {
                log.push("iterator-step");
                return {
                    get done() {
                        log.push("iterator-done");
                        return true;
                    },
                    get value() {
                        log.push("iterator-value");
                    }
                };
            }
        };
        var source = {};
        source[Symbol.iterator] = function () {
            log.push("iterator");
            return iterator;
        };
        return source;
    }
    function target() {
        log.push("target");
        return {
            set q(v) {
                log.push("set");
            }
        };
    }
    function targetKey() {
        log.push("target-key");
        return {
            toString: function () {
                log.push("target-key-tostring");
                return "q";
            }
        };
    }

    ([target()[targetKey()]] = source());
    return log.join('|');
})()