(function () {
    class C {
        #method() {}
        *#gen() { yield 1; }
        async #asyncMethod() {}
        async *#asyncGen() { yield 1; }
        static #staticMethod() {}
        static *#staticGen() { yield 1; }
        static async #staticAsyncMethod() {}
        static async *#staticAsyncGen() { yield 1; }

        get method() { return this.#method; }
        get gen() { return this.#gen; }
        get asyncMethod() { return this.#asyncMethod; }
        get asyncGen() { return this.#asyncGen; }
        static get staticMethod() { return this.#staticMethod; }
        static get staticGen() { return this.#staticGen; }
        static get staticAsyncMethod() { return this.#staticAsyncMethod; }
        static get staticAsyncGen() { return this.#staticAsyncGen; }
    }

    var c = new C();
    return [
        c.method.name,
        c.gen.name,
        c.asyncMethod.name,
        c.asyncGen.name,
        C.staticMethod.name,
        C.staticGen.name,
        C.staticAsyncMethod.name,
        C.staticAsyncGen.name
    ].join('|');
})()