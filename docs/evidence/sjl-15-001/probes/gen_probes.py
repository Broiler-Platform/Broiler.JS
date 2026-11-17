#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Write the correspondence probes of SJL-15.1, Broiler.JS's half: probes.tsv and probes.js.

    python3 -I gen_probes.py        (from this folder; two runs write the same bytes)

Each probe is a source text compiled by an indirect eval as global code (prelude.js's tt), its first statement
`__reached = true;`, so that its answer says whether the engine refused it before any of it ran ("THROWS (template)"),
after some of it ran ("THROWS"), or completed (its value, as prelude.js's show renders it). probes.tsv gives each probe
its row of the correspondence (Broiler.VM's docs/evidence/sjl-15-001/correspondence.md, this engine's column in
../correspondence-broiler-js.md), the catalogue row where it has one, the specification's answer (ECMA-262 ES2026 at
0248456c) and why.

Two groups:
- T, timing: for each `semantics` row of the stage table that a Script reaches and whose Broiler.JS site is a compiler
  site, one refused construct, probed at the top level of the eval code, inside a function expression that is never
  called, and inside one that is called. Every one is an early error of the whole eval code, so the specification's
  answer is "THROWS (template) SyntaxError" in each form; an engine that checks the row only when the function's body is
  compiled, or not at all, answers otherwise, and that is the input of the rule's class T (decision D64's second half).
- V, values: the facts a completion value shows (rows FF3, FF4, FF5, RS2, AB1, LB1, PN1, NE1 and the eval-code rows),
  each with the completion value the specification gives.
