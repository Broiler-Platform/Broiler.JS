// Stage js-arch: the script paths that reach the interpolations the host-culture readers' architecture test finds at
// origin/main (bundle sjl-9-001, 2026-11-12): each line's text, or the error it throws, under the host culture the run sets.
function line(label, f) { var r; try { r = String(f()); } catch (e) { r = "throws " + (e && e.name) + ": " + (e && e.message); } console.log(label + " => " + r); }
line("eval('if (')", function () { return eval("if ("); });
line("eval('var x = {a: 1;')", function () { return eval("var x = {a: 1;"); });
line("eval('for (;;')", function () { return eval("for (;;"); });
line("new Uint16Array(new ArrayBuffer(8), 1)", function () { return new Uint16Array(new ArrayBuffer(8), 1); });
line("new Uint32Array(new ArrayBuffer(6))", function () { return new Uint32Array(new ArrayBuffer(6)); });
line("/(a)(b)/.exec('ab'); RegExp.$1 + RegExp.$2", function () { /(a)(b)/.exec("ab"); return RegExp.$1 + RegExp.$2; });
line("duplicate named groups", function () { return JSON.stringify(/(?<a>x)|(?<a>y)\k<a>/.exec("yy")); });
line("non-configurable element pop", function () { "use strict"; var a = [1, 2]; Object.defineProperty(a, 1, { value: 2, configurable: false, writable: true }); return a.pop(); });
line("non-configurable element splice", function () { "use strict"; var a = [1, 2, 3]; Object.defineProperty(a, 0, { value: 1, configurable: false, writable: true }); return a.splice(0, 1); });
line("generator prototype", function () { function* g() { yield 1; } return Object.prototype.toString.call(g()) + " " + g().next().value; });
line("class private and computed members", function () { var k = "m"; class A { #x = -1; static #y = -2; [k]() { return this.#x; } static get y() { return A.#y; } get #z() { return -3; } z() { return this.#z; } } return new A().m() + "," + A.y + "," + new A().z(); });
line("switch default", function () { var r = []; for (var i = -1; i < 2; i++) { switch (i) { case 0: r.push("zero"); break; default: r.push("d" + i); } } return r.join(); });
line("stack lines", function () { function f() { return new Error("e").stack; } return String(f()).split("\n").length > 0; });
line("negative numbers in text", function () { return [-1, -2.5, String(-3), (-4).toString(), `${-5}`, -6 + ""].join(); });
line("BigInt and Decimal-free fixed text", function () { return (-7n).toString() + "," + (1.25).toFixed(1) + "," + (-1.25).toFixed(1); });
