(function () {
    var ev = eval;
    var deletable = ev("var y = 5; (function () { return delete y; })")();
    var strictDelete;
    try {
        eval('"use strict"; var o = Object.freeze({ noconfig: 1 }); delete o.noconfig;');
        strictDelete = 'no-throw';
    } catch (e) {
        strictDelete = e.name;
    }
    var sloppyDelete = eval('var o = Object.freeze({ noconfig: 1 }); delete o.noconfig;');
    return [deletable, sloppyDelete, strictDelete].join('|');
})();