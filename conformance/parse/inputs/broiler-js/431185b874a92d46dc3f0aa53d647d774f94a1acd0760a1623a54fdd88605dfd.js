(function () {
    var arrayName = (([cls = class {}] = []) => cls.name)();
    var objectName = (({ cls = class {} } = {}) => cls.name)();
    return arrayName + '|' + objectName;
})()