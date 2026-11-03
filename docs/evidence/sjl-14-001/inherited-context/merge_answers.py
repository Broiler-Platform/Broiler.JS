#!/usr/bin/env python3
"""Merge the four runs' answers with probes.tsv into answers.tsv (SJL-14.1, decision D44).

Usage, from this folder: python3 -I merge_answers.py
Writes answers.tsv: one line per probe, with the specification's answer and each engine's answer in each form. The
column `matches` says, per engine and form, whether the answer begins with the specification's (a refusal is matched
on its error name, the message being each engine's own). Two runs write the same bytes."""
import os

HERE = os.path.dirname(os.path.abspath(__file__))
RUNS = ('js-direct', 'js-in-eval', 'vm-direct', 'vm-in-eval')


def rows(path):
    with open(path, encoding='utf-8') as fh:
        lines = [l for l in fh.read().split('\n') if l]
    head = lines[0].split('\t')
    return [dict(zip(head, l.split('\t'))) for l in lines[1:]]


probes = rows(os.path.join(HERE, 'probes.tsv'))
answers = {r: {a['id']: a['answer'] for a in rows(os.path.join(HERE, 'runs', r, 'answers.tsv'))} for r in RUNS}
out = ['\t'.join(['id', 'context', 'caller', 'probe', 'specification'] + ['broiler_js' if r == 'js-direct' else
       'broiler_vm' if r == 'vm-direct' else r.replace('-', '_').replace('js_', 'broiler_js_').replace('vm_', 'broiler_vm_')
       for r in RUNS] + ['matches'])]
for p in probes:
    got = [answers[r][p['id']] for r in RUNS]
    m = ''.join('=' if g.startswith(p['specification']) else 'x' for g in got)
    out.append('\t'.join([p['id'], p['context'], p['caller'], p['probe'], p['specification']] + got + [m]))
with open(os.path.join(HERE, 'answers.tsv'), 'w', encoding='utf-8', newline='\n') as fh:
    fh.write('\n'.join(out) + '\n')
