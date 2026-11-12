// Stage js-c78c's probe on branch js-c78: the script paths that reach the sites the owner's ruling of 2026-11-11, "Fix all
// + enforce", has fixed (array and property keys as text, JSON's keys and source text, RegExp's group names, flags and
// escapes, template spans, private names, BigInt from a String with a sign, string case), each line in its own try so that
// a refusal is that line's answer. The CLR bridge's sites have no path from a script in the shell; the tests read them.
function line(label, f) { var r; try { r = String(f()); } catch (e) { r = "throws " + (e && e.name) + ": " + (e && e.message); } console.log(label + " => " + r); }
line("JSON.stringify([1,[2,3],{a:-1.5,I:2}])", function () { return JSON.stringify([1, [2, 3], { a: -1.5, I: 2 }]); });
line("JSON.stringify([7,8], replacer of key)", function () { return JSON.stringify([7, 8], function (k, v) { return typeof v === "number" ? k + ":" + v : v; }); });
line("JSON.stringify({b:[1,2]}, null, 1)", function () { return JSON.stringify({ b: [1, 2] }, null, 1); });
line("Object.keys(JSON.parse('{\"1\":1,\"-1\":2,\"01\":3,\"4294967295\":4,\"I\":5}'))", function () { return JSON.stringify(Object.keys(JSON.parse('{"1":1,"-1":2,"01":3,"4294967295":4,"I":5}'))); });
line("JSON.parse('[10,[20]]', reviver of key)", function () { var ks = []; JSON.parse('[10,[20]]', function (k, v) { ks.push(k); return v; }); return ks.join("|"); });
line("JSON.parse('[1.5,-2]', reviver of source)", function () { var ss = []; JSON.parse('[1.5,-2]', function (k, v, c) { ss.push(c && c.source); return v; }); return ss.join("|"); });
line("/(?<I>a)(?<i>b)/.exec('ab').groups", function () { var g = /(?<I>a)(?<i>b)/.exec('ab').groups; return g.I + g.i; });
line("'aIb'.replace(/(?<nI>I)/, '[$<nI>]')", function () { return 'aIb'.replace(/(?<nI>I)/, '[$<nI>]'); });
line("[...'aIaI'.matchAll(/(?<x>I)/g)].map(m => m.index)", function () { return [...'aIaI'.matchAll(/(?<x>I)/g)].map(function (m) { return m.index; }).join(); });
line("new RegExp('I', 'gimsuy').flags", function () { return new RegExp('I', 'gimsuy').flags; });
line("/I/i.test('i'), /i/i.test('I')", function () { return /I/i.test('i') + "," + /i/i.test('I'); });
line("/I/i.test('\\u0131'), /i/i.test('\\u0130')", function () { return /I/i.test('ı') + "," + /i/i.test('İ'); });
line("/(?i:I)b/.test('ib')", function () { return /(?i:I)b/.test('ib'); });
line("RegExp.escape('I-1.5 \\u0130')", function () { return RegExp.escape('I-1.5 İ'); });
line("new RegExp('\\n\\u2028/').source", function () { return new RegExp('\n /').source; });
line("/a{0,2}/.exec('aa')[0], /a{00,1}/.exec('aa')[0]", function () { return /a{0,2}/.exec('aa')[0] + "," + /a{00,1}/.exec('aa')[0]; });
line("/(a)|(b)/.exec('b').length", function () { return /(a)|(b)/.exec('b').length; });
line("'I'.toLowerCase(), 'i'.toUpperCase()", function () { return 'I'.toLowerCase() + "," + 'i'.toUpperCase(); });
line("'I'.toLocaleLowerCase(), 'i'.toLocaleUpperCase()", function () { return 'I'.toLocaleLowerCase() + "," + 'i'.toLocaleUpperCase(); });
line("'I'.toLocaleLowerCase('tr'), 'i'.toLocaleUpperCase('tr')", function () { return 'I'.toLocaleLowerCase('tr') + "," + 'i'.toLocaleUpperCase('tr'); });
line("BigInt('-5'), BigInt('+5'), BigInt(' 7 ')", function () { return BigInt('-5') + "," + BigInt('+5') + "," + BigInt(' 7 '); });
line("BigInt('\\u22125')", function () { return BigInt('−5'); });
line("clearTimeout('\\u22125'), clearTimeout('-5')", function () { return clearTimeout('−5') + "," + clearTimeout('-5'); });
line("Object.keys('abc'), Object.keys([1,,3])", function () { return JSON.stringify(Object.keys('abc')) + JSON.stringify(Object.keys([1, , 3])); });
line("Object.getOwnPropertyNames(new String('ab'))", function () { return JSON.stringify(Object.getOwnPropertyNames(new String('ab'))); });
line("for (k in [5,6,7]) ...", function () { var ks = []; for (var k in [5, 6, 7]) ks.push(k); return ks.join(); });
line("Reflect.ownKeys(new Proxy([1,2], {}))", function () { return JSON.stringify(Reflect.ownKeys(new Proxy([1, 2], {}))); });
line("Proxy traps' keys", function () { var log = []; var p = new Proxy([5, 6], { get: function (t, k) { log.push("g" + String(k)); return t[k]; }, set: function (t, k, v) { log.push("s" + String(k)); t[k] = v; return true; }, deleteProperty: function (t, k) { log.push("d" + String(k)); return delete t[k]; }, defineProperty: function (t, k, d) { log.push("p" + String(k)); return Reflect.defineProperty(t, k, d); } }); p[1]; p[0] = 9; delete p[1]; Object.defineProperty(p, 3, { value: 1 }); return log.join(); });
line("[1,[2,[3]]].flat(Infinity)", function () { return [1, [2, [3]]].flat(Infinity).join(); });
line("[1,2].flatMap(x => [x, x * 1.5])", function () { return [1, 2].flatMap(function (x) { return [x, x * 1.5]; }).join(); });
line("Intl.getCanonicalLocales({length:2, 0:'en', 1:'DE'})", function () { return Intl.getCanonicalLocales({ length: 2, 0: 'en', 1: 'DE' }).join(); });
line("tag`a${1}b${2}c`.raw", function () { return (function (s) { return s.raw.join('|'); })`a${1}b${2}c`; });
line("`x${1.5}y${-2}`", function () { return `x${1.5}y${-2}`; });
line("class C { #I = 1; ... }", function () { class C { #I = 1; #i() { return 2; } static g(o) { return o.#I + o.#i(); } } return C.g(new C()); });
line("'#I' in class keys", function () { class C { static ['#I'] = 3; } return Object.keys(C).join(); });
line("(1.5).toString(), String(-1.5), (1e21).toString()", function () { return (1.5).toString() + "," + String(-1.5) + "," + (1e21).toString(); });
line("[-1.5, 2.25].join()", function () { return [-1.5, 2.25].join(); });
