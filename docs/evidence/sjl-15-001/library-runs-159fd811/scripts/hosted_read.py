# hosted_read.py <workflow dir>: for each native-aot-probe job of the imported run, the probe's summary lines in its log
# (probe-runs.txt) and, for the artifact's two publish logs and each publish step of the job's log, the count of lines
# matching the workflow's `warning [A-Z]+[0-9]+`, a `warning` in any letter case, and an IL code of four digits
# (warnings.txt), in bundle sjl-13-001's part-d-commit/probe/workflow/ form. Reads only; written 2026-11-28.
import gzip, re, sys, os
w = sys.argv[1]
legs = {"140783796376": "linux-x64", "140783796520": "win-x64"}
pats = [("coded", re.compile(r"warning [A-Z]+[0-9]+")), ("any-case", re.compile(r"warning", re.I)), ("IL code", re.compile(r"\bIL[0-9]{4}\b"))]
runs, warns = [], []
def count(lines):
    return [(n, sum(1 for l in lines if p.search(l))) for n, p in pats]
for job, leg in legs.items():
    lines = gzip.open(f"{w}/jobs/{job}.log.gz", "rt", encoding="utf-8", errors="replace").read().split("\n")
    for l in lines:
        if "Broiler.ECMAScript.Probe:" in l:
            runs.append(f"{leg}\t{job}\t{l.split('Z ', 1)[1] if 'Z ' in l else l}")
    groups, cur = [], None
    for l in lines:
        if "##[group]Run " in l:
            cur = [l]; groups.append(cur)
        elif cur is not None:
            cur.append(l)
    for g in groups:
        body = "\n".join(g[:4])
        if "dotnet publish Broiler.ECMAScript/Broiler.ECMAScript.Probe" in body:
            kind = "jit" if "PublishAot=false" in body else "aot"
            # the step's own output: lines after its ##[endgroup] (the command echo and env block are excluded)
            out = g[next(i for i, x in enumerate(g) if "##[endgroup]" in x) + 1:]
            for n, c in count(out):
                warns.append(f"{leg}\tjob {job} log, publish step ({kind})\t{n}\t{c}")
    for f in ("probe-publish.log", "probe-publish-jit.log"):
        art = open(f"{w}/artifacts/native-aot-probe-{leg}/{f}", encoding="utf-8", errors="replace").read().split("\n")
        for n, c in count(art):
            warns.append(f"{leg}\tartifact {f}\t{n}\t{c}")
open(f"{w}/probe-runs.txt", "w").write("".join(r + "\n" for r in runs))
open(f"{w}/warnings.txt", "w").write("leg\twhere\tpattern\tlines\n" + "".join(x + "\n" for x in warns))
print("summary lines", len(runs), "warning counts", sum(int(x.split("\t")[-1]) for x in warns))
