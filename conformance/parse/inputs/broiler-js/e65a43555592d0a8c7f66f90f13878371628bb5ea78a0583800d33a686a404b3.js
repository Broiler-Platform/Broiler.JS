(function () {
    var x = 0;
    var scope = {
        get x() {
            delete this.x;
            return 2;
        }
    };
    var simpleScope = { x: 4 };

    with (scope) {
        x ^= 3;
    }
    with (simpleScope) {
        x = (delete simpleScope.x, 5);
    }

    return [scope.x, simpleScope.x, x].join('|');
})()