#!/usr/bin/env python3
"""Part K's reading aid on the search's narrowed scopes (bundle sjl-14-001, decision rule part K). Written 2026-11-24,
in review of the after readings.

    scope_k.py <repository> <out file>

The rule's configuration searches "over Broiler.JS/ and Broiler.ECMAScript/ outside docs/, each listed member's
declarations and references". The committed search (tools/deletion-search/search.py) searches a name that is a common
word of the engine's code in a narrower scope: the file or folder that declared the member at 33d3093b. This aid, read
at the after commit 83a9bd82 from its commit object (git grep, the search's own word options and build exclusions):
  1. lists every file of both trees outside docs/ that declares one of the listed types (FastParser, FastScanner,
     FastTokenStream, FastKeywordMap, CharExtensions, FastScope, FastScopeItem) at 33d3093b and at 83a9bd82, and says
     whether each lies inside the narrowed scope that type's names were searched in;
  2. for each name the search took in a narrowed scope, counts its word hits in the rule's scope, inside the narrowed
     scope and outside it, and checks the inside count against after/local/delsearch/search.txt;
  3. does the same for the direct-eval prefix's text, searched in the compiler's folder alone.
It judges nothing. Run with python3 -I."""
import re, subprocess, sys

repo, out = sys.argv[1:3]
COMMIT, LISTED_AT = "83a9bd82a5d258d3001468095ebf43f9efc3e9a7", "33d3093b"
PARSER, COMPILER = "Broiler.JS/Broiler.JavaScript.Parser", "Broiler.JS/Broiler.JavaScript.Compiler"
RULE = ["Broiler.JS", "Broiler.ECMAScript", ":!Broiler.JS/docs", ":!Broiler.ECMAScript/docs", ":!**/bin/**", ":!**/obj/**"]
TYPES = ["FastParser", "FastScanner", "FastTokenStream", "FastKeywordMap", "CharExtensions", "FastScope", "FastScopeItem"]
# The type each narrowed section's names belong to, and the scope search.py searched them in.
SCOPE = {"FastParser": PARSER, "FastScanner": PARSER + "/FastScanner.cs", "FastTokenStream": PARSER + "/FastTokenStream.cs",
         "FastKeywordMap": PARSER + "/FastKeywordMap.cs", "CharExtensions": PARSER, "FastScope": PARSER + "/FastScope*.cs",
         "FastScopeItem": PARSER + "/FastScope*.cs"}
SECTION_TYPE = {"The members FastParser's partial files": "FastParser", "FastScanner's scanning code: common names": "FastScanner",
                "FastTokenStream's scanning code": "FastTokenStream", "FastKeywordMap's table": "FastKeywordMap",
                "CharExtensions' scanner members": "CharExtensions", "The parser's half of FastScope": "FastScope"}
PREFIX = r'"\"use strict\";\n"'


def grep(commit, args, specs):
    r = subprocess.run(["git", "-C", repo, "grep", "-n", "-I", *args, commit, "--", *specs], capture_output=True, text=True)
    if r.returncode not in (0, 1):
        raise SystemExit(r.stderr)
    return [l.split(":", 1)[1] for l in r.stdout.splitlines()]


def inside(path, scope):
    if scope.endswith("*.cs"):
        return path.startswith(scope[:-4]) and path.endswith(".cs") and "/" not in path[len(PARSER) + 1:]
    return path == scope or path.startswith(scope + "/")


L = ["# scope_k.py: the deletion search's narrowed scopes read against the rule's scope, at " + COMMIT, ""]
L.append("## 1. Files declaring a listed type (class, struct, record, interface or enum), rule scope, code lines only")
for commit in (LISTED_AT, COMMIT):
    hits = grep(commit, ["-E", r"^\s*(public |internal |static |sealed |abstract |partial )*(class|struct|record|interface|enum)\s+(%s)\b" % "|".join(TYPES)], RULE)
    for h in sorted(hits):
        path, num, text = h.split(":", 2)
        t = re.search(r"(class|struct|record|interface|enum)\s+(\w+)", text).group(2)
        L.append("%s\t%s:%s\t%s\tnarrowed scope %s\t%s" % (commit[:8], path, num, t, SCOPE[t], "inside" if inside(path, SCOPE[t]) else "OUTSIDE"))
L.append("")
L.append("## 2. Each name searched in a narrowed scope: word hits in the rule's scope, inside and outside the narrowed scope")
L.append("section type\tname\tnarrowed scope\trule scope\tinside\toutside\tsearch.txt's count")
section, counts = None, {}
for line in open(repo + "/docs/evidence/sjl-14-001/after/local/delsearch/search.txt", encoding="utf-8"):
    if line.startswith("== "):
        section = next((t for k, t in SECTION_TYPE.items() if line[3:].startswith(k)), None)
        continue
    m = re.match(r"^-- (\S+)(?: \([^)]*\))?: (\d+) hit\(s\)", line)
    if m and section:
        name, listed = m.group(1), int(m.group(2))
        hits = grep(COMMIT, ["-w", "-e", name], RULE)
        n_in = sum(inside(h.split(":", 1)[0], SCOPE[section]) for h in hits)
        L.append("%s\t%s\t%s\t%d\t%d\t%d\t%d%s" % (section, name, SCOPE[section], len(hits), n_in, len(hits) - n_in, listed,
                                                  "" if n_in == listed else "\tINSIDE COUNT DIFFERS"))
        counts[section] = counts.get(section, 0) + len(hits) - n_in
L.append("")
L.append("## 3. The direct-eval prefix as text (fixed string), narrowed scope " + COMPILER)
hits = grep(COMMIT, ["-F", "-e", PREFIX], RULE)
L.append("rule scope %d; inside %d; outside %d" % (len(hits), sum(inside(h.split(":", 1)[0], COMPILER) for h in hits),
                                                 sum(not inside(h.split(":", 1)[0], COMPILER) for h in hits)))
L += ["  " + h for h in hits]
L.append("")
L.append("# outside the narrowed scope, by type: " + "; ".join("%s %d" % kv for kv in sorted(counts.items())))
open(out, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
print(L[-1])
