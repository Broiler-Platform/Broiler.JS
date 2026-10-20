(function () {
    var evalAlias = eval;
    evalAlias('(() => super())();');
})();