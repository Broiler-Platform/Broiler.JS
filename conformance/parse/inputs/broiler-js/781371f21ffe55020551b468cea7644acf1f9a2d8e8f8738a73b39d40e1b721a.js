(function () {
    class C {
        static async #$(value) {
            return await value;
        }
        static async #_(value) {
            return await value + 1;
        }; m() { return 42; }

        static async $(value) {
            return await this.#$(value);
        }
        static async _(value) {
            return await this.#_(value);
        }
    }

    var c = new C();
    return Promise.all([C.$(1), C._(1)]).then(results => {
        return [c.m(), results[0], results[1]].join('|');
    });
})()