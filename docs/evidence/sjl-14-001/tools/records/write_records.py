#!/usr/bin/env python3
"""Write bundle sjl-14-001's generated record tables (SJL-14.1, task 1.1).

Usage, from the repository root:
  python3 -I docs/evidence/sjl-14-001/tools/records/write_records.py write
  python3 -I docs/evidence/sjl-14-001/tools/records/write_records.py check

`write` writes, under docs/evidence/sjl-14-001/:
  corrections/class-e.tsv         every `syntax` row of the stage table that Broiler.JS checks only in its compiler
                                  (stage table column js_compiler_only = yes): class E of the rule (decision D45);
  corrections/class-t.tsv         those of them checked lazily or after other code ran (js_lazy = yes or partly):
                                  class T (decision D64);
  corrections/partly.tsv          the `syntax` rows whose check Broiler.JS makes partly in its parser and partly in its
                                  compiler (js_compiler_only = partly), which class E as decision D45 words it does not
                                  name (corrections.md, finding S14-F1);
  corrections/k-paths.tsv         each correction K1 to K10 with its rows and the test262 paths that show it;
  corrections/test262-paths.tsv   every test262 input and variant of the parse corpus whose expected outcome at the
                                  syntax half, in configuration J of bundle sjl-13-001's classification, differs from
                                  this engine's SJL-12 after baseline, with the correction or class that explains it;
  dispositions.tsv                every public type and member of Broiler.JavaScript.Ast and Broiler.JavaScript.Parser
                                  in the API listings under api/, with its disposition (decision D50).
`check` rebuilds each in memory and compares it with the file byte for byte. Two runs write the same bytes: no clock,
locale, hash order or file-system order reaches them.

Inputs (all in this repository): Broiler.ECMAScript/docs/frontend/stages.tsv and early-errors.tsv; bundle
sjl-13-001's classification/classification.tsv.gz; conformance/parse/corpus.manifest; bundle sjl-12-001's
after/parse-corpus-c.tsv.gz (entries P and K at the SJL-12 after commit); corrections/k-rows.tsv (written by hand);
api/Broiler.JavaScript.Ast.txt and api/Broiler.JavaScript.Parser.txt.
"""
import csv, gzip, io, os, re, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..', '..', '..'))
B = os.path.join(ROOT, 'docs', 'evidence', 'sjl-14-001')
STAGES = os.path.join(ROOT, 'Broiler.ECMAScript', 'docs', 'frontend', 'stages.tsv')
CATALOGUE = os.path.join(ROOT, 'Broiler.ECMAScript', 'docs', 'frontend', 'early-errors.tsv')
CLASSIFICATION = os.path.join(ROOT, 'docs', 'evidence', 'sjl-13-001', 'classification', 'classification.tsv.gz')
MANIFEST = os.path.join(ROOT, 'conformance', 'parse', 'corpus.manifest')
JB = os.path.join(ROOT, 'docs', 'evidence', 'sjl-12-001', 'after', 'parse-corpus-c.tsv.gz')
KROWS = os.path.join(B, 'corrections', 'k-rows.tsv')
API = os.path.join(B, 'api')

COMPILER_SITES = {'SyntaxValidation', 'FastCompiler.EarlyErrors', 'Visit*', 'DirectEvalSupport'}
REGEXP_ROWS = {'EE0186', 'EE0187', 'EE0188', 'EE0189', 'EE0190', 'EE0208'}
CORR_ROW = re.compile(r'^(?:[TPNDRB]\d|IC\d)')


def tsv(path, opener=open):
    with opener(path, 'rt', encoding='utf-8', newline='') as fh:
        lines = [l for l in fh.read().split('\n') if l and not l.startswith('#')]
    head = lines[0].split('\t')
    return [dict(zip(head, l.split('\t'))) for l in lines[1:]]


def out(lines):
    return ''.join(l + '\n' for l in lines)


