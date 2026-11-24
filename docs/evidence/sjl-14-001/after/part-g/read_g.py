#!/usr/bin/env python3
"""Part G's comparison (bundle sjl-14-001, decision rule part G): "per test, its result against the base candidate and
against the after candidate", from TRX files alone. Written 2026-11-24.

    read_g.py <suite> <base trx> <after trx> [<suite> <base trx> <after trx> ...] > <out>

Each test is named by its TestMethod's className and name (the TRX's TestDefinitions) and its result is its
UnitTestResult's outcome; a test that appears more than once in one TRX (data rows) is keyed by its testName as well,
and where two rows still share that key (a data row's display name cut short alike), the key holds their outcomes in
order, compared as a list.
For each suite: the tests on both sides, those on one side only, and every test whose outcome differs, with both
outcomes; and the rule's condition, "every test that passes against the base candidate passes against the after
candidate". Run with python3 -I."""
import collections, sys
import xml.etree.ElementTree as ET


def read(path):
    t = ET.parse(path).getroot()
    tag = lambda e: e.tag.rsplit("}", 1)[-1]
    meth = {}
    for e in t.iter():
        if tag(e) == "UnitTest":
            m = next((c for c in e if tag(c) == "TestMethod"), None)
            meth[e.get("id")] = (m.get("className", "") + "." + m.get("name", "")) if m is not None else e.get("name")
    res = {}
    for e in t.iter():
        if tag(e) == "UnitTestResult":
            key = (meth.get(e.get("testId"), "?"), e.get("testName"))
            res.setdefault(key, []).append(e.get("outcome"))
    return {k: "+".join(sorted(v)) for k, v in res.items()}


args = sys.argv[1:]
print("# read_g.py: part G per test, each suite's TRX at the base candidate and at the after candidate, as given")
ok_all = True
for i in range(0, len(args), 3):
    suite, bp, ap = args[i:i + 3]
    b, a = read(bp), read(ap)
    both = set(b) & set(a)
    lost = sorted(k for k in b if set(b[k].split("+")) == {"Passed"} and a.get(k) != b[k])
    changed = sorted(k for k in both if b[k] != a[k])
    print("## %s" % suite)
    print("base: %s" % bp)
    print("after: %s" % ap)
    print("results at the base: %d (%s); after: %d (%s); test keys on both: %d; at the base only: %d; after only: %d" % (
        sum(len(v.split("+")) for v in b.values()), ", ".join("%s %d" % kv for kv in sorted(collections.Counter(o for v in b.values() for o in v.split("+")).items())),
        sum(len(v.split("+")) for v in a.values()), ", ".join("%s %d" % kv for kv in sorted(collections.Counter(o for v in a.values() for o in v.split("+")).items())),
        len(both), len(set(b) - set(a)), len(set(a) - set(b))))
    for k in sorted(set(b) - set(a)): print("  at the base only: %s [%s] %s" % (k[0], k[1], b[k]))
    for k in sorted(set(a) - set(b)): print("  after only: %s [%s] %s" % (k[0], k[1], a[k]))
    print("outcome differs: %d" % len(changed))
    for k in changed: print("  %s [%s]: %s -> %s" % (k[0], k[1], b[k], a[k]))
    print("passing against the base candidate and not passing against the after candidate: %d" % len(lost))
    for k in lost: print("  %s [%s]: %s -> %s" % (k[0], k[1], b[k], a.get(k, "absent")))
    ok_all &= not lost
    print()
print("every test that passes against the base candidate passes against the after candidate, in every suite above: %s" % ok_all)
