(function () {
    class C {
        *m() { return 42; } static x = 1; static #y = 1;
        static getX() { return this.x; }
        static getY() { return this.#y; }
    }

    var c = new C();
    return [
        c.x === undefined,
        C.x,
        C.getX(),
        C.getY(),
        c.m().next().value
    ].join('|');
})()