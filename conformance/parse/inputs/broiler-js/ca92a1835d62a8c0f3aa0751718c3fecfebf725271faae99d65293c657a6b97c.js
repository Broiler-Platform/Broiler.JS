(function () {
    try {
        (function() { "use strict"; var s = new String("abc"); delete s.length; })();
        return 'no-throw';
    } catch (e) {
        return e instanceof TypeError;
    }
})()