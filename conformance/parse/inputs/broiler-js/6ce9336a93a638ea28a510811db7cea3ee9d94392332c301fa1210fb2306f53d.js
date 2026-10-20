(function () {
    var obj = { value: "myObj_value" };
    var result = "result";

    try {
        with (obj) {
            var value = "value";
            throw value;
        }
    } catch (e) {
        result = e;
    }

    return [result, obj.value].join("|");
})();