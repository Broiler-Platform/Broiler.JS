(function () {
    class C {
        #m() { return 'test262'; }

        method() {
            return eval('this.#m()');
        }
    }

    return new C().method();
})()