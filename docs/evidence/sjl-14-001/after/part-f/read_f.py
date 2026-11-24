#!/usr/bin/env python3
"""Part F's metadata reading (bundle sjl-14-001, decision rule part F: "every public type dispositions.md marks obsolete
carries the mark with the removal version decision D50 names"): the after run's metadata reader output
(after/local/obsolete/obsolete-marks.tsv, every public type of Broiler.JavaScript.Ast and Broiler.JavaScript.Parser in
the packages it read) against dispositions.tsv's type lines, each type with its disposition. Written 2026-11-24.

    read_f.py <repo root> <obsolete-marks.tsv> <out file>

A type whose disposition is obsolete or facade-obsolete must be read "yes", with the diagnostic identifier BJS0002 and
a message naming the removal version 1.0.0 (decision D50: "One removal version, 1.0.0"); a moved-forwarded type is read
"forwarded" (to Broiler.JavaScript.Expressions) or, where it stays in Ast, "no"; every reader row is matched to a line of
dispositions.tsv, nested types by their outer type's line. Run with python3 -I."""
import re, sys
root, marks, out = sys.argv[1:4]
disp = {}
for line in open(root + "/docs/evidence/sjl-14-001/dispositions.tsv", encoding="utf-8"):
    if line.startswith("#") or line.startswith("assembly\t"): continue
    f = line.rstrip("\n").split("\t")
    if f[3] == "(type)": disp[(f[0], f[1] + "." + f[2])] = (f[4], f[5])
L, bad = [], 0
L.append("# read_f.py: each public type the after commit's metadata reader lists, with dispositions.tsv's line for it")
L.append("assembly\ttype\treader\tdiagnosticId\tmessage names 1.0.0\tdispositions.tsv\tremoval\treading")
rows = [l.rstrip("\n").split("\t") for l in open(marks, encoding="utf-8")][1:]
for asm, typ, obs, did, err, msg in rows:
    name = re.sub(r"`\d+", "", typ).replace("+", ".")
    d = disp.get((asm, name))
    if d is None:
        d = disp.get((asm, name.rsplit(".", 1)[0]), ("(none)", "-"))
    want_mark = d[0] in ("obsolete", "facade-obsolete")
    if want_mark:
        ok = obs == "yes" and did == "BJS0002" and "removed in version 1.0.0" in msg and d[1] == "1.0.0"
        reading = "marked, as dispositions.md asks" if ok else "NOT MARKED AS ASKED"
    elif d[0] == "moved-forwarded":
        ok = obs in ("forwarded", "no")
        reading = ("forwarded to " + msg) if obs == "forwarded" else "not forwarded, not marked: dispositions.md marks it moved-forwarded, which asks no mark" if obs == "no" else "MARKED, which dispositions.md does not ask"
    else:
        ok, reading = False, "NO LINE IN dispositions.tsv"
    bad += not ok
    L.append("\t".join([asm, typ, obs, did or "-", str("removed in version 1.0.0" in msg).lower() if obs == "yes" else "-", d[0], d[1], reading]))
L.append("# rows: %d; read as dispositions.md asks: %d; not: %d" % (len(rows), len(rows) - bad, bad))
types_marked = {k for k, v in disp.items() if v[0] in ("obsolete", "facade-obsolete")}
seen = {(a, re.sub(r"`\d+", "", t).replace("+", ".")) for a, t, *_ in rows}
missing = sorted(types_marked - seen)
L.append("# types dispositions.tsv marks obsolete that the reader does not list (a public type absent at the after commit): %d" % len(missing))
L += ["#   " + "\t".join(m) for m in missing]
open(out, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
print(L[-1 - len(missing)], L[-2 - len(missing)])
