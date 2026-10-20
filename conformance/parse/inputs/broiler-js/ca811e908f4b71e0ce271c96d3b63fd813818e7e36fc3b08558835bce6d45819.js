(function () {
    var yield = 'y';
    var yieldSet = '';
    var obj = {
        get [yield]() { return 'get yield'; },
        set [yield](param) { yieldSet = param; }
    };

    var getterValue = obj.y;
    obj.y = 'set yield';

    return getterValue + '|' + yieldSet;
})()