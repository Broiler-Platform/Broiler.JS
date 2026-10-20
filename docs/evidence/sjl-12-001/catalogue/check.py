#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Re-run the early-error catalogue's completeness check (milestone SJL-12.3, task 3.11) against the inventories
beside this file. Standard library only.

    python3 -I docs/evidence/sjl-12-001/catalogue/check.py --vm <Broiler.VM checkout at d82b07a> \
        --js <Broiler.JS checkout at ec494f66 or later with the same Broiler.JS/ tree> \
        --test262 <tc39/test262 checkout at ccaac100> \
        --catalogue Broiler.ECMAScript/docs/frontend/early-errors.tsv \
        [--edition <es2026 spec.html, sha256 ce7bc301...>]

It re-derives each inventory's population from the code and the suite and checks, exiting 1 on any difference:
  (b) Broiler.VM: every line of src/Broiler.VM.Profile.JavaScript.Compiler/*.cs that names a registry code
      (SliceSourceDiagnosticCode.<name>), the enum's own file and the slice's sample programs aside, is a row of
      vm-sites.tsv, and every row names catalogue rows; every `new JsRegExpSyntaxError(` of
      src/Broiler.VM.Profile.JavaScript.Format/JsRegExpMatcher.cs is a row of vm-regexp-sites.tsv, the same;
  (b) Broiler.JS: every `throw` of Broiler.JavaScript.Parser, .Compiler and .Ast, and every call of a ThrowIf*
      helper, is a row of js-sites.tsv; every one that throws a SyntaxError, a ReferenceError or a TypeError names
      catalogue rows, and every other is marked not-a-refusal;
  (c) every test262 test whose front matter says `phase: parse` is a row of parse-negative.tsv, and names rows;
  (a, R20) with --edition, every clause outside the Early Errors clauses whose own text names SyntaxError is a row of
      spec-syntaxerror-clauses.tsv. Direction (a) itself is scripts/frontend/ee_extract.py's `check`.
"""
import argparse, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))


def read_tsv(name):
    with open(os.path.join(HERE, name), encoding='utf-8') as f:
        lines = f.read().split('\n')
    head = lines[0].split('\t')
    return [dict(zip(head, l.split('\t'))) for l in lines[1:] if l]


def catalogue_ids(path):
    with open(path, encoding='utf-8') as f:
        return {l.split('\t')[0] for l in f.read().split('\n')[1:] if l}


def rows_ok(cell, ids):
    parts = [x for x in cell.split(';') if x]
    return bool(parts) and all(x in ids for x in parts)


def vm_sites(vm):
    cdir = os.path.join(vm, 'src/Broiler.VM.Profile.JavaScript.Compiler')
    with open(os.path.join(cdir, 'SliceSourceDiagnostics.cs'), encoding='utf-8') as f:
        codes = set(re.findall(r'(\w+) = (\d{4}),', f.read()))
    names = {n for n, _ in codes}
    out = set()
    for fn in sorted(os.listdir(cdir)):
        if not fn.endswith('.cs') or fn in ('SliceSourceDiagnostics.cs', 'SliceSourcePrograms.cs'):
            continue
        with open(os.path.join(cdir, fn), encoding='utf-8') as f:
            for i, line in enumerate(f.read().splitlines(), 1):
                if any(m in names for m in re.findall(r'SliceSourceDiagnosticCode\.(\w+)', line)):
                    out.add(f'{fn}:{i}')
    return out


def vm_regexp_sites(vm):
    p = os.path.join(vm, 'src/Broiler.VM.Profile.JavaScript.Format/JsRegExpMatcher.cs')
    with open(p, encoding='utf-8') as f:
        return {f'JsRegExpMatcher.cs:{i}' for i, l in enumerate(f.read().splitlines(), 1) if 'new JsRegExpSyntaxError(' in l}


def js_sites(js):
    root = os.path.join(js, 'Broiler.JS')
    out = set()
    for proj in ('Broiler.JavaScript.Parser', 'Broiler.JavaScript.Compiler', 'Broiler.JavaScript.Ast'):
        for d, _, files in os.walk(os.path.join(root, proj)):
            if '/obj' in d or '/bin' in d:
                continue
            for fn in sorted(files):
                if not fn.endswith('.cs'):
                    continue
                rel = os.path.relpath(os.path.join(d, fn), root)
                with open(os.path.join(d, fn), encoding='utf-8-sig') as f:
                    for i, l in enumerate(f.read().splitlines(), 1):
                        s = l.strip()
                        if s.startswith('//') or s.startswith('*') or s.startswith('/*'):
                            continue
                        code = l.split('//')[0] if '"' not in l else l
                        if re.search(r'\bthrow\b', code):
                            out.add(f'{rel}:{i}')
                        elif re.search(r'\bThrowIf(FunctionDeclarationBody|LabeledFunctionInBody|LabelledFunctionLoopBody)\(', l) and 'static void' not in l:
                            out.add(f'{rel}:{i}')
    return out


def parse_negative(t262):
    out = set()
    for d, _, files in os.walk(os.path.join(t262, 'test')):
        for fn in files:
            if fn.endswith('.js'):
                p = os.path.join(d, fn)
                with open(p, encoding='utf-8', errors='replace') as f:
                    if re.search(r'^\s*phase:\s*parse', f.read(), re.M):
                        out.add(os.path.relpath(p, t262))
    return out


def main(argv):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--vm', required=True)
    ap.add_argument('--js', required=True)
    ap.add_argument('--test262', required=True)
    ap.add_argument('--catalogue', required=True)
    ap.add_argument('--edition')
    a = ap.parse_args(argv)
    ids = catalogue_ids(a.catalogue)
    problems = []

    def compare(label, derived, rows, key):
        have = {r[key] for r in rows}
        for s in sorted(derived - have):
            problems.append(f'{label}: {s} is in the code and not in the inventory')
        for s in sorted(have - derived):
            problems.append(f'{label}: {s} is in the inventory and not in the code')

    vm = read_tsv('vm-sites.tsv')
    compare('vm-sites.tsv', vm_sites(a.vm), vm, 'site')
    problems += [f"vm-sites.tsv: {r['site']} names no catalogue row" for r in vm if not rows_ok(r['rows'], ids)]
    rx = read_tsv('vm-regexp-sites.tsv')
    compare('vm-regexp-sites.tsv', vm_regexp_sites(a.vm), rx, 'site')
    problems += [f"vm-regexp-sites.tsv: {r['site']} names no catalogue row" for r in rx if not rows_ok(r['rows'], ids)]
    js = read_tsv('js-sites.tsv')
    compare('js-sites.tsv', js_sites(a.js), js, 'site')
    for r in js:
        if r['basis'] == 'not-a-refusal':
            if r['throws'] in ('SyntaxError', 'ReferenceError', 'TypeError', 'SyntaxError (helper call)'):
                problems.append(f"js-sites.tsv: {r['site']} throws {r['throws']} and is marked not-a-refusal")
        elif not rows_ok(r['rows'], ids) and r['rows'] != '-':
            problems.append(f"js-sites.tsv: {r['site']} names no catalogue row")
    pn = read_tsv('parse-negative.tsv')
    compare('parse-negative.tsv', parse_negative(a.test262), pn, 'test')
    for r in pn:
        named = {x for c in ('cited_by', 'vm_rows', 'js_rows') for x in r[c].split(';') if x and x != '-'}
        if not named or not named <= ids:
            problems.append(f"parse-negative.tsv: {r['test']} names no catalogue row")
    if a.edition:
        sys.path.insert(0, os.path.join(HERE, '../../../../scripts/frontend'))
        import ee_extract as E
        spec = E.Spec(a.edition, 'edition')
        derived = set()
        for sec in spec.sections:
            if 'Early Errors' in E.heading(sec):
                continue
            parts = []
            def walk(n):
                for c in n.children:
                    if isinstance(c, str):
                        parts.append(c)
                    elif c.tag not in E.SECTION_TAGS:
                        parts.append(c.text()) if c.tag == 'emu-xref' else walk(c)
            walk(sec)
            if 'SyntaxError' in ''.join(parts):
                derived.add(sec.attrs.get('id', ''))
        compare('spec-syntaxerror-clauses.tsv', derived, read_tsv('spec-syntaxerror-clauses.tsv'), 'clause')
    for p in problems:
        print(p)
    print(f'{len(vm)} VM emission sites, {len(rx)} VM regular-expression sites, {len(js)} Broiler.JS throw sites, '
          f'{len(pn)} parse-negative tests; {len(problems)} problems')
    return 1 if problems else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
