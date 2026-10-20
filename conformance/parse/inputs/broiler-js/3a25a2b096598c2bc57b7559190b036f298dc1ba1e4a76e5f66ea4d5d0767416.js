(function () {
    class Base {
        constructor() {
            this.value = 1;
        }
    }

    class Derived extends Base {
        constructor() {
            super();
        }
    }

    return new Derived().value;
})()