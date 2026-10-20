(function (a) {
    Object.defineProperty(arguments, "0", { configurable: false });
    a = 2;
    return [a, arguments[0], Object.getOwnPropertyDescriptor(arguments, "0").value].join("|");
})(1);