"""

from __future__ import annotations

import os

HERE = os.path.dirname(os.path.abspath(__file__))
TEMPLATE_SE = "THROWS (template) SyntaxError"

# (catalogue row, correspondence row, construct): a statement list that is an early error wherever it stands in a
# function body or in global code. The constructs are the semantics vectors' own refusals, in a form that can be nested.
TIMING = [
    ("EE0015", "EE1", "({ m() { super(); } });"),
    ("EE0018", "EE1", "({ __proto__: 1, __proto__: 2 });"),
    ("EE0054", "EE1", "for (this in {}) ;"),
    ("EE0058", "LB1", "continue;"),
    ("EE0059", "LB1", "break;"),
    ("EE0074", "EE1", "function f(a = super.x) {}"),
    ("EE0075", "EE1", "function f() { super.x; }"),
    ("EE0076", "EE1", "function f(a = super()) {}"),
    ("EE0077", "EE1", "function f() { super(); }"),
    ("EE0081", "LB1", "L: { (function () { break L; }); }"),
    ("EE0082", "LB1", "L: do { (function () { continue L; }); } while (0);"),
    ("EE0093", "EE1", "({ *g() { super(); } });"),
    ("EE0102", "EE1", "function* g(a = super.x) {}"),
    ("EE0103", "EE1", "function* g() { super.x; }"),
    ("EE0104", "EE1", "function* g(a = super()) {}"),
    ("EE0105", "EE1", "function* g() { super(); }"),
    ("EE0106", "EE1", "({ async *g() { super(); } });"),
    ("EE0117", "EE1", "async function* g(a = super.x) {}"),
    ("EE0118", "EE1", "async function* g() { super.x; }"),
    ("EE0119", "EE1", "async function* g(a = super()) {}"),
    ("EE0120", "EE1", "async function* g() { super(); }"),
    ("EE0121", "EE1", "class C { constructor() { super(); } }"),
    ("EE0124", "EE1", "class C { m() { super(); } }"),
    ("EE0126", "EE1", "class C { static m() { super(); } }"),
    ("EE0130", "EE1", "class C { x = arguments; }"),
    ("EE0131", "EE1", "class C { x = super(); }"),
    ("EE0136", "LB1", "L: { class C { static { break L; } } }"),
    ("EE0137", "LB1", "L: do { class C { static { continue L; } } } while (0);"),
    ("EE0138", "EE1", "class C { static { arguments; } }"),
    ("EE0139", "EE1", "class C { static { super(); } }"),
    ("EE0142", "EE1", "({ async m() { super(); } });"),
    ("EE0150", "EE1", "async function f(a = super.x) {}"),
    ("EE0151", "EE1", "async function f() { super.x; }"),
    ("EE0152", "EE1", "async function f(a = super()) {}"),
    ("EE0153", "EE1", "async function f() { super(); }"),
    ("EE0165", "LB1", "break L;"),
    ("EE0166", "LB1", "L: { continue L; }"),
    ("EE0167", "PN1", "class C { m() { return this.#y; } }"),
]

# Rows whose construct is an early error only at the top level of a Script or eval code: probed there alone.
TOP_ONLY = [
    ("EE0162", "EE1", "super.x;"),
    ("EE0162", "EE1", "() => super.x;"),
    ("EE0167", "PN1", "this.#x;"),
]

# (correspondence row, catalogue row or "-", source, the specification's answer as show() renders it, why).
VALUES = [
    ("FF3", "-", "function f(a) { arguments[0] = 2; return a; } f(1)", "2",
     "a sloppy function with a simple parameter list maps arguments to its parameters (10.4.4.7)"),
    ("FF3", "-", "function f(a) { 'use strict'; arguments[0] = 2; return a; } f(1)", "1",
     "a strict function's arguments object is unmapped (10.4.4.6)"),
    ("FF3", "-", "function f() { return eval('arguments.length'); } f(1, 2)", "2",
     "a direct eval in a function reaches the function's arguments object"),
    ("FF3", "-", "function f() { return (() => arguments[1])(); } f(1, 9)", "9",
     "an arrow has no arguments binding and reads the enclosing function's"),
    ("FF3", "-", "function f() { var e = eval; return e('typeof arguments'); } f(1)", "\"undefined\"",
     "an indirect eval is global code, which has no arguments binding"),
    ("FF3", "-", "function f() { var arguments; return typeof arguments; } f()", "\"object\"",
     "a var named arguments is initialised with the arguments object (FunctionDeclarationInstantiation)"),
    ("FF3", "-", "function f() { function arguments() { return 4; } return arguments(); } f()", "4",
     "a function declaration named arguments makes argumentsObjectNeeded false"),
    ("FF3", "-", "function f() { let arguments = 6; return arguments; } f()", "6",
     "a lexical declaration named arguments makes argumentsObjectNeeded false"),
    ("FF4", "-", "var x = 'outer'; function f(a = () => x) { var x = 'inner'; return a(); } f()", "\"outer\"",
     "parameter expressions give the body a separate variable environment, which a parameter closure does not see"),
    ("FF4", "-", "function f(a, b = function () { return a; }) { var a = 2; return a + ',' + b(); } f(1)", "\"2,1\"",
     "the body's var a starts with the parameter's value and is a separate binding"),
    ("FF4", "-", "function f(a = 1) { var a; return a; } f()", "1",
     "a var of a parameter's name is initialised with the parameter's value"),
    ("FF5", "-", "var x = 'outer'; function f() { eval('var x = \"inner\"'); return x; } f()", "\"inner\"",
     "a sloppy direct eval declares its var in the function's variable environment"),
    ("FF5", "-", "function f() { var x = 1; (0, eval)('var x = 2'); return x; } f()", "1",
     "an indirect eval declares its var in the global environment"),
    ("FF5", "-", "function f() { var x = 1; var e = eval; e('var x = 2'); return x; } f()", "1",
     "a mention of eval that is not a direct call evaluates as global code"),
    ("FF5", "-", "function f() { var x = 1; (() => eval('var x = 2'))(); return x; } f()", "1",
     "a direct eval in an arrow declares its var in the arrow's own variable environment"),
    ("FF5", "-", "function f() { var x = 1; { eval('var x = 2'); } return x; } f()", "2",
     "a direct eval in a block declares its var in the function's variable environment"),
    ("FF5", "-", "function f() { 'use strict'; eval('var y = 1'); return typeof y; } f()", "\"undefined\"",
     "strict eval code has a variable environment of its own"),
    ("RS2", "-", "var x = 'g'; var o = { x: 'o' }; function f() { with (o) { return x; } } f()", "\"o\"",
     "a with statement's object environment is searched first"),
    ("RS2", "-", "var o = { x: 1 }; with (o) { var x = 2; } o.x", "2",
     "a var initialiser in a with body assigns through the object environment"),
    ("RS3", "-", "typeof undeclaredName", "\"undefined\"", "an unresolvable reference under typeof is undefined"),
    ("RS3", "-", "undeclaredName", "THROWS ReferenceError", "an unresolvable reference throws when read (GetValue)"),
    ("RS4", "-", "const c = 1; var r; try { c = 2; } catch (e) { r = e.name; } r", "\"TypeError\"",
     "assignment to a const binding is a run-time TypeError (SetMutableBinding), not an early error"),
    ("AB1", "-", "{ function g() { return 1; } } g()", "1",
     "B.3.2.2: a block function in global code is also var-scoped (Annex B)"),
    ("AB1", "-", "function f() { { function g() { return 5; } } return g(); } f()", "5",
     "B.3.2.1: a block function in a function body is also var-scoped (Annex B)"),
    ("AB1", "-", "function f() { var t = g; { function g() {} } return t; } f()", "undefined",
     "B.3.2.1: the var binding exists, undefined, before the block (Annex B)"),
    ("AB1", "-", "eval('{ function g() { return 4; } }'); g()", "4",
     "B.3.2.3: a block function in eval code is also var-scoped (Annex B)"),
    ("AB1", "-", "function f(g) { { function g() { return 3; } } return typeof g; } f(1)", "\"number\"",
     "B.3.2.1: no alias for a parameter's name"),
    ("AB1", "-", "function f() { { function arguments() {} } return typeof arguments; } f()", "\"function\"",
     "B.3.2.1: no var binding for arguments, but the declaration's evaluation sets the arguments binding"),
    ("AB1", "-", "function f() { { let g = 1; { function g() {} } } return typeof g; } f()", "\"undefined\"",
     "B.3.2.1: no alias where the replacing var would conflict with a lexical declaration it hoists through"),
    ("AB1", "-", "'use strict'; { function g() {} } typeof g", "\"undefined\"",
     "no Annex B alias in strict code"),
    ("LB1", "EE0166", "var n = 0; A: B: for (var i = 0; i < 2; i++) { n++; continue A; } n", "2",
     "continue may name any label of an iteration statement's label set"),
    ("LB1", "EE0082", "function f() { var n = 0; A: B: for (var i = 0; i < 3; i++) { n++; continue A; } return n; } f()", "3",
     "the same in a function body"),
    ("LB1", "EE0166", "var s = ''; o: for (var i = 0; i < 2; i++) { for (var j = 0; j < 2; j++) { if (j) continue o; s += i + '' + j; } } s",
     "\"0010\"", "continue to an outer loop's label"),
    ("PN1", "EA0009", "class C { #x = 5; m() { return eval('this.#x'); } } new C().m()", "5",
     "eval code in a method sees the class's private names"),
    ("PN1", "EA0009", "class C { #x = 7; m() { return (() => eval('this.#x'))(); } } new C().m()", "7",
     "the same through an arrow"),
    ("PN1", "EA0009", "class C { m() { return eval('this.#y'); } } new C().m()", "THROWS SyntaxError",
     "an undeclared private name in eval code is an early error of the eval code (PerformEval step 7)"),
    ("PN1", "EA0009", "var r = ''; class C { m() { try { eval(\"r += 'a'; this.#y\"); } catch (e) { r += e.name; } return r; } } new C().m()",
     "\"SyntaxError\"", "the eval code is refused before any of it runs"),
    ("PN1", "EA0009", "var r = ''; try { eval(\"r += 'a'; this.#x\"); } catch (e) { r += e.name; } r", "\"SyntaxError\"",
     "eval code outside any class: the private name is undeclared, refused before any of it runs"),
    ("NE1", "-", "var f = function () {}; f.name", "\"f\"", "NamedEvaluation of an anonymous function in a var initialiser"),
    ("NE1", "-", "var o = {}; o.q = function () {}; o.q.name", "\"\"", "no NamedEvaluation for a property assignment"),
    ("NE1", "-", "var a; a ??= function () {}; a.name", "\"a\"", "NamedEvaluation in a logical assignment"),
    ("NE1", "-", "var x = (function () {}); x.name", "\"x\"", "NamedEvaluation through a parenthesised expression"),
    ("NE1", "-", "var y = (0, function () {}); y.name", "\"\"", "a comma expression is no anonymous function definition"),
    ("NE1", "-", "class C { static #m = function () {}; static n() { return C.#m.name; } } C.n()", "\"#m\"",
     "NamedEvaluation for a private field's initialiser"),
    ("NE1", "-", "var [k = function () {}] = []; k.name", "\"k\"", "NamedEvaluation for a binding element's initialiser"),
    ("EA0006", "EA0006", "let x; eval('var x;')", "THROWS SyntaxError",
     "EvalDeclarationInstantiation: a var of eval code against a global lexical declaration"),
    ("EA0007", "EA0007", "function f() { let x; { eval('var x;'); } } f()", "THROWS SyntaxError",
     "EvalDeclarationInstantiation: a var of eval code against a lexical declaration it hoists past"),
    ("EA0007", "EA0007", "var r; try { throw 1; } catch (x) { eval('var x = 2;'); r = x; } r", "2",
     "B.3.4: a var of eval code may name a catch parameter (Annex B)"),
    ("EE1", "EA0003", "({ m() { return eval('super.constructor') === Object; } }).m()", "true",
     "eval code in a method may hold a super property (PerformEval step 11.f)"),
    ("EE1", "EA0003", "function f() { return eval('super.x'); } f()", "THROWS SyntaxError",
     "eval code outside a method may not (step 11.f)"),
    ("EE1", "EA0004", "class B { constructor() { this.v = 7; } } class C extends B { constructor() { eval('super()'); } } new C().v", "7",
     "eval code in a derived constructor may hold a super call (step 11.g)"),
    ("EE1", "EA0004", "({ m() { eval('super()'); } }).m()", "THROWS SyntaxError", "eval code in a method may not (step 11.g)"),
    ("EE1", "EA0005", "class C { x = eval('arguments'); } new C()", "THROWS SyntaxError",
     "eval code in a field initialiser may not hold arguments (step 11.h)"),
    ("EE1", "EA0005", "class C { x = eval('(function () { return arguments.length; })(1, 2, 3)'); } new C().x", "3",
     "ContainsArguments does not look into a function the eval code holds"),
]


def js_string(text: str) -> str:
    return '"' + text.replace("\\", "\\\\").replace('"', '\\"') + '"'


def probes():
    out = []
    n = 0
    for row, corr, construct in TIMING:
        for form, src, where in (
                ("top", "__reached = true; " + construct, "at the eval code's top level"),
                ("nested", "__reached = true; (function () { " + construct + " });", "in a function never called"),
                ("called", "__reached = true; (function () { " + construct + " })();", "in a function called")):
            n += 1
            out.append((f"T{n:03d}-{row}-{form}", "T", corr, row, src, TEMPLATE_SE,
                        f"an early error of the whole eval code, {where}"))
    for row, corr, construct in TOP_ONLY:
        n += 1
        out.append((f"T{n:03d}-{row}-top", "T", corr, row, "__reached = true; " + construct, TEMPLATE_SE,
                    "an early error of the whole eval code, at its top level"))
    for i, (corr, row, source, answer, why) in enumerate(VALUES, 1):
        # A directive stays the first statement, so that it is still one.
        directive = "'use strict'; " if source.startswith("'use strict'; ") else ""
        src = directive + "__reached = true; " + source[len(directive):]
        out.append((f"V{i:03d}-{corr}", "V", corr, row, src, answer, why))
    return out


def main() -> int:
    rows = probes()
    with open(os.path.join(HERE, "probes.tsv"), "w", encoding="utf-8", newline="\n") as fh:
        fh.write("id\tgroup\tcorrespondence\tcatalogue\tprogram\tspecification\twhy\n")
        for r in rows:
            fh.write("\t".join(r) + "\n")
    with open(os.path.join(HERE, "probes.js"), "w", encoding="utf-8", newline="\n") as fh:
        fh.write("// SJL-15.1 (Broiler.JS's half): the correspondence probes, generated by gen_probes.py. Run as one\n"
                 "// program, prelude.js + <this file> + epilogue.js. Each line prints `<id> => <value>` or\n"
                 "// `<id> => THROWS [(template)] <name>: <message>`.\n")
        for r in rows:
            fh.write(f"tt({js_string(r[0])}, {js_string(r[4])});\n")
    print(f"{len(rows)} probes")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