def class_tables():
    stages = tsv(STAGES)
    e_rows, t_rows, p_rows = [], [], []
    for r in stages:
        if r['stage'] != 'syntax':
            continue
        if r['js_compiler_only'] == 'yes':
            parts = set(r['js_stage'].split('+'))
            assert parts <= COMPILER_SITES, r['id']
            e_rows.append(r)
            if r['js_lazy'] in ('yes', 'partly'):
                t_rows.append(r)
        elif r['js_compiler_only'] == 'partly':
            p_rows.append(r)
    hdr = ['# Bundle sjl-14-001 (Broiler.JS): %s', '# Written by tools/records/write_records.py from',
           '# Broiler.ECMAScript/docs/frontend/stages.tsv; never edited by hand. corrections.md says how it is read.']
    cols = ['id', 'shared_code', 'js_stage', 'js_lazy', 'js_site', 'rows']
    files = {}
    for name, rows, what in (('class-e.tsv', e_rows, 'class E, the syntax rows Broiler.JS checks only in its compiler (decision D45)'),
                             ('class-t.tsv', t_rows, 'class T, the class E rows checked lazily or after other code ran (decision D64)'),
                             ('partly.tsv', p_rows, 'the syntax rows Broiler.JS checks partly in its parser, partly in its compiler (finding S14-F1)')):
        lines = [hdr[0] % what, hdr[1], hdr[2], '\t'.join(cols)]
        lines += ['\t'.join(r[c] for c in cols) for r in rows]
        files['corrections/' + name] = out(lines)
    return files, {r['id'] for r in e_rows}, {r['id'] for r in t_rows}, {r['id'] for r in p_rows}


def paths_table(e_ids, t_ids, p_ids):
    krows = {r['row']: r['k'] for r in tsv(KROWS)}
    cat = {r['id']: r for r in tsv(CATALOGUE)}
    manifest = {r['id']: r for r in tsv(MANIFEST)}
    jb = {}
    for r in tsv(JB, gzip.open):
        jb[(r['id'], r['variant'], r['entry'])] = r['outcome']
    lines = ['# Bundle sjl-14-001 (Broiler.JS): the test262 inputs of the parse corpus whose expectation at the syntax half',
             '# (bundle sjl-13-001\'s classification, configuration J) differs from this engine\'s SJL-12 after baseline',
             '# (bundle sjl-12-001, after/parse-corpus-c.tsv.gz, entries P and K). Written by tools/records/write_records.py;',
             '# never edited by hand. corrections.md says how it is read. kind: missed (expected refused, P and K accepted),',
             '# stage (expected refused, P accepted, K refused), refused-legal (expected accepted, P or K refused).',
             '\t'.join(['path', 'variant', 'id', 'expect', 'P', 'K', 'kind', 'first', 'rows', 'explained_by'])]
    for r in tsv(CLASSIFICATION, gzip.open):
        if r['config'] != 'J' or r['variant'] not in ('strict', 'sloppy', 'module', 'raw'):
            continue
        m = manifest.get(r['id'])
        if not m or m['origin'] != 'test262':
            continue
        exp = r['expect']
        if exp == 'either':
            continue
        p = jb.get((r['id'], r['variant'], 'P'), '-')
        k = jb.get((r['id'], r['variant'], 'K'), '-')
        if exp == 'refused' and p == 'accepted' and k == 'accepted':
            kind = 'missed'
        elif exp == 'refused' and p == 'accepted' and k != 'accepted':
            kind = 'stage'
        elif exp == 'accepted' and (p != 'accepted' or k != 'accepted'):
            kind = 'refused-legal'
        else:
            continue
        basis = [b for b in r['basis'].split(';') if CORR_ROW.match(b)]
        first = [f for f in r['first'].split(';') if f != '-']
        ks = sorted({krows[b] for b in basis if b in krows}, key=lambda x: int(x[1:]))
        other = sorted({b for b in basis if b not in krows})
        if kind == 'stage' and not ks:
            if first and all(f in e_ids for f in first):
                ks = ['T' if all(f in t_ids for f in first) else 'E']
            elif any(f in e_ids for f in first):
                ks = ['E-if-coded']
            elif any(f in p_ids for f in first):
                ks = ['E-partly']
            elif other:
                ks = ['row:' + o for o in other]
            else:
                ks = ['unexplained']
        elif kind == 'refused-legal' and not ks:
            ks = ['row:' + o for o in other] or ['unexplained']
        elif not ks:
            fam = {cat[f]['family'] for f in first if f in cat}
            if first and set(first) <= REGEXP_ROWS:
                ks = ['K9']
            elif first and fam <= {'EX'}:
                ks = ['K8']
            elif first and fam and 'EX' not in fam:
                ks = ['K7']
            elif first:
                ks = ['K7', 'K8']
            else:
                ks = ['unexplained']
        lines.append('\t'.join([m['path'], r['variant'], r['id'], exp, p, k, kind, ';'.join(first) or '-',
                                ';'.join(basis) or '-', ';'.join(ks)]))
    return out(lines)


