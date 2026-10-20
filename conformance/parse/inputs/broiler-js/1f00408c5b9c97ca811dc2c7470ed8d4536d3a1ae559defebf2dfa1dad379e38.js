(function () {
    class C {
        get "string"() { return 1; }
        set "string"(value) {}
        get 0b10() { return 2; }
        static get "\u0073tatic"() { return 3; }
        static set "\u0073tatic"(value) {}
        static get 1e2() { return 4; }
    }

    var stringDesc = Object.getOwnPropertyDescriptor(C.prototype, "string");
    var numericDesc = Object.getOwnPropertyDescriptor(C.prototype, 2);
    var staticStringDesc = Object.getOwnPropertyDescriptor(C, "static");
    var staticNumericDesc = Object.getOwnPropertyDescriptor(C, 100);

    return [
        stringDesc.get.name,
        stringDesc.set.name,
        numericDesc === undefined ? "missing" : numericDesc.get.name,
        staticStringDesc.get.name,
        staticStringDesc.set.name,
        staticNumericDesc === undefined ? "missing" : staticNumericDesc.get.name
    ].join('|');
})()