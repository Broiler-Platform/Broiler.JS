#!/usr/bin/env python3
"""test_projects.py <slnx-test-projects.txt> <ci-folder>: writes, to stdout, every test project of Broiler.JS.slnx at
be6c4a5c with every "Passed!"/"Failed!" line the test jobs' logs hold for its assembly, in the form of bundle
sjl-14-001's bridge/w1-ci/test-projects.tsv, and the engine corpus's summary from CI's two engine vectors jobs. It reads
the job logs retained in <ci-folder> as GitHub returned them: ci.yml's first attempt and Shared libraries' one attempt
from read-2026-11-24T1826Z/, ci.yml's second attempt (the re-run of its failed jobs) from logs-45455636089-attempt-2.zip.
Read-only; written 2026-11-24 for part B at be6c4a5c."""
import os, re, sys, zipfile
slnx, ci = sys.argv[1], sys.argv[2]
R1 = os.path.join(ci, "read-2026-11-24T1826Z")
SOURCES = [  # (zip, member, run, attempt, job id, job name)
    (os.path.join(R1, "logs-45455636086.zip"), "3_Build and SL rules (ubuntu-24.04).txt", "45455636086", "1", "138664188516", "Build and SL rules (ubuntu-24.04)"),
    (os.path.join(R1, "logs-45455636086.zip"), "4_Build and SL rules (windows-latest).txt", "45455636086", "1", "138664188152", "Build and SL rules (windows-latest)"),
    (os.path.join(R1, "logs-45455636089.zip"), "5_ubuntu-24.04 _ Release.txt", "45455636089", "1", "138664605408", "ubuntu-24.04 / Release"),
    (os.path.join(R1, "logs-45455636089.zip"), "3_windows-latest _ Release.txt", "45455636089", "1", "138664605556", "windows-latest / Release"),
    (os.path.join(ci, "logs-45455636089-attempt-2.zip"), "2_windows-latest _ Release.txt", "45455636089", "2", "138692855522", "windows-latest / Release"),
]
VECTORS = [
    (os.path.join(R1, "logs-45455636089.zip"), "2_engine vectors (ubuntu-24.04).txt", "45455636089", "1", "138664605531", "engine vectors (ubuntu-24.04)"),
    (os.path.join(R1, "logs-45455636089.zip"), "4_engine vectors (windows-latest).txt", "45455636089", "1", "138664605462", "engine vectors (windows-latest)"),
]
TS = re.compile(r"^\d{4}-\d\d-\d\dT[\d:.]+Z ")
def lines(z, m):
    with zipfile.ZipFile(z) as f:
        return [TS.sub("", l.rstrip("\r")) for l in f.read(m).decode("utf-8", "replace").split("\n")]
logs = [(s, lines(s[0], s[1])) for s in SOURCES]
print("# be6c4a5c3e389c7eb2aff71c34e31bdae5efe276: every test project of Broiler.JS.slnx at the first model commit, with the job and the result line its run wrote.")
print("# Read from the job logs retained beside this file (as GitHub returned them), by ../scripts/test_projects.py. Written 2026-11-24.")
print("# Only the lines the logs hold are listed: ci.yml's Test step leaves the Broiler.ECMAScript projects to Shared libraries, which runs no other,")
print("# and in ci.yml's first attempt the Windows job's Test step was aborted when the test host of Broiler.ECMAScript.Algorithms.Tests crashed,")
print("# so that job has no line of the isolated CompilerScaling step, which was skipped (see ../README.md).")
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
