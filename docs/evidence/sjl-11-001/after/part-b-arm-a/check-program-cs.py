#!/usr/bin/env python3
"""Part B's arm A precondition under decision-rule-revised-2026-11-19.md. Runs nothing; reads git objects only.

    python3 -I check-program-cs.py --repo <a clone of Broiler.JS> --after <the after commit>

Arm A builds the after commit with RegExpBenchmarks.cs as the inputs commit has it and the benchmark Program.cs as the
after commit has it. Before any lane runs, the revision requires that the after commit's Program.cs differs from the
inputs commit's only by the removal of the --regex-tiering block, and that its --regexp-answers path and the benchmark
entry are byte-identical. This script checks, from git:
  1. the inputs commit's Program.cs is blob 6074646340c64176f4c918b1c963dbd1347cdcff;
  2. the inputs commit's Program.cs with its one --regex-tiering block removed (the if-statement whose condition is
     args.Length == 1 && args[0] == "--regex-tiering", from its first line to its closing brace, and the blank line
     after it) is, byte for byte, the after commit's Program.cs;
  3. the --regexp-answers block (its if-statement, first line to closing brace) and the benchmark entry (the
     statement from "BenchmarkSwitcher" to ".Run(args, BenchmarkConfiguration.Create());") are byte-identical in both,
     each found exactly once;
  4. RegExpBenchmarks.cs is the same blob at the inputs commit and the after commit (reported; arm A uses the inputs
     commit's, so a difference is reported and not refused).
It prints each check and exits 0 only if 1 to 3 hold; otherwise it prints REFUSED and exits 1, and the run stops.
"""
import argparse, hashlib, subprocess, sys

INPUTS = "24fe7cba2dadbadb5917cf20e6c884d1ddabec85"
INPUTS_BLOB = "6074646340c64176f4c918b1c963dbd1347cdcff"
DIR = "Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks"

def git(repo, *a):
    return subprocess.run(["git", "-C", repo, *a], check=True, capture_output=True).stdout

def block(lines, head):
    """The if-statement starting at the line equal to head, through its closing brace at the same indent."""
    at = [i for i, l in enumerate(lines) if l == head]
    if len(at) != 1:
        return None, len(at)
    i = at[0]
    indent = head[: len(head) - len(head.lstrip())]
    if lines[i + 1] != indent + b"{":
        return None, -1
    j = i + 2
    while lines[j] != indent + b"}":
        j += 1
    return (i, j), 1

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--repo", required=True)
    ap.add_argument("--after", required=True)
    a = ap.parse_args()
    after = git(a.repo, "rev-parse", a.after + "^{commit}").decode().strip()
    ok = True
    def say(cond, text):
        nonlocal ok
        ok = ok and cond
        print(("yes  " if cond else "NO   ") + text)
    print(f"inputs commit {INPUTS}; after commit {after}")
    ib = git(a.repo, "rev-parse", f"{INPUTS}:{DIR}/Program.cs").decode().strip()
    ab = git(a.repo, "rev-parse", f"{after}:{DIR}/Program.cs").decode().strip()
    print(f"Program.cs: inputs blob {ib}; after blob {ab}")
    say(ib == INPUTS_BLOB, f"1. the inputs commit's Program.cs is blob {INPUTS_BLOB}")
    it = git(a.repo, "show", f"{INPUTS}:{DIR}/Program.cs")
    at = git(a.repo, "show", f"{after}:{DIR}/Program.cs")
    il = it.split(b"\n")
    al = at.split(b"\n")
    tier_head = b'        if (args.Length == 1 && args[0] == "--regex-tiering")'
    span, n = block(il, tier_head)
    say(span is not None, f"2a. the inputs file holds exactly one --regex-tiering block (found {n})")
    if span is not None:
        i, j = span
        removed = il[i : j + 1]
        if j + 1 < len(il) and il[j + 1] == b"":
            removed = il[i : j + 2]
        rest = il[:i] + il[i + len(removed):]
        print(f"   the block: inputs lines {i + 1} to {i + len(removed)} ({len(removed)} lines, the last blank), sha256 "
              f"{hashlib.sha256(b'\n'.join(removed) + b'\n').hexdigest()}")
        for l in removed:
            print("   | " + l.decode())
        say(b"\n".join(rest) == at, "2b. the inputs file with that block removed is the after file, byte for byte")
    span_a, _ = block(al, tier_head)
    say(span_a is None, "2c. the after file holds no --regex-tiering block")
    ans_head = b'        if (args.Length == 1 && args[0] == "--regexp-answers")'
    si, ni = block(il, ans_head)
    sa, na = block(al, ans_head)
    good = si is not None and sa is not None
    if good:
        bi = b"\n".join(il[si[0] : si[1] + 1]); ba = b"\n".join(al[sa[0] : sa[1] + 1])
        say(bi == ba, f"3a. the --regexp-answers block, once in each (inputs lines {si[0] + 1}-{si[1] + 1}, after lines "
            f"{sa[0] + 1}-{sa[1] + 1}), is byte-identical; sha256 {hashlib.sha256(ba).hexdigest()}")
    else:
        say(False, f"3a. the --regexp-answers block once in each (found {ni} and {na})")
    def entry(lines):
        s = [k for k, l in enumerate(lines) if l.strip() == b"BenchmarkSwitcher"]
        e = [k for k, l in enumerate(lines) if l.strip() == b".Run(args, BenchmarkConfiguration.Create());"]
        if len(s) != 1 or len(e) != 1 or e[0] < s[0]:
            return None
        return s[0], e[0]
    ei, ea = entry(il), entry(al)
    if ei and ea:
        bi = b"\n".join(il[ei[0] : ei[1] + 1]); ba = b"\n".join(al[ea[0] : ea[1] + 1])
        say(bi == ba, f"3b. the benchmark entry, once in each (inputs lines {ei[0] + 1}-{ei[1] + 1}, after lines "
            f"{ea[0] + 1}-{ea[1] + 1}), is byte-identical; sha256 {hashlib.sha256(ba).hexdigest()}")
    else:
        say(False, "3b. the benchmark entry once in each")
    ri = git(a.repo, "rev-parse", f"{INPUTS}:{DIR}/RegExpBenchmarks.cs").decode().strip()
    ra = git(a.repo, "rev-parse", f"{after}:{DIR}/RegExpBenchmarks.cs").decode().strip()
    print(("same " if ri == ra else "DIFF ") + f"4. RegExpBenchmarks.cs: inputs blob {ri}; after blob {ra} (arm A uses the inputs commit's)")
    print("the condition holds" if ok else "REFUSED: the condition does not hold; the run stops and the question goes back to the owner")
    return 0 if ok else 1

if __name__ == "__main__":
    sys.exit(main())
