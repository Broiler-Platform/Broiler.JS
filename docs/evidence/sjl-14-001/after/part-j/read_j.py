#!/usr/bin/env python3
"""Part J's reading aid (bundle sjl-14-001, decision rule part J): the session's findings and the control's, by seed,
type and message, from the after run's retained session folders (after/local/fuzz/session/, after/local/fcontrol/
session/), with each seed's process record and goal, and the single-case reading aids of 2026-11-24
(after/owed-runs/jsingle/cases.tsv). Written 2026-11-24.

    read_j.py <repo root> > <out>

Run with python3 -I."""
import collections, os, sys
root = sys.argv[1]
B = os.path.join(root, "docs/evidence/sjl-14-001")
goal = {}
for line in open(os.path.join(B, "inputs/fuzz-seeds.txt"), encoding="utf-8"):
    f = line.rstrip("\n").split("\t")
    if len(f) >= 5 and not line.startswith("#") and f[0] != "seed":
        goal[f[0]] = (f[1], f[3], f[4])
print("# read_j.py: part J's findings and the control's, by seed, type and message; each seed's goal, mutant count and selection")
print("# from inputs/fuzz-seeds.txt; its process record from processes.tsv; and the single-case reading aids")
for name in ("fuzz", "fcontrol"):
    d = os.path.join(B, "after/local", name, "session")
    rows = [l.rstrip("\n").split("\t") for l in open(os.path.join(d, "findings.tsv"), encoding="utf-8")]
    proc = {l.split("\t")[0]: l.rstrip("\n").split("\t") for l in open(os.path.join(d, "processes.tsv"), encoding="utf-8")}
    outcomes = collections.Counter(l.split("\t")[4] for l in open(os.path.join(d, "mutants.tsv"), encoding="utf-8"))
    print("\n## %s (after/local/%s/session/): mutants %d (%s); findings.tsv rows %d; processes %d, exits %s" % (
        "the session" if name == "fuzz" else "the control", name, sum(outcomes.values()),
        ", ".join("%s %d" % kv for kv in sorted(outcomes.items())), len(rows), len(proc) - 1,
        ", ".join("%s %d" % kv for kv in sorted(collections.Counter(p[1] for k, p in proc.items() if k != "seed").items()))))
    c = collections.Counter((r[0], r[2], r[3]) for r in rows)
    for (seed, typ, msg), n in sorted(c.items()):
        g = goal.get(seed, ("?", "?", "?"))
        print("%d\t%s\tgoal %s, %s mutants, %s\tprocess exit %s in %s s\t%s\t%s" % (n, seed, g[0], g[1], g[2], proc.get(seed, ["", "?", "?"])[1], proc.get(seed, ["", "?", "?"])[2], typ, msg))
print("\n## the single-case reading aids (after/owed-runs/jsingle/cases.tsv), each file compiled alone at the after commit")
for line in open(os.path.join(B, "after/owed-runs/jsingle/cases.tsv"), encoding="utf-8"):
    print(line.rstrip("\n"))
