(function () {
    var arrayName = (([arrow = () => {}] = []) => arrow.name)();
    var objectName = (({ arrow = () => {} } = {}) => arrow.name)();
    return arrayName + '|' + objectName;
})()