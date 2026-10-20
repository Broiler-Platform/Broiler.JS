(function () {
    var defaultSet = '';
    var unicodeSet = '';
    var numericSet = '';
    var obj = {
        get ['def\u{61}ult']() { return 'get default'; },
        set ['def\u{61}ult'](value) { defaultSet = value; },
        get ['unicod\u{000065}Escape']() { return 'get unicode'; },
        set ['unicod\u{000065}Escape'](value) { unicodeSet = value; },
        get 0.0000001() { return 'get numeric'; },
        set 0.0000001(value) { numericSet = value; }
    };

    obj['default'] = 'set default';
    obj['unicodeEscape'] = 'set unicode';
    obj['1e-7'] = 'set numeric';

    return [
        obj['default'],
        defaultSet,
        obj['unicodeEscape'],
        unicodeSet,
        obj['1e-7'],
        numericSet
    ].join('|');
})()