#!/usr/bin/env python3
"""Bundle sjl-13-001: apply the rule's third revision (decision-rule-revised-2026-11-03-third.md) to part D.

Usage: python3 -I partd.py <repo> [<commit>...]

With no commit, it walks `git rev-list --first-parent --merges --reverse <library commit>..origin/main` and reports
the first merge whose workflow file passes the revision's test: the part D commit. For each commit it reports the
test, and the condition: `git diff --name-only <library commit> <commit>` naming no held path (the three folders and
eng/Broiler.Packaging.props in any letter case; the nine configuration names as a path's last component, at any depth,
in any letter case), and every line `git diff -U0 <library commit> <commit> -- .github/workflows/shared-libraries.yml`
removes or adds being one the revision admits. It reads git and the workflow file alone, and writes to stdout.
It follows the script used when the part D commit was first read, before this run, with four changes: the
names are read NUL-separated, a step written without `name:` ends the step before it, the group line is admitted only
under `concurrency:`, and jobs are read only under `jobs:`.
"""
import re, subprocess, sys
import yaml

L = "d4b330d8ed25aeee0e7d5fe1e4129f3dc480b318"
WF = ".github/workflows/shared-libraries.yml"
NAMES = {n.lower() for n in [".gitattributes", ".editorconfig", ".globalconfig", "Directory.Build.props",
                             "Directory.Build.targets", "Directory.Build.rsp", "Directory.Packages.props",
                             "NuGet.config", "global.json"]}
FOLDERS = ("broiler.ecmascript/", "conformance/vectors/", "data/unicode/")
FILE = "eng/broiler.packaging.props"
GROUP_OLD = "  group: shared-${{ github.workflow }}-${{ github.ref }}-${{ inputs.package-version }}"
GROUP_NEW = ("  group: shared-${{ github.workflow }}-${{ github.ref }}-${{ inputs.package-version }}"
             "${{ github.event_name == 'push' && format('-{0}', github.sha) || '' }}")
JIT_STEPS = {"Publish with the JIT", "Run the corpus under the JIT publication, and compare"}
KEPT = {"probe-publish-jit.log", "probe-jit-published.tsv"}
repo = sys.argv[1]


def git(*a):
    return subprocess.run(["git", "-C", repo, *a], check=True, capture_output=True, text=True).stdout


def passes_test(c):
    """The revision's test: in job native-aot-probe, a step running `dotnet publish Broiler.ECMAScript/
    Broiler.ECMAScript.Probe` with -p:PublishAot=false, a step running that publication over conformance/vectors, and
    `cmp probe-aot.tsv` against that run's report."""
    try:
        doc = yaml.safe_load(git("show", f"{c}:{WF}"))
    except subprocess.CalledProcessError:
        return False, "no workflow file"
    steps = ((doc.get("jobs") or {}).get("native-aot-probe") or {}).get("steps") or []
    runs = [s.get("run") or "" for s in steps]
    out = None
    for r in runs:
        for line in r.splitlines():
            m = re.search(r"dotnet publish Broiler\.ECMAScript/Broiler\.ECMAScript\.Probe\b(.*)$", line)
            if m and "-p:PublishAot=false" in m.group(1):
                o = re.search(r"\s-o\s+(\S+)", m.group(1))
                out = o.group(1) if o else "Broiler.ECMAScript/Broiler.ECMAScript.Probe/bin/Release/net10.0/publish"
    if not out:
        return False, "no JIT publish step"
    for r in runs:
        if "dotnet publish" in r:
            continue
        for line in r.splitlines():
            if out + "/" in line and "conformance/vectors" in line:
                rep = re.search(r"--report\s+(\S+)", line)
                if rep and re.search(r"^\s*cmp probe-aot\.tsv " + re.escape(rep.group(1)) + r"\s*$", r, re.M):
                    return True, f"publish -o {out}; run over conformance/vectors --report {rep.group(1)}; cmp probe-aot.tsv {rep.group(1)}"
    return False, "no run of the JIT publication over conformance/vectors compared with probe-aot.tsv"


def path_held(p):
    q = p.lower()
    return q.startswith(FOLDERS) or q == FILE or p.rsplit("/", 1)[-1].lower() in NAMES


