#!/usr/bin/env python3
"""Part C's local runs at W1, read and not judged (bundle sjl-14-001): every test's result and every test262 path and
variant's verdict, with the compiler's switch off and on. It classifies nothing.

    read_local.py <run dir> <corrections dir> <out dir>

<run dir> holds tests-off/ and tests-on/ (trx/<project>.trx, one dotnet test per test project) and t262-off/ and
t262-on/ (test262-results.json, merge_test262_shards.py's merged report of eight shards, and rerun-timeouts.json, each
timed-out path run again alone with one worker, whose rows replace the same (path, variant) rows, as
compare_test262_paths.py's --base-rerun and --after-rerun do). Writes, into <out dir>:
  tests-off.tsv.gz, tests-on.tsv.gz  project, test, outcomes (a data-driven test may report one name more than once)
  tests-differ.tsv                   every test whose outcomes differ off and on, or that one run has and the other not,
                                     with the switch-on run's first error line and first stack line
  t262-off.tsv.gz, t262-on.tsv.gz    path, variant, status, and whether the re-run gave it
  t262-differ.tsv.gz                 every (path, variant) whose status differs, with each run's first stderr line, and
                                     the rows of corrections/k-paths.tsv and corrections/test262-paths.tsv that name the
                                     path (a listing, not a class)
  counts.txt                         the counts of the above
The gzip files are written with no name and mtime 0, so the same inputs give the same bytes. Run with python3 -I.
"""
import collections, gzip, io, json, os, sys
import xml.etree.ElementTree as ET

run, corr, out = sys.argv[1:4]
NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def gz_write(path, text):
    with open(path, "wb") as f:
        with gzip.GzipFile(filename="", mode="wb", fileobj=f, compresslevel=9, mtime=0) as g:
            g.write(text.encode("utf-8"))


def first(e):
    t = (e.text or "").strip() if e is not None else ""
    return t.splitlines()[0][:400].replace("\t", " ") if t else "-"


def tests(d):
    res = {}
    for f in sorted(os.listdir(d + "/trx")):
        if not f.endswith(".trx"):
            continue
        proj = f[:-4]
        root = ET.parse(d + "/trx/" + f).getroot()
        for r in root.iter(NS + "UnitTestResult"):
            key = (proj, r.get("testName").replace("\t", " "))
            item = (r.get("outcome"), first(r.find(NS + "Output/" + NS + "ErrorInfo/" + NS + "Message")),
                    first(r.find(NS + "Output/" + NS + "ErrorInfo/" + NS + "StackTrace")))
            res.setdefault(key, []).append(item)
    return res


def table(path):
    cols, rows = None, []
    for line in open(path, encoding="utf-8"):
        if line.startswith("#"):
            continue
        f = line.rstrip("\n").split("\t")
        if cols is None:
            cols = f
            continue
        rows.append(dict(zip(cols, f)))
    return rows


def t262(d):
    def one(x, how):
        e = (x.get("stderr") or "").strip().splitlines()
        return (x["status"], how, (e[0][:300].replace("\t", " ") if e else "-"))
    r = json.load(open(d + "/test262-results.json", encoding="utf-8"))
    res = {(x["path"], x["variant"]): one(x, "-") for x in r["results"]}
    rr = d + "/rerun-timeouts.json"
    if os.path.exists(rr):
        for x in json.load(open(rr, encoding="utf-8"))["results"]:
            res[(x["path"], x["variant"])] = one(x, "rerun")
    return res


counts = []
a, b = tests(run + "/tests-off"), tests(run + "/tests-on")
for name, t in (("tests-off.tsv.gz", a), ("tests-on.tsv.gz", b)):
    gz_write(out + "/" + name, "project\ttest\toutcomes\n" + "".join(
        "%s\t%s\t%s\n" % (k[0], k[1], ",".join(x[0] for x in t[k])) for k in sorted(t)))
    c = collections.Counter(x[0] for v in t.values() for x in v)
    counts.append("%s: %d test name(s); results by outcome: %s" % (
        name[:-7], len(t), ", ".join("%s %d" % kv for kv in sorted(c.items()))))
moves = collections.Counter()
lines = ["project\ttest\toff\ton\ton-message\ton-stack\n"]
for k in sorted(set(a) | set(b)):
    x = ",".join(i[0] for i in a.get(k, [])) or "absent"
    y = ",".join(i[0] for i in b.get(k, [])) or "absent"
    if x != y:
        m = next((i for i in b.get(k, []) if i[0] != "Passed"), ("", "-", "-"))
        lines.append("%s\t%s\t%s\t%s\t%s\t%s\n" % (k[0], k[1], x, y, m[1], m[2]))
        moves[(k[0], x, y)] += 1
with open(out + "/tests-differ.tsv", "w", encoding="utf-8", newline="\n") as w:
    w.writelines(lines)
counts.append("tests whose outcomes differ off and on: %d" % (len(lines) - 1))
for k, v in sorted(moves.items()):
    counts.append("  %d\t%s\toff %s\ton %s" % (v, *k))

kp = collections.defaultdict(set)
for r in table(corr + "/k-paths.tsv"):
    if r.get("path") not in (None, "", "-", "none"):
        kp[r["path"]].add(r["k"] + ":" + r["row"])
tp = collections.defaultdict(set)
for r in table(corr + "/test262-paths.tsv"):
    tp[r["path"]].add(r["explained_by"])
a, b = t262(run + "/t262-off"), t262(run + "/t262-on")
for name, t in (("t262-off.tsv.gz", a), ("t262-on.tsv.gz", b)):
    gz_write(out + "/" + name, "path\tvariant\tstatus\trerun\n" + "".join(
        "%s\t%s\t%s\t%s\n" % (k[0], k[1], t[k][0], t[k][1]) for k in sorted(t)))
    c = collections.Counter(v[0] for v in t.values())
    counts.append("%s: %d case(s) (path and variant), after the re-run's rows; by status: %s; from the re-run: %d" % (
        name[:-7], len(t), ", ".join("%s %d" % kv for kv in sorted(c.items())),
        sum(1 for v in t.values() if v[1] == "rerun")))
moves = collections.Counter()
rows = ["path\tvariant\toff\ton\toff-stderr\ton-stderr\tk-paths\ttest262-paths\n"]
for k in sorted(set(a) | set(b)):
    x = a.get(k, ("absent", "-", "-"))
    y = b.get(k, ("absent", "-", "-"))
    if x[0] == y[0]:
        continue
    ks = ",".join(sorted(kp.get(k[0], ()))) or "-"
    eb = ",".join(sorted(tp.get(k[0], ()))) or "-"
    rows.append("\t".join([k[0], k[1], x[0], y[0], x[2], y[2], ks, eb]) + "\n")
    moves[(x[0], y[0], ks != "-" or eb != "-")] += 1
gz_write(out + "/t262-differ.tsv.gz", "".join(rows))
counts.append("test262 cases whose status differs off and on: %d" % (len(rows) - 1))
for k, v in sorted(moves.items()):
    counts.append("  %d\toff %s\ton %s\t%s" % (v, k[0], k[1], "a corrections table names the path" if k[2]
                                               else "no corrections table names the path"))
with open(out + "/counts.txt", "w", encoding="utf-8", newline="\n") as w:
    w.write("# read_local.py's counts; a reading, not a class\n" + "".join(c + "\n" for c in counts))
print(open(out + "/counts.txt", encoding="utf-8").read())
