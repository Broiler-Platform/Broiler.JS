(function () {
    class C {
        static async *#gen() { yield 1; }
        static get gen() { return this.#gen; }
    }

    var iter = C.gen();
    return [
        Object.prototype.hasOwnProperty.call(C, '#gen'),
        Object.prototype.hasOwnProperty.call(C.prototype, '#gen'),
        Object.getOwnPropertyNames(C).includes('#gen'),
        Object.getOwnPropertyDescriptor(C, '#gen') === undefined,
        typeof iter.next
    ].join('|');
})()