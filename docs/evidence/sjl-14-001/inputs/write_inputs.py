#!/usr/bin/env python3
"""Write bundle sjl-14-001's part J seed list, inputs/fuzz-seeds.txt (SJL-14.1, task 1.2).

Usage, from the repository root:
  python3 -I docs/evidence/sjl-14-001/inputs/write_inputs.py write
  python3 -I docs/evidence/sjl-14-001/inputs/write_inputs.py check

The seeds are (1) every configuration-J line of bundle sjl-13-001's home fuzz seeds
(docs/evidence/sjl-13-001/inputs/fuzz-seeds.txt), each an input of the parse corpus chosen by that file's rule from the
manifest alone, and (2) every file of inputs/seeds/, written for this bundle to reach the facts the bridge synthesises
(decision D44), each named by its SHA-256. `check` rebuilds the file in memory and compares it byte for byte. Two runs
write the same bytes: no clock, locale, hash order or file-system order reaches them (the seed files are sorted by
name)."""
import hashlib, os, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..', '..'))
HERE = os.path.join(ROOT, 'docs', 'evidence', 'sjl-14-001', 'inputs')
HOME_SEEDS = os.path.join(ROOT, 'docs', 'evidence', 'sjl-13-001', 'inputs', 'fuzz-seeds.txt')
OUT = os.path.join(HERE, 'fuzz-seeds.txt')
MUTANTS = 64
BOUND_MS = 10000


def build():
    with open(HOME_SEEDS, encoding='utf-8') as fh:
        home = fh.read().split('\n')
    corpus = [l for l in home if l.startswith('#% corpus\t')]
    assert len(corpus) == 1
    home_sha = hashlib.sha256(open(HOME_SEEDS, 'rb').read()).hexdigest()
    lines = [
        '# Bundle sjl-14-001 (Broiler.JS), rule part J: the seeds of the seeded mutation session at the after commit.',
        '# Written by inputs/write_inputs.py; never edited by hand. inputs/README.md says how a seed is read.',
        '#% format\tsjl-14-001-fuzz-seeds\t1',
        corpus[0],
        '#% home-seeds\tdocs/evidence/sjl-13-001/inputs/fuzz-seeds.txt\t' + home_sha,
        '#%% time-bound-per-compile-ms\t%d' % BOUND_MS,
        '#%% mutants-per-line\t%d' % MUTANTS,
        'seed\tgoal\tsource\tmutants\tselection',
    ]
    started = False
    for l in home:
        if not l or l.startswith('#'):
            continue
        if not started:
            started = True
            continue
        c = l.split('\t')
        if c[2] != 'J':
            continue
        lines.append('\t'.join([c[0], c[1], 'corpus', str(MUTANTS), 'home:' + c[4]]))
    seeds = os.path.join(HERE, 'seeds')
    for name in sorted(os.listdir(seeds)):
        data = open(os.path.join(seeds, name), 'rb').read()
        goal = 'module' if name.endswith('.mjs') else 'script'
        lines.append('\t'.join(['seeds/' + name, goal, hashlib.sha256(data).hexdigest(), str(MUTANTS), 'bridge-facts']))
    return ''.join(l + '\n' for l in lines)


def main():
    if len(sys.argv) != 2 or sys.argv[1] not in ('write', 'check'):
        sys.exit(__doc__)
    text = build()
    if sys.argv[1] == 'write':
        with open(OUT, 'w', encoding='utf-8', newline='\n') as fh:
            fh.write(text)
        print('wrote inputs/fuzz-seeds.txt')
        return
    try:
        same = open(OUT, encoding='utf-8', newline='').read() == text
    except FileNotFoundError:
        same = False
    print(('same      ' if same else 'DIFFERENT ') + 'inputs/fuzz-seeds.txt')
    sys.exit(0 if same else 1)


if __name__ == '__main__':
    main()
