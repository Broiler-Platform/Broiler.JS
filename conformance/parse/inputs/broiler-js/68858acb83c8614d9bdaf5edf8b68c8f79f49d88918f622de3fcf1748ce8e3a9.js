(function () {
    var outerLet = eval("let x; (function() { return delete x; })");
    var outerVarInForLet = eval("for (let q = 0; q < 1; q++) { var x; } (function() { return delete x; })");
    return [outerLet(), outerVarInForLet(), outerVarInForLet()].join('|');
})();