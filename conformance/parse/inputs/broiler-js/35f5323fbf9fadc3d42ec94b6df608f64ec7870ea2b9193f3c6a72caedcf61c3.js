(function (a) {
    Object.defineProperty(arguments, "0", { configurable: false });
    a = 2;
    Object.defineProperty(arguments, "0", { writable: false });
    var before = [a, arguments[0], Object.getOwnPropertyDescriptor(arguments, "0").value].join("|");
    a = 3;
    return before + "|" + [a, arguments[0], Object.getOwnPropertyDescriptor(arguments, "0").value].join("|");
})(1);