(function () {
    var postfix = 0;
    var prefix = 0;
    var viaMethod = 0;

    (function () { postfix++; })();
    (function () { ++prefix; })();
    ({ update: function () { viaMethod++; } }).update();

    return [postfix, prefix, viaMethod].join('|');
})();