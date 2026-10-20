(function () {
    var x = 0;
    var scope = {
        get x() {
            delete this.x;
            return 2;
        }
    };

    var postfix;
    var prefix;
    with (scope) {
        postfix = x--;
        prefix = --x;
    }

    return [postfix, prefix, scope.x, x].join('|');
})()