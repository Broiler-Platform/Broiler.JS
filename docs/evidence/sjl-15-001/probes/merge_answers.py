#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Merge the two runs' answers with probes.tsv into answers.tsv (SJL-15.1, Broiler.JS's half).

Usage, from this folder: python3 -I merge_answers.py
Writes answers.tsv: one line per probe, with the specification's answer and each engine's. The column `matches` says,
per engine (Broiler.JS, then Broiler.VM), whether the answer begins with the specification's (`=`) or not (`x`); a
refusal is matched on its error name and on whether any of the probe ran ("THROWS (template)" or "THROWS"), the message
being each engine's own. Bundle sjl-14-001's merge_answers.py, for two runs. Two runs write the same bytes."""
import os

HERE = os.path.dirname(os.path.abspath(__file__))
RUNS = ('js', 'vm')


def rows(path):
    with open(path, encoding='utf-8') as fh:
        lines = [l for l in fh.read().split('\n') if l]
    head = lines[0].split('\t')
    return [dict(zip(head, l.split('\t'))) for l in lines[1:]]


def matches(answer, expected):
    if not answer.startswith(expected):
        return False
    # "THROWS SyntaxError" must not match "THROWS SyntaxErrorX", nor a value its own prefix ("2" against "21").
    rest = answer[len(expected):]
    return rest == '' or rest.startswith(':')


probes = rows(os.path.join(HERE, 'probes.tsv'))
answers = {r: {a['id']: a['answer'] for a in rows(os.path.join(HERE, 'runs', r, 'answers.tsv'))} for r in RUNS}
out = ['\t'.join(['id', 'group', 'correspondence', 'catalogue', 'specification', 'broiler_js', 'broiler_vm', 'matches'])]
for p in probes:
    got = [answers[r][p['id']] for r in RUNS]
    m = ''.join('=' if matches(g, p['specification']) else 'x' for g in got)
    out.append('\t'.join([p['id'], p['group'], p['correspondence'], p['catalogue'], p['specification']] + got + [m]))
with open(os.path.join(HERE, 'answers.tsv'), 'w', encoding='utf-8', newline='\n') as fh:
    fh.write('\n'.join(out) + '\n')
