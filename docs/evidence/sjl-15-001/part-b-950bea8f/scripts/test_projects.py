#!/usr/bin/env python3
"""test_projects.py <slnx-test-projects.txt> <ci-folder>: writes, to stdout, every test project of Broiler.JS.slnx at
950bea8f with every "Passed!"/"Failed!" line the test jobs' logs hold for its assembly, in the form of bundle
sjl-14-001's bridge/w1-ci/test-projects.tsv, and the engine corpus's summary from CI's two engine vectors jobs. It reads
the job logs retained in <ci-folder> as GitHub returned them: ci.yml's one attempt and Shared libraries' one attempt.
Read-only. Copied 2026-11-27 from stage 6's part B script SCRATCH/sjl-rest/js-s156-pb/scripts/test_projects.py (itself
stage 5c's, stage 5b's, stage 5a's, stage 4's, stage 3's, stage 2's and stage 1's, for part B at 2c48beb1) for part B at
950bea8f: only this header, the log sources (each run's one logs zip, its members, run and job ids) and the printed
header lines changed."""
import os, re, sys, zipfile
slnx, ci = sys.argv[1], sys.argv[2]
SOURCES = [  # (zip, member, run, attempt, job id, job name)
    (os.path.join(ci, "logs-46029175030.zip"), "5_Build and SL rules (ubuntu-24.04).txt", "46029175030", "1", "140608266891", "Build and SL rules (ubuntu-24.04)"),
    (os.path.join(ci, "logs-46029175030.zip"), "1_Build and SL rules (windows-latest).txt", "46029175030", "1", "140608267017", "Build and SL rules (windows-latest)"),
    (os.path.join(ci, "logs-46029175113.zip"), "5_ubuntu-24.04 _ Release.txt", "46029175113", "1", "140608660671", "ubuntu-24.04 / Release"),
    (os.path.join(ci, "logs-46029175113.zip"), "2_windows-latest _ Release.txt", "46029175113", "1", "140608660701", "windows-latest / Release"),
]
VECTORS = [
    (os.path.join(ci, "logs-46029175113.zip"), "4_engine vectors (ubuntu-24.04).txt", "46029175113", "1", "140608660643", "engine vectors (ubuntu-24.04)"),
    (os.path.join(ci, "logs-46029175113.zip"), "3_engine vectors (windows-latest).txt", "46029175113", "1", "140608660675", "engine vectors (windows-latest)"),
]
TS = re.compile(r"^\d{4}-\d\d-\d\dT[\d:.]+Z ")
def lines(z, m):
    with zipfile.ZipFile(z) as f:
        return [TS.sub("", l.rstrip("\r")) for l in f.read(m).decode("utf-8", "replace").split("\n")]
logs = [(s, lines(s[0], s[1])) for s in SOURCES]
print("# 950bea8f315aa5f10d4b251a7d39ed7658912512: every test project of Broiler.JS.slnx at the ninth model commit, with the job and the result line its run wrote.")
print("# Read from the job logs retained beside this file (as GitHub returned them), by ../scripts/test_projects.py. Written 2026-11-27.")
print("# Only the lines the logs hold are listed: ci.yml's Test step tests every project but those of Broiler.ECMAScript.slnf (--engine-only),")
print("# which Shared libraries' Build and SL rules jobs run, and no other (see ../README.md).")
print("project\trun\tattempt\tjob-id\tjob\tresult-line")
for p in open(slnx).read().split():
    name = os.path.basename(p)[:-len(".csproj")]
    for s, ls in logs:
        hits = [l for l in ls if re.match(r"^(Passed|Failed)!", l) and (" - %s.dll " % name) in l]
        for h in hits:
            print("\t".join([name, s[2], s[3], s[4], s[5], h]))
    if name == "Broiler.JavaScript.Vectors.Tests":
        for v in VECTORS:
            ls = lines(v[0], v[1])
            i = next(k for k, l in enumerate(ls) if l.startswith("Total tests:"))
            summ = "Category=EngineCorpus: " + " ".join(x.strip() for x in ls[i:i + 3])
            print("\t".join([name, v[2], v[3], v[4], v[5], summ]))
