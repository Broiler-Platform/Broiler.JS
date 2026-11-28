import sys, re, os
# links.py <repo root> <file...>: every relative markdown link target (path part) in each file must exist.
root = sys.argv[1]; bad = 0; n = 0
for f in sys.argv[2:]:
    p = os.path.join(root, f); d = os.path.dirname(p)
    text = open(p, encoding="utf-8").read()
    for m in re.finditer(r"\]\(([^)\s]+)\)", text):
        t = m.group(1)
        if re.match(r"[a-z]+:", t) or t.startswith("#"): continue
        path = t.split("#")[0]
        n += 1
        if not os.path.exists(os.path.normpath(os.path.join(d, path))):
            bad += 1; print("missing", f, t)
print("links checked", n, "missing", bad)
sys.exit(1 if bad else 0)
