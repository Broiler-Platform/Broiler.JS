#!/usr/bin/env python3
"""Part J's corrected session, read (bundle sjl-14-001, the owner's answer 14 of 2026-11-24): the corrected session's
findings and its control's, by seed, type and message (after/part-j-corrected/fuzz/session/ and fcontrol/session/),
each set against the first session's (after/local/fuzz/session/ and fcontrol/session/) mutant by mutant, and each
finding read under decision-rule-revised-2026-11-24.md's part J: a compile past the bound of the seed V000002, or of a
mutant of it that the frontend accepts, is not a finding. Whether a timed-out mutant of V000002 is one the frontend
accepts is read from after/owed-runs/jsingle/cases.tsv (each file compiled alone at the after commit, waiting past the
bound), by the mutant's SHA-256. Written 2026-11-24.

    read_jc.py <bundle folder> > <out>

Run with python3 -I."""
import collections, os, sys

B = sys.argv[1]
P = lambda *p: os.path.join(B, *p)
SEED = "V000002"
SEED_SHA = "875ff95ce7bf8af754d637c19327bb231574c898c21deba0a731963783cfd28f"


def rows(path):
    return [l.rstrip("\n").split("\t") for l in open(path, encoding="utf-8") if l.strip()]


single = {}
for r in rows(P("after/owed-runs/jsingle/cases.tsv"))[1:]:
    single[r[1]] = r
out = ["# read_jc.py: part J's corrected session and its control, against the first session's, and read under the revision"]
for name in ("fuzz", "fcontrol"):
    d = P("after/part-j-corrected", name, "session")
    first = P("after/local", name, "session")
    m = {(r[0], r[1]): r for r in rows(os.path.join(d, "mutants.tsv"))}
    m1 = {(r[0], r[1]): r for r in rows(os.path.join(first, "mutants.tsv"))}
    proc = {r[0]: r for r in rows(os.path.join(d, "processes.tsv"))}
    fnd = rows(os.path.join(d, "findings.tsv")) if os.path.exists(os.path.join(d, "findings.tsv")) else []
    out.append("")
    out.append("## %s (after/part-j-corrected/%s/session/): mutants %d (%s); findings.tsv rows %d; processes %d, exits %s" % (
        "the corrected session" if name == "fuzz" else "its control", name, len(m),
        ", ".join("%s %d" % kv for kv in sorted(collections.Counter(r[4] for r in m.values()).items())), len(fnd),
        len(proc) - 1, ", ".join("%s %d" % kv for kv in sorted(collections.Counter(p[1] for k, p in proc.items() if k != "seed").items()))))
    same = sorted(m) == sorted(m1) and all(m[k][2:4] == m1[k][2:4] for k in m)
    out.append("# against the first session's (after/local/%s/session/mutants.tsv): the same seeds, indices, operators and "
               "mutant SHA-256s: %s" % (name, "yes" if same else "NO"))
    moved = collections.Counter((k[0], m1[k][4] + " -> " + m[k][4]) for k in m if k in m1 and m1[k][4] != m[k][4])
    for (seed, mv), n in sorted(moved.items()):
        out.append("outcome moved\t%d\t%s\t%s" % (n, seed, mv))
    if not moved:
        out.append("outcome moved\tnone")
    reading = collections.Counter()
    for r in fnd:
        seed, index, typ, msg, sha = r[0], r[1], r[2], r[3], r[4]
        if seed == SEED and typ == "timeout":
            s = single.get(sha)
            if s is not None and s[3] == "accepted":
                k = "not a finding under the revision: a compile past the bound of a mutant of V000002 the frontend accepts"
            else:
                k = "a finding: a compile past the bound of a mutant of V000002 not shown accepted"
        else:
            k = "a finding"
        reading[(seed, typ, msg, k)] += 1
    for (seed, typ, msg, k), n in sorted(reading.items()):
        out.append("%d\t%s\t%s\t%s\t%s" % (n, seed, typ, msg, k))
    out.append("# findings read as findings under the revision: %d" % sum(n for (s, t, mm, k), n in reading.items() if k.startswith("a finding")))
seed_line = [r for r in rows(P("inputs/fuzz-seeds.txt")) if r[0] == SEED]
out.append("")
out.append("## the seed: %s, %s" % (SEED, "\t".join(seed_line[0][1:]) if seed_line else "NOT IN THE SEED LIST"))
manifest = [r for r in rows(os.path.join(B, "../../../conformance/parse/corpus.manifest")) if r[0] == SEED]
out.append("# its manifest row: %s; SHA-256 %s" % (
    "\t".join(manifest[0][1:4]) if manifest else "NOT FOUND", "the revision's" if manifest and manifest[0][3] == SEED_SHA else "NOT THE REVISION'S"))
print("\n".join(out))