def k_paths(test262_lines):
    manifest = {r['id']: r for r in tsv(MANIFEST)}
    lines = ['# Bundle sjl-14-001 (Broiler.JS): each correction K1 to K10 with its rows and the test262 paths that show it:',
             '# the corpus inputs each row cites (source cited, from corrections/k-rows.tsv\'s evidence column) and the inputs',
             '# corrections/test262-paths.tsv explains by the row (source paths). Written by tools/records/write_records.py;',
             '# never edited by hand. A row with no test262 path is shown by the probes or vectors its evidence names.',
             '\t'.join(['k', 'row', 'id', 'path', 'source'])]
    seen = set()
    for r in tsv(KROWS):
        ids = re.findall(r'\b(T\d{6})(?:\s*(?:-|to)\s*T(\d{6}))?', r['evidence'] + ' ' + r['broiler_js_at_the_base'])
        got = []
        for a, b in ids:
            if b:
                got += ['T%06d' % i for i in range(int(a[1:]), int(b) + 1)]
            else:
                got.append(a)
        for i in got:
            m = manifest.get(i)
            if m and m['origin'] == 'test262' and (r['k'], r['row'], i) not in seen:
                seen.add((r['k'], r['row'], i))
                lines.append('\t'.join([r['k'], r['row'], i, m['path'], 'cited']))
        for t in test262_lines:
            if r['row'] in t[1].split(';') and (r['k'], r['row'], t[2]) not in seen:
                seen.add((r['k'], r['row'], t[2]))
                lines.append('\t'.join([r['k'], r['row'], t[2], t[0], 'paths']))
        if not any(x[0] == r['k'] and x[1] == r['row'] for x in seen):
            lines.append('\t'.join([r['k'], r['row'], '-', '-', 'none']))
    return out(lines)


