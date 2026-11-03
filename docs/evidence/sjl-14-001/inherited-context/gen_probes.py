#!/usr/bin/env python3
# SJL-14.1 task 1.1 (decision D44): generates the inherited-context probes.
#
# Writes, beside this file:
#   ic-values.js - one program body (prelude.js + ic-values.js + epilogue.js is the program each engine runs);
#   probes.tsv   - one line per probe: id, context, caller, construct, eval text, the specification's answer, why.
#
# Every probe compiles its context by an indirect eval of a template, so that a template one engine refuses stops
# only that probe; the template sets __reached just before its direct eval, so that a refusal of the template itself
# reads "THROWS (template) ..." and a refusal of the evaluated text reads "THROWS ...". The direct eval's caller is the
# template's code, whose context (function, method, derived constructor, field initialiser, static block, class body)
# is what the engine passes to the parse of the evaluated text.
import json, os

HERE = os.path.dirname(os.path.abspath(__file__))

# The constructs: the evaluated text, and the specification's answer as a function of the context's facts.
#   f = inFunction, m = inMethod, d = inDerivedConstructor, i = inClassFieldInitializer (PerformEval, ES2026 19.2.1.1),
#   s = the caller is strict, p = a private name #p is visible (the context is inside a class that declares it),
#   nt = what typeof new.target answers where it is admitted, pv = what `#p in this` answers where it is admitted,
#   ar = what typeof arguments answers where it is admitted.
S = 'THROWS SyntaxError'
CONSTRUCTS = [
    ('NT', 'typeof new.target', lambda c: c['nt'] if c['f'] else S, 'new.target (inFunction)'),
    ('SP', 'typeof super.toString', lambda c: '"function"' if c['m'] else S, 'a super property (inMethod)'),
    ('SC', 'super()', lambda c: ('THROWS ReferenceError' if c['d'] else S), 'a super call (inDerivedConstructor); the template has called super() already, so an admitted call throws ReferenceError'),
    ('AR', 'typeof arguments', lambda c: S if c['i'] else c['ar'], 'arguments (inClassFieldInitializer refuses it)'),
    ('PD', '#p in this', lambda c: c['pv'] if c['p'] else S, 'a private name the enclosing class declares'),
    ('PU', '#q in this', lambda c: S, 'a private name no enclosing class declares'),
    ('ST', 'var public = 1; typeof public', lambda c: S if c['s'] else '"number"', 'a strict reserved word (the caller\'s strictness)'),
]

# The contexts: id, description, caller strictness label, template (E marks the direct eval's argument), facts.
U, F_, O = '"undefined"', '"function"', '"object"'
def ctx(f=False, m=False, d=False, i=False, s=False, p=False, nt=U, pv='-', ar=O):
    return dict(f=f, m=m, d=d, i=i, s=s, p=p, nt=nt, pv=pv, ar=ar)

