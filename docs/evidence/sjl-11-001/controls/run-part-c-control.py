#!/usr/bin/env python3
"""run-part-c-control.py <tree> <shell-dll> <out-dir> <class>: base/run-part-c.py, byte for byte, over the cases of one
class only (part C's control reads the M cases). It runs that script with its manifest filtered: a copy of the
manifest holding the header and the rows of <class> is written to a scratch copy of the inputs folder, and the
programs are read from there, each still checked against the manifest's SHA-256."""
import os, shutil, subprocess, sys
tree, shell, out, klass = sys.argv[1:5]
src = os.path.join(tree, "docs/evidence/sjl-11-001/inputs/adversarial")
scratch = os.path.join(out, "_tree")
dst = os.path.join(scratch, "docs/evidence/sjl-11-001/inputs/adversarial")
os.makedirs(dst)
lines = open(os.path.join(src, "manifest.tsv"), encoding="utf-8").read().split("\n")
keep, cols = [], None
for l in lines:
    if not l or l.startswith("#"): keep.append(l); continue
    if cols is None: cols = l; keep.append(l); continue
    r = l.split("\t")
    if r[1] == klass:
        keep.append(l); shutil.copy(os.path.join(src, r[3]), os.path.join(dst, r[3]))
open(os.path.join(dst, "manifest.tsv"), "w", encoding="utf-8").write("\n".join(keep))
rc = subprocess.run([sys.executable, "-I", "/home/user/js-115after/docs/evidence/sjl-11-001/base/run-part-c.py", scratch, shell, out]).returncode
shutil.rmtree(scratch)
sys.exit(rc)
