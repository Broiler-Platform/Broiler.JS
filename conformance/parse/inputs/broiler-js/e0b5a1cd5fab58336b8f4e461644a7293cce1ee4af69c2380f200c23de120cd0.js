(function () {
    var obj = {
        get "string"() { return 1; },
        set "string"(value) {},
        get 0b10() { return 2; },
        get "\u0073tatic"() { return 3; },
        set "\u0073tatic"(value) {},
        get 1e2() { return 4; }
    };

    var stringDesc = Object.getOwnPropertyDescriptor(obj, "string");
    var numericDesc = Object.getOwnPropertyDescriptor(obj, 2);
    var escapedDesc = Object.getOwnPropertyDescriptor(obj, "static");
    var staticNumericDesc = Object.getOwnPropertyDescriptor(obj, 100);

    return [
        stringDesc.get.name,
        stringDesc.set.name,
        numericDesc === undefined ? "missing" : numericDesc.get.name,
        escapedDesc.get.name,
        escapedDesc.set.name,
        staticNumericDesc === undefined ? "missing" : staticNumericDesc.get.name
    ].join('|');
})()