CONTEXTS = [
    ('G', 'global code', 'sloppy', '__reached = true; eval(E)', ctx(ar=U), 'global'),
    ('G', 'global code', 'strict', "'use strict'; __reached = true; eval(E)", ctx(s=True, ar=U), 'global'),
    ('F', 'a function called', 'sloppy', '(function () { __reached = true; return eval(E); })()', ctx(f=True), 'expr'),
    ('F', 'a function called', 'strict', "(function () { 'use strict'; __reached = true; return eval(E); })()", ctx(f=True, s=True), 'expr'),
    ('FN', 'a function constructed with new', 'sloppy', 'new (function F() { __reached = true; this.r = eval(E); })().r', ctx(f=True, nt=F_), 'expr'),
    ('FN', 'a function constructed with new', 'strict', "new (function F() { 'use strict'; __reached = true; this.r = eval(E); })().r", ctx(f=True, s=True, nt=F_), 'expr'),
    ('AF', 'an arrow in a function', 'sloppy', '(function () { return (() => { __reached = true; return eval(E); })(); })()', ctx(f=True), 'expr'),
    ('AF', 'an arrow in a function', 'strict', "(function () { 'use strict'; return (() => { __reached = true; return eval(E); })(); })()", ctx(f=True, s=True), 'expr'),
    ('OM', 'an object literal method', 'sloppy', '({ m() { __reached = true; return eval(E); } }).m()', ctx(f=True, m=True), 'expr'),
    ('OM', 'an object literal method', 'strict', "(function () { 'use strict'; return ({ m() { __reached = true; return eval(E); } }).m(); })()", ctx(f=True, m=True, s=True), 'expr'),
    ('OG', 'an object literal getter', 'sloppy', '({ get g() { __reached = true; return eval(E); } }).g', ctx(f=True, m=True), 'expr'),
    ('CM', 'a class method', 'strict (class code)', 'new (class { #p = 7; m() { __reached = true; return eval(E); } })().m()', ctx(f=True, m=True, s=True, p=True, pv='true'), 'expr'),
    ('CS', 'a static class method', 'strict (class code)', '(class { #p = 7; static m() { __reached = true; return eval(E); } }).m()', ctx(f=True, m=True, s=True, p=True, pv='false'), 'expr'),
    ('BC', "a base class's constructor", 'strict (class code)', 'new (class { #p = 7; constructor() { __reached = true; this.r = eval(E); } })().r', ctx(f=True, m=True, s=True, p=True, pv='true', nt=F_), 'expr'),
    ('DC', "a derived class's constructor, after super()", 'strict (class code)', '(function () { class B {} class D extends B { #p = 7; constructor() { super(); __reached = true; this.r = eval(E); } } return new D().r; })()', ctx(f=True, m=True, d=True, s=True, p=True, pv='true', nt=F_), 'expr'),
    ('AD', "an arrow in a derived class's constructor, after super()", 'strict (class code)', '(function () { class B {} class D extends B { #p = 7; constructor() { super(); this.r = (() => { __reached = true; return eval(E); })(); } } return new D().r; })()', ctx(f=True, m=True, d=True, s=True, p=True, pv='true', nt=F_), 'expr'),
    ('FI', "a base class's field initialiser", 'strict (class code)', 'new (class { #p = 7; x = (__reached = true, eval(E)); })().x', ctx(f=True, m=True, i=True, s=True, p=True, pv='true'), 'expr'),
    ('FD', "a derived class's field initialiser", 'strict (class code)', '(function () { class B {} class D extends B { #p = 7; x = (__reached = true, eval(E)); } return new D().x; })()', ctx(f=True, m=True, i=True, s=True, p=True, pv='true'), 'expr'),
    ('AI', 'an arrow in a field initialiser', 'strict (class code)', 'new (class { #p = 7; x = (() => { __reached = true; return eval(E); })(); })().x', ctx(f=True, m=True, i=True, s=True, p=True, pv='true'), 'expr'),
    ('SF', 'a static field initialiser', 'strict (class code)', '(class { #p = 7; static x = (__reached = true, eval(E)); }).x', ctx(f=True, m=True, i=True, s=True, p=True, pv='false'), 'expr'),
    ('SB', 'a class static block', 'strict (class code)', '(function () { var r; class C { #p = 7; static { __reached = true; r = eval(E); } } return r; })()', ctx(f=True, m=True, s=True, p=True, pv='false'), 'expr'),
    ('CK', "a computed key in a class body, in a sloppy function's call", 'strict (class code)', "(function () { var r; class C { #p = 7; [(__reached = true, r = eval(E), 'k')]() {} } return r; })()", ctx(f=True, s=True, p=True, pv='false'), 'expr'),
]

