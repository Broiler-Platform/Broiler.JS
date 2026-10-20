(function () {
    function thrownName(callback) {
        try {
            callback();
            return 'no-throw';
        } catch (e) {
            return e.constructor.name;
        }
    }

    var assignmentCount = 0;
    var assignmentScope = { x: 1 };
    with (assignmentScope) {
        var assignmentError = thrownName(function () {
            'use strict';
            assignmentCount++;
            x = (delete assignmentScope.x, 2);
            assignmentCount++;
        });
    }

    var compoundCount = 0;
    var compoundScope = {
        get x() {
            delete this.x;
            return 2;
        }
    };
    with (compoundScope) {
        var compoundError = thrownName(function () {
            'use strict';
            compoundCount++;
            x ^= 3;
            compoundCount++;
        });
    }

    var updateCount = 0;
    var updateScope = {
        get x() {
            delete this.x;
            return 2;
        }
    };
    with (updateScope) {
        var updateError = thrownName(function () {
            'use strict';
            updateCount++;
            x++;
            updateCount++;
        });
    }

    return [
        assignmentError, assignmentCount, 'x' in assignmentScope,
        compoundError, compoundCount, 'x' in compoundScope,
        updateError, updateCount, 'x' in updateScope
    ].join('|');
})()