def dispositions():
    moved = {'StringSpan', 'StringSpanReader', 'ArraySpan', 'IPropertyAccessor', 'IPropertyValue', 'UnaryOperator',
             'TokenTypes', 'CancellableDisposableAction', 'RegExpUnicodeValidator', 'FastParseException'}
    lines = ['# Bundle sjl-14-001 (Broiler.JS): every public type and member of Broiler.JavaScript.Ast and',
             '# Broiler.JavaScript.Parser at 33d3093b (api/, PublicApiGenerator 11.4.6), with its disposition (decision D50).',
             '# Written by tools/records/write_records.py; never edited by hand. dispositions.md defines each disposition.',
             '\t'.join(['assembly', 'namespace', 'type', 'member', 'disposition', 'removal', 'home_after'])]
    for asm in ('Broiler.JavaScript.Ast', 'Broiler.JavaScript.Parser'):
        ns, stack = None, []
        with open(os.path.join(API, asm + '.txt'), encoding='utf-8') as fh:
            text = fh.read().split('\n')
        pending_attr = ''
        for raw in text:
            m = re.match(r'^namespace (\S+)', raw)
            if m:
                ns = m.group(1)
                continue
            s = raw.strip()
            if not s or s in ('{', '}'):
                if s == '}' and stack and len(raw) - len(raw.lstrip()) == stack[-1][1]:
                    stack.pop()
                continue
            if s.startswith('[') and not s.endswith(']') or s.startswith('"'):
                pending_attr += s
                continue
            if s.startswith('['):
                pending_attr += s
                continue
            indent = len(raw) - len(raw.lstrip())
            while stack and stack[-1][1] >= indent:
                stack.pop()
            tm = re.match(r'public (?:static |abstract |sealed |readonly |)*(class|struct|enum|interface|delegate) (\w+)', s)
            top = stack[0][0] if stack else None
            if tm:
                name = tm.group(2)
                path = '.'.join([x[0] for x in stack] + [name])
                outer = stack[0][0] if stack else name
                disp, removal, home = classify(asm, ns, outer, None, moved)
                lines.append('\t'.join([asm, ns, path, '(type)', disp, removal, home]))
                if not s.endswith('{ }'):
                    stack.append((name, indent))
                pending_attr = ''
                continue
            member = re.sub(r'\s*\{ \}$|\s*\{ get; (?:set; |init; )?\}$|\s*\{ (?:set|init); \}$|;$', '', s)
            member = re.sub(r'\s+', ' ', member)
            if stack:
                outer = stack[0][0]
                path = '.'.join(x[0] for x in stack)
                disp, removal, home = classify(asm, ns, outer, member, moved, obsolete=('Obsolete' in pending_attr))
                lines.append('\t'.join([asm, ns, path, member, disp, removal, home]))
            pending_attr = ''
    return out(lines)


def classify(asm, ns, outer, member, moved, obsolete=False):
    if asm == 'Broiler.JavaScript.Parser':
        if member is not None and 'UndoMark()' in member:
            return 'kept-obsolete-no-version', '-', 'Broiler.JavaScript.Parser (facade)'
        if outer in ('FastScope', 'FastScopeItem'):
            return 'facade-obsolete', '1.0.0', 'Broiler.JavaScript.Parser (facade)'
        return 'facade-obsolete', '1.0.0', 'Broiler.JavaScript.Parser (facade over the bridge)'
    if outer in moved:
        return 'moved-forwarded', '-', 'Broiler.JavaScript.Expressions (Ast forwards it)'
    return 'obsolete', '1.0.0', 'Broiler.JavaScript.Ast'


def build():
    files, e_ids, t_ids, p_ids = class_tables()
    files['corrections/test262-paths.tsv'] = paths_table(e_ids, t_ids, p_ids)
    t_lines = [l.split('\t') for l in files['corrections/test262-paths.tsv'].split('\n')[6:] if l]
    files['corrections/k-paths.tsv'] = k_paths([(c[0], c[8], c[2]) for c in t_lines])
    files['dispositions.tsv'] = dispositions()
    return files


def main():
    if len(sys.argv) != 2 or sys.argv[1] not in ('write', 'check'):
        sys.exit(__doc__)
    files = build()
    bad = 0
    for rel, text in sorted(files.items()):
        path = os.path.join(B, rel)
        if sys.argv[1] == 'write':
            with open(path, 'w', encoding='utf-8', newline='\n') as fh:
                fh.write(text)
            print('wrote ' + rel)
        else:
            try:
                with open(path, encoding='utf-8', newline='') as fh:
                    same = fh.read() == text
            except FileNotFoundError:
                same = False
            print(('same      ' if same else 'DIFFERENT ') + rel)
            bad += 0 if same else 1
    sys.exit(1 if bad else 0)


if __name__ == '__main__':
    main()
