(function (a) {
    Object.defineProperty(arguments, "0", { configurable: false });
    var deleted = delete arguments[0];
    a = 2;
    return [deleted, a, arguments[0], Object.getOwnPropertyDescriptor(arguments, "0").value].join("|");
})(1);