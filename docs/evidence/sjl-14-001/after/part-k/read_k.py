#!/usr/bin/env python3
"""Part K's reading aid (bundle sjl-14-001, decision rule part K): every hit the after run's deletion search listed
(after/local/delsearch/search.txt), one row each, with what it is, as tools/deletion-search/README.md says the part's
reading sorts them ("which hits are facade members, the compiler's own ContainsLegacyOctalLiteral that stays until
SJL-15.5, other types' members of the same name, or text"). Written 2026-11-24.

    read_k.py <search.txt> <repo root> <out file>

Each hit's line is read again from the after commit's tree (<repo root>, a checkout of 83a9bd82) and must be the text
the search printed (the first 200 characters). The sort, by the file and the line's own text:
  text: record     a line of a Markdown file, a JSON manifest or a PublicAPI.txt list (prose and records, not code)
  text: comment    a code line that is a comment (it begins with //, /// or *)
  facade member    a code line of the adapter Parser package's facade files (FastParser.cs, FastScanner.cs,
                   FastTokenStream.cs, FastScope.cs, FastScopeItem.cs, FastKeywordMap.cs, CharExtensions.cs), a public
                   member dispositions.md keeps as a facade over the bridge or a use of one inside it, but the lines
                   OTHER names below
  the bridge's own a code line of Broiler.JavaScript.Parser/Bridge/: the bridge's own member of the same name
  the library's    a code line under Broiler.ECMAScript/: the shared library's own member of the same name
  kept compiler    FastCompiler.EarlyErrors' own ContainsLegacyOctalLiteral and its helper, which dispositions.md
                   keeps ("not a re-scan and stays until SJL-15.5")
  OTHER            named one by one below with what the line holds; anything else is "unread", which the reading names
Writes one row per hit (section, name, file:line, sort, the line) and the counts. Run with python3 -I."""
import collections, os, re, sys
search, root, out = sys.argv[1:4]
FACADE = {"FastParser.cs", "FastScanner.cs", "FastTokenStream.cs", "FastScope.cs", "FastScopeItem.cs", "FastKeywordMap.cs", "CharExtensions.cs"}
OTHER = {
    "Broiler.JS/Broiler.JavaScript.Parser/FastScanner.cs:71": "another type's member of the same name: the bridge's token chain's Next, read by the facade",
    "Broiler.JS/Broiler.JavaScript.Parser/FastScanner.cs:105": "the facade's own: State's constructor parameter named position (a facade member), not the deleted cursor field",
    "Broiler.JS/Broiler.JavaScript.Parser/FastScanner.cs:114": "the facade's own: State's constructor parameter named position, not the deleted cursor field",
    "Broiler.JS/Broiler.JavaScript.Parser/FastScanner.cs:122": "the facade's own: State's constructor parameter named position, not the deleted cursor field",
    "Broiler.JS/Broiler.JavaScript.Parser/FastScanner.cs:125": "the facade's own: State's constructor parameter named position, not the deleted cursor field",
    "Broiler.JS/Broiler.JavaScript.Parser/FastScanner.cs:129": "the facade's own: State's constructor parameter named position, not the deleted cursor field",
    "Broiler.JS/Broiler.JavaScript.Parser/FastTokenStream.cs:321": "another type's member of the same name: the enum member TokenTypes.LineTerminator, not FastTokenStream's deleted LineTerminator",
    "Broiler.JS/Broiler.JavaScript.Parser/FastParser.cs:158": "another type's member of the same name: AstExpressionStatement's Expression, in a property pattern of the facade",
    "Broiler.JS/Broiler.JavaScript.Parser/FastParserOptions.cs:112": "another type's member of the same name: System.HashCode.Combine",
}
rows, section, name = [], "", ""
for line in open(search, encoding="utf-8"):
    line = line.rstrip("\n")
    if line.startswith("== "): section = line[3:]; continue
    if line.startswith("-- "): name = line[3:].split(":")[0]; continue
    if not line.startswith("   "): continue
    hit = line[3:]
    m = re.match(r"^(.+?):(\d+):(.*)$", hit)
    path, num, text = m.group(1), int(m.group(2)), m.group(3)
    src = open(os.path.join(root, path), encoding="utf-8", errors="replace").read().split("\n")[num - 1]
    same = src[:200].rstrip() == text[:200].rstrip() or src.lstrip("﻿")[:200].rstrip() == text[:200].rstrip()
    code = src.strip()
    key = f"{path}:{num}"
    if path.endswith((".md", ".json")) or path.endswith("PublicAPI.txt"):
        sort = "text: record"
    elif code.startswith(("//", "*", "/*")):
        sort = "text: comment"
    elif key in OTHER:
        sort = "OTHER: " + OTHER[key]
    elif path.startswith("Broiler.JS/Broiler.JavaScript.Parser/") and os.path.basename(path) in FACADE:
        sort = "facade member"
    elif path.startswith("Broiler.JS/Broiler.JavaScript.Parser/Bridge/"):
        sort = "the bridge's own member of the same name"
    elif path.startswith("Broiler.ECMAScript/"):
        sort = "the library's own member of the same name"
    elif path == "Broiler.JS/Broiler.JavaScript.Compiler/FastCompiler.EarlyErrors.cs" and name in ("ContainsLegacyOctalLiteral", "TryGetLegacyOctalToken"):
        sort = "kept compiler: FastCompiler.EarlyErrors' own, which dispositions.md keeps until SJL-15.5"
    else:
        sort = "unread"
    rows.append((section.split(" (scope")[0], name, key, sort, "same" if same else "DIFFERS FROM THE TREE", hit[len(key) + 1:][:200]))
c = collections.Counter(r[3].split(":")[0] if r[3].startswith(("OTHER", "kept")) else r[3] for r in rows)
L = ["# read_k.py: every hit of after/local/delsearch/search.txt, with its sort; the line read again from the tree of 83a9bd82",
     "section\tname\tfile:line\tsort\tline as in the tree\tline"]
L += ["\t".join(r) for r in rows]
L.append("# hits: %d; by sort: %s" % (len(rows), "; ".join("%s %d" % kv for kv in sorted(c.items()))))
L.append("# lines that differ from the tree: %d; unread: %d" % (sum(r[4] != "same" for r in rows), c.get("unread", 0)))
open(out, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
print(L[-2]); print(L[-1])
for r in rows:
    if r[3] in ("unread",) or r[4] != "same": print(r)
