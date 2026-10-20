(function () {
    var instanceDefaultSet = '';
    var instanceNumericSet = '';
    var staticUnicodeSet = '';
    var staticNumericSet = '';

    class C {
        get 'def\u{61}ult'() { return 'get instance default'; }
        set 'def\u{61}ult'(value) { instanceDefaultSet = value; }
        get 0.0000001() { return 'get instance numeric'; }
        set 0.0000001(value) { instanceNumericSet = value; }
        static get 'unicod\u{000065}Escape'() { return 'get static unicode'; }
        static set 'unicod\u{000065}Escape'(value) { staticUnicodeSet = value; }
        static get 0.0000001() { return 'get static numeric'; }
        static set 0.0000001(value) { staticNumericSet = value; }
    }

    C.prototype['default'] = 'set instance default';
    C.prototype['1e-7'] = 'set instance numeric';
    C['unicodeEscape'] = 'set static unicode';
    C['1e-7'] = 'set static numeric';

    return [
        C.prototype['default'],
        instanceDefaultSet,
        C.prototype['1e-7'],
        instanceNumericSet,
        C['unicodeEscape'],
        staticUnicodeSet,
        C['1e-7'],
        staticNumericSet
    ].join('|');
})()