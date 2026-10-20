(function () {
    class Base {
        get value() {
            return 41;
        }

        method() {
            return 1;
        }
    }

    class Derived extends Base {
        method() {
            return super.value + super.method();
        }
    }

    return new Derived().method();
})()