#!/usr/bin/env python3
"""The third reading's aid for part K (bundle sjl-14-001, under decision-rule.md as revised by
decision-rule-revised-2026-11-23.md, decision-rule-revised-2026-11-24.md and decision-rule-revised-2026-11-24-k.md;
the owner's answer 16 of 2026-11-24): part K's retained hits read again under the kinds the revisions name, with
nothing built or run. Written 2026-11-24.

    read_3.py <bundle folder> > <out>

It reads after/part-k/hits.tsv (every hit of the after run's search, with its sort), after/part-k/listing.tsv (every
hit but a facade member, with the declaration that shows what it is) and after/part-k/listing-2.tsv (the message-string
hits with their message table, and the hits of a type outside the repository with that type and its assembly), each
checked by its SHA-256 against after/part-k/hashes.txt, and listing-2.tsv and scope.txt also against the digests the
revisions name; and after/local/delsearch/control.txt, the control. Each hit is read as one of the kinds part K's MET
line admits, as the revisions word it:
  a facade member          hits.tsv's sort "facade member", or listing.tsv's "the facade's own" (FastScanner's State
                           and its members, which dispositions.md keeps)
  a changelog line, a code comment, a record line                       listing.tsv's text kinds (2026-11-24)
  another type's member of the same name, listed with its declaration  listing.tsv's rows with a declaration in the
                           repository (2026-11-24)
  the same, its declaration shown by a .NET library type and its assembly   a listing.tsv row with no declaration
                           ("- :: -") that listing-2.tsv lists as a member of a .NET library type, with the type and
                           its assembly (2026-11-24-k)
  words inside a string literal that is a message text, listed with its message table   listing.tsv's message-string
                           rows that listing-2.tsv lists with the message table (member and declaration), its header
                           giving every check held (2026-11-24-k)
Any other hit is NOT ADMITTED. Run with python3 -I."""
import collections, hashlib, os, sys

B = sys.argv[1]
P = lambda *p: os.path.join(B, *p)
LISTING2_SHA = "89f60c40f0f274b5a059635de546482ca08ceecaa77b6416e8ce22183ea02334"  # decision-rule-revised-2026-11-24-k.md
SCOPE_SHA = "1cc113c548816a83eed6618890131f3bcaeea542d9ff1647f536626f6ef710e6"  # decision-rule-revised-2026-11-24.md
STRING_KIND = "text: words of a message string (not a comment, changelog or record line)"
out = []
w = out.append


def sha(path):
    return hashlib.sha256(open(path, "rb").read()).hexdigest()


def rows(path):
    for line in open(path, encoding="utf-8"):
        if line.startswith("#") or line.startswith("section\t") or line.startswith("what it is\t"):
            continue
        yield line.rstrip("\n").split("\t")


w("# read_3.py: part K read a third time, under the rule and its revisions of 2026-11-23, 2026-11-24 and 2026-11-24-k")
w("")
w("## the files read")
recorded = {}
for line in open(P("after/part-k/hashes.txt"), encoding="utf-8"):
    digest, name = line.split()
    recorded[name[2:]] = digest
for name in ["hits.tsv", "listing.tsv", "listing-2.tsv", "scope.txt"]:
    d = sha(P("after/part-k", name))
    note = "as after/part-k/hashes.txt names it" if recorded.get(name) == d else "NOT AS after/part-k/hashes.txt NAMES IT"
    if name == "listing-2.tsv":
        note += "; the revision of 2026-11-24-k's" if d == LISTING2_SHA else "; NOT THE REVISION OF 2026-11-24-k'S"
    if name == "scope.txt":
        note += "; the revision of 2026-11-24's" if d == SCOPE_SHA else "; NOT THE REVISION OF 2026-11-24'S"
    w("after/part-k/%s\t%s\t%s" % (name, d, note))
head = [l for l in open(P("after/part-k/listing-2.tsv"), encoding="utf-8") if l.startswith("#")]
w("# listing-2.tsv's header: %s" % ("every check held" if any(l.startswith("# every check held:") for l in head) else "NO CHECK RECORD"))
control = open(P("after/local/delsearch/control.txt"), encoding="utf-8").read()
w("# the control (after/local/delsearch/control.txt): %s" % (
    "reported, the revert equal to the commit's blob" if "-- the control is reported: True" in control
    and "equal to the commit's blob: True" in control else "NOT REPORTED"))

listing = {(r[1], r[2]): r for r in rows(P("after/part-k/listing.tsv"))}
listing2 = {(r[1], r[2]): r for r in rows(P("after/part-k/listing-2.tsv"))}
TEXT = {"text: a changelog line": "a line of a changelog",
        "text: a comment line": "a code comment",
        "text: a record line": "a line of a record",
        "text: a record line, naming another type's member of the same name": "a line of a record"}
kinds = collections.Counter()
bad = []
w("")
w("## part K's hits (after/part-k/hits.tsv), each read")
for x in rows(P("after/part-k/hits.tsv")):
    name, key, sort = x[1].split(" (")[0], x[2], x[3]
    if sort == "facade member":
        kinds["a facade member (hits.tsv's sort)"] += 1
        continue
    r = listing.get((name, key))
    if r is None:
        kinds["NOT ADMITTED: not in listing.tsv"] += 1
        bad.append((key, name, "not in listing.tsv"))
        continue
    what, decl = r[3], r[4]
    if what.startswith("the facade's own"):
        kinds["a facade member (listing.tsv: " + what + ")"] += 1
    elif what == STRING_KIND:
        r2 = listing2.get((name, key))
        if r2 and r2[0] == "text: words of a message string, in a message table" and r2[3].startswith("the message table ") and r2[4] == decl:
            kinds["words inside a string literal that is a message text, listed with its message table (listing-2.tsv)"] += 1
            w("2026-11-24-k\t%s\t%s\tmessage text\t%s\t%s" % (key, name, r2[3], r2[4]))
        else:
            kinds["NOT ADMITTED: " + what] += 1
            bad.append((key, name, what))
    elif what == "another type's member of the same name" and decl == "- :: -":
        r2 = listing2.get((name, key))
        if r2 and r2[0] == "a member of a .NET library type" and " in the reference assembly " in r2[4]:
            kinds["another type's member of the same name, shown by its .NET library type and assembly (listing-2.tsv)"] += 1
            w("2026-11-24-k\t%s\t%s\t.NET library type\t%s\t%s" % (key, name, r2[3], r2[4]))
        else:
            kinds["NOT ADMITTED: no declaration, and no library type and assembly"] += 1
            bad.append((key, name, what))
    elif what == "another type's member of the same name":
        kinds["another type's member of the same name, listed with its declaration"] += 1
    elif all(part in TEXT for part in what.split(" + ")):
        for part in what.split(" + "):
            kinds[TEXT[part]] += 1
    else:
        kinds["NOT ADMITTED: " + what] += 1
        bad.append((key, name, what))
for k, n in sorted(kinds.items()):
    w("%d\t%s" % (n, k))
w("# hits read NOT ADMITTED: %d" % len(bad))
for r in bad:
    w("NOT ADMITTED\t" + "\t".join(r))
print("\n".join(out))