def owners(text):
    """Map each line number (1-based) of a workflow file to (top-level key, job, step name)."""
    own, top, job, step = {}, None, None, None
    steps_indent = item_indent = None
    for i, ln in enumerate(text.split("\n"), 1):
        stripped = ln.lstrip(" ")
        indent = len(ln) - len(stripped)
        if stripped and not stripped.startswith("#"):
            mt = re.match(r"^([A-Za-z0-9_-]+):", ln)
            mj = re.match(r"^  ([A-Za-z0-9_-]+):\s*$", ln)
            ms = re.match(r"^(\s*)- (.*)$", ln)
            if mt:
                top, job, step, steps_indent, item_indent = mt.group(1), None, None, None, None
            elif top == "jobs" and mj:
                job, step, steps_indent, item_indent = mj.group(1), None, None, None
            elif job and steps_indent is None and re.match(r"^\s*steps:\s*$", ln):
                steps_indent, item_indent, step = indent, None, None
            elif steps_indent is not None and indent <= steps_indent and not (ms and indent == steps_indent):
                steps_indent, item_indent, step = None, None, None
            elif steps_indent is not None and ms and (item_indent is None or indent == item_indent):
                # a new step: its name, if this line gives it; otherwise none until a `name:` key of its own
                item_indent = indent
                n = re.match(r"name:\s*(.*)$", ms.group(2))
                step = n.group(1).strip() if n else None
            elif item_indent is not None and step is None and indent == item_indent + 2:
                n = re.match(r"^\s*name:\s*(.*)$", ln)
                if n:
                    step = n.group(1).strip()
        own[i] = (top, job, step)
    return own


def condition(c):
    names = [p for p in git("diff", "-z", "--name-only", L, c).split("\0") if p]
    held = [p for p in names if path_held(p)]
    new_own = owners(git("show", f"{c}:{WF}"))
    old_own = owners(git("show", f"{L}:{WF}"))
    diff = git("diff", "-U0", L, c, "--", WF).split("\n")
    lines, outside = [], []
    ol = nl = None
    for ln in diff:
        h = re.match(r"^@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@", ln)
        if h:
            ol, nl = int(h.group(1)), int(h.group(2))
            continue
        if ln.startswith(("--- ", "+++ ", "diff ", "index ")) or not ln or ln[0] not in "+-":
            continue
        body = ln[1:]
        comment = body.lstrip(" ").startswith("#")
        if ln[0] == "-":
            where = old_own.get(ol, (None, None, None))
            why = ("comment" if comment else
                   "the group line under concurrency:, removed" if where[0] == "concurrency" and body == GROUP_OLD else None)
            ol += 1
        else:
            where = new_own.get(nl, (None, None, None))
            why = ("comment" if comment else
                   "the group line under concurrency:, added" if where[0] == "concurrency" and body == GROUP_NEW else
                   f"a line of step \"{where[2]}\" in job native-aot-probe" if where[1] == "native-aot-probe" and where[2] in JIT_STEPS else
                   "a kept path in step \"Keep the logs\" of job native-aot-probe"
                   if where[1] == "native-aot-probe" and where[2] == "Keep the logs" and body.strip() in KEPT else None)
            nl += 1
        lines.append((ln, why, where))
        if why is None:
            outside.append(ln)
    return names, held, lines, outside


def report(c):
    full = git("rev-parse", c).strip()
    t, how = passes_test(full)
    names, held, lines, outside = condition(full)
    print(f"commit {full}")
    print(f"  test: {'passes' if t else 'fails'} ({how})")
    print(f"  git diff --name-only {L} {full}: {len(names)} paths; held paths named: {len(held)}")
    for p in held:
        print(f"    held: {p}")
    print(f"  git diff -U0 of {WF}: {len(lines)} lines removed or added; outside the admitted set: {len(outside)}")
    for ln, why, where in lines:
        print(f"    {ln[0]} {why or 'NOT ADMITTED'} | job {where[1]} step {where[2]} | {ln[1:]}")
    holds = not held and not outside
    print(f"  condition: {'holds' if holds else 'fails'}")
    return t, holds


if len(sys.argv) > 2:
    for c in sys.argv[2:]:
        report(c)
else:
    merges = git("rev-list", "--first-parent", "--merges", "--reverse", f"{L}..origin/main").split()
    print(f"git rev-list --first-parent --merges --reverse {L}..origin/main: {len(merges)} merges")
    found = None
    for m in merges:
        t, how = passes_test(m)
        print(f"  {m}: test {'passes' if t else 'fails'} ({how})")
        if t and found is None:
            found = m
            break
    print(f"the part D commit: {found or 'none'}")
    if found:
        report(found)

print("path predicate, checked on names that are and are not held:")
for p in ["conformance/.gitattributes", "data/.gitattributes", "Broiler.JS/Directory.Build.props", "x/y/nuget.CONFIG",
          "GLOBAL.JSON", "docs/a/.editorconfig", "src/.GlobalConfig", "Directory.Build.rsp", "ENG/broiler.packaging.PROPS",
          "data/unicode/pins/unicode.pin", "broiler.ecmascript/Broiler.ECMAScript.Probe/Extra.cs",
          "conformance/Vectors/x.vectors", "docs/evidence/sjl-13-001/README.md", "docs/publishing.md", "eng/other.props"]:
    print(f"  {p}: {'held' if path_held(p) else 'not held'}")
