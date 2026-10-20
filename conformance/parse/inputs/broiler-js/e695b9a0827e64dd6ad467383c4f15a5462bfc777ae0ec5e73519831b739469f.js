(function () {
    var calls = 0;
    function rhs() {
        calls++;
        return 'rhs';
    }

    var falsy = 0;
    var truthy = 'left';
    var empty = null;

    var andResult = (falsy &&= rhs());
    var orResult = (truthy ||= rhs());
    var coalesceResult = (empty ??= rhs());

    return [andResult, falsy, orResult, truthy, coalesceResult, empty, calls].join('|');
})()