# Further probes of the same family: nesting, indirect eval, positions (N25) and a hashbang (N26).
EXTRA = [
    ('X01', 'a function called', 'sloppy', 'nested direct eval of new.target', '(function () { __reached = true; return eval(E); })()', "eval('typeof new.target')", U, 'the inner eval inherits the outer eval code\'s context'),
    ('X02', 'a function called', 'sloppy', 'indirect eval of new.target', '(function () { __reached = true; return (0, eval)(@E@); })()', 'typeof new.target', S, 'an indirect eval is global code'),
    ('X03', 'a function called', 'strict', 'a refusal on the text\'s third line (positions)', "(function () { 'use strict'; __reached = true; return eval(E); })()", '1;\n2;\n  @', S, 'the message, position included, is recorded (N25)'),
    ('X04', 'a function called', 'sloppy', 'a refusal on the text\'s third line (positions)', '(function () { __reached = true; return eval(E); })()', '1;\n2;\n  @', S, 'the sloppy control of X03'),
    ('X05', 'a function called', 'strict', 'a hashbang at the start of the text', "(function () { 'use strict'; __reached = true; return eval(E); })()", '#!x\n1', '1', 'eval code is a Script, which may begin with a hashbang (N26)'),
    ('X06', 'a function called', 'sloppy', 'a hashbang at the start of the text', '(function () { __reached = true; return eval(E); })()', '#!x\n1', '1', 'the sloppy control of X05'),
    ('X07', 'a function called', 'strict', 'a class element named static', "(function () { 'use strict'; __reached = true; return eval(E); })()", 'class C { static = 1 }; new C().static', '1', 'a field may be named static in strict code (R36)'),
    ('X08', 'a function called', 'strict', 'legacy octal 010', "(function () { 'use strict'; __reached = true; return eval(E); })()", '010', S, 'strict code refuses a legacy octal literal (T16, K1)'),
    ('X09', 'a function called', 'strict', 'non-octal decimal 08', "(function () { 'use strict'; __reached = true; return eval(E); })()", '08', S, 'strict code refuses a NonOctalDecimalIntegerLiteral (K1)'),
    ('X10', 'a function called', 'strict', 'the strict caller\'s own first statement (F24)', "(function () { 'use strict'; __reached = true; return eval(E); })()", '5', '5', 'the completion value of the text\'s first statement'),
    ('X11', 'a static block, nested arrow', 'strict (class code)', 'typeof arguments', '(function () { var r; class C { static { r = (() => { __reached = true; return eval(E); })(); } } return r; })()', 'typeof arguments', O, 'a static block is no field initialiser (F22)'),
    ('X12', "a derived class's field initialiser", 'strict (class code)', 'typeof new.target', '(function () { class B {} class D extends B { x = (__reached = true, eval(E)); } return new D().x; })()', 'typeof new.target', U, 'new.target is undefined in a field initialiser'),
]

# The dynamic function: bad parameters and bodies (CreateDynamicFunction parses each alone, ES2026 20.2.1.1.1).
FUNCTIONS = [
    ('Y01', "Function('a,,b', '')", S, 'an elision in the parameters'),
    ('Y02', "Function('a = super.x', '')", S, 'a super property in the parameters'),
    ('Y03', "Function('...a, b', '')", S, 'a rest parameter not last'),
    ('Y04', "Function('a', 'a', '\"use strict\"')", S, 'duplicate parameters with a strict body'),
    ('Y05', "Function('a = 1', '\"use strict\"')", S, "'use strict' with non-simple parameters"),
    ('Y06', "Function('/*', '*/){')", S, 'parameters that close a comment the body opens'),
    ('Y07', "Function('a', '}); (function(){')", S, 'a body that closes the function early'),
    ('Y08', "Function('', 'super()')", S, 'a super call in the body'),
    ('Y09', "Function('', 'return super.x')", S, 'a super property in the body'),
    ('Y10', "Function('', 'return typeof new.target')()", U, 'new.target in the body'),
    ('Y11', "typeof new (class { #p = 1; m() { return Function('', 'return this.#p'); } })().m()", S, 'a private name of the calling class in the body'),
    ('Y12', "Function('', 'return typeof arguments')()", O, 'arguments in the body'),
    ('Y13', "(function () { 'use strict'; return Function('', 'with ({}) {} return 010')(); })()", '8', 'a strict caller: the body is sloppy'),
    ('Y14', "Function('yield', 'return yield')(5)", '5', 'yield as a parameter name, sloppy'),
    ('Y15', "Object.getPrototypeOf(function* () {}).constructor('yield', '')", S, 'yield as a generator parameter name'),
    ('Y16', "Object.getPrototypeOf(async function () {}).constructor('await', '')", S, 'await as an async function parameter name'),
    ('Y17', "Function('a', 'let a;')", S, 'a lexical declaration of a parameter name'),
    ('Y18', "Function('a', 'b', 'return a + b')(1, 2)", '3', 'the control'),
    ('Y19', "Function('', '}')", S, 'a body that is only a closing brace'),
    ('Y20', "Function(')', '')", S, 'parameters that are only a closing parenthesis'),
    ('Y21', "Function('a', 'a', 'return a')(1, 2)", '2', 'duplicate parameters, sloppy simple list'),
    ('Y22', "Object.getPrototypeOf(function* () {}).constructor('', 'yield 1')().next().value", '1', 'a generator body'),
    ('Y23', "Function('a = new.target', 'return typeof a')()", U, 'new.target in a parameter initialiser'),
    ('Y24', "Function('', '\"use strict\"; var public;')", S, 'a strict body with a strict reserved word'),
    ('Y25', "Function('', 'return 010')()", '8', 'a sloppy body with a legacy octal literal'),
]


