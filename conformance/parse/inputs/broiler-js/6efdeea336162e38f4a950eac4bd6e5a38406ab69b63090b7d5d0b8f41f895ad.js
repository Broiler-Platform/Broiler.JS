(function () {
    class C {
        method(a,) {}
        methodDefault(a = 1,) {}
        *gen(a,) {}
        *genDefault(a = 1,) {}
        async asyncMethod(a,) {}
        async asyncMethodDefault(a = 1,) {}
        async *asyncGen(a,) {}
        async *asyncGenDefault(a = 1,) {}
    }

    return [
        (function (a,) {}).length,
        (function (a = 1,) {}).length,
        (function (...rest) {}).length,
        (async function (a,) {}).length,
        (async function (a = 1,) {}).length,
        (function* (a,) {}).length,
        (function* (a = 1,) {}).length,
        (async function* (a,) {}).length,
        (async function* (a = 1,) {}).length,
        ((a,) => {}).length,
        ((a = 1,) => {}).length,
        C.prototype.method.length,
        C.prototype.methodDefault.length,
        C.prototype.gen.length,
        C.prototype.genDefault.length,
        C.prototype.asyncMethod.length,
        C.prototype.asyncMethodDefault.length,
        C.prototype.asyncGen.length,
        C.prototype.asyncGenDefault.length
    ].join('|');
})()