def js(s):
    return json.dumps(s, ensure_ascii=True)


def subst(tmpl, text):
    if '(0, eval)(@E@)' in tmpl:
        return tmpl.replace('@E@', js(text))
    return tmpl.replace('eval(E)', 'eval(' + js(text) + ')')


def main():
    rows = []
    head = [
        '// SJL-14.1 task 1.1 (decision D44): the inherited-context probes, generated by gen_probes.py. Run as one',
        '// program, prelude.js + <this file> + epilogue.js. Each line prints `<id> => <value>` or `<id> => THROWS <name>:',
        '// <message>`.']
    direct = head + ['// The direct form: each probe\'s context is written in this program itself (global code for the G probes).']
    ineval = head + ['// The in-eval form: each probe\'s context is compiled by an indirect eval, so its functions and classes are',
                     '// themselves eval code; "THROWS (template)" means the engine refused or failed that program, not the text.']
    n = 0
    probes = []
    for cid, cdesc, caller, tmpl, facts, kind in CONTEXTS:
        for kid, text, spec, why in CONSTRUCTS:
            n += 1
            pid = 'IC%02d-%s-%s-%s' % (n, cid, 'S' if caller.startswith('strict') else 's', kid)
            probes.append((pid, cdesc, caller, kid + ': ' + text, spec(facts), why, subst(tmpl, text), kind, text))
    for xid, cdesc, caller, what, tmpl, text, spec, why in EXTRA:
        probes.append((xid, cdesc, caller, what + ': ' + text, spec, why, subst(tmpl, text), 'expr', text))
    for yid, expr, spec, why in FUNCTIONS:
        probes.append((yid, 'a dynamic function (Function, GeneratorFunction, AsyncFunction)', 'sloppy unless stated', why, spec, 'CreateDynamicFunction', '__reached = true; ' + expr, 'fn', expr))
    for pid, cdesc, caller, probe, spec, why, src, kind, text in probes:
        ineval.append('tt(%s, %s);' % (js(pid), js(src)))
        if kind == 'global' and caller == 'sloppy':
            direct.append('try { __v = show(eval(%s)); } catch (e) { __v = \'THROWS \' + errName(e) + \': \' + plain(String(e && e.message)); } out(%s + \' => \' + __v);' % (js(text), js(pid)))
        elif kind == 'global':
            direct.append('tt(%s, %s);' % (js(pid), js(src)))
        elif kind == 'fn':
            direct.append('t(%s, function () { return %s; });' % (js(pid), src.replace('__reached = true; ', '')))
        else:
            direct.append('t(%s, function () { return %s; });' % (js(pid), src))
        rows.append((pid, cdesc, caller, probe, spec, why, src))
    for name, lines in (('ic-values.js', direct), ('ic-values-in-eval.js', ineval)):
        with open(os.path.join(HERE, name), 'w', encoding='utf-8', newline='\n') as fh:
            fh.write('\n'.join(lines) + '\n')
    with open(os.path.join(HERE, 'probes.tsv'), 'w', encoding='utf-8', newline='\n') as fh:
        fh.write('id\tcontext\tcaller\tprobe\tspecification\twhy\tprogram\n')
        for r in rows:
            fh.write('\t'.join(c.replace('\t', ' ').replace('\n', '\\n') for c in r) + '\n')


if __name__ == '__main__':
    main()
