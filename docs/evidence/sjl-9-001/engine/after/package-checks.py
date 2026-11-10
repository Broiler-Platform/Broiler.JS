"""Package checks for bundle sjl-9-001 at the after commit: the packages the pristine consumer packed.
For each package of the Intl area (Broiler.ECMAScript.Intl, Broiler.ECMAScript.Algorithms, Broiler.JavaScript.BuiltIns):
its dependencies as its nuspec declares them, its entries, and whether any entry holds 32 bytes taken from the start,
the middle or the end of any table under Broiler.ECMAScript/data/intl/generated/ (the tables the engine embeds)."""
import sys, zipfile, re, hashlib
from pathlib import Path
feed, tree = Path(sys.argv[1]), Path(sys.argv[2])
tables = sorted((tree / "Broiler.ECMAScript/data/intl/generated").iterdir())
probes = []
for t in tables:
    b = t.read_bytes()
    if len(b) < 96: continue
    for where, off in (("start", 0), ("middle", len(b)//2), ("end", len(b)-32)):
        probes.append((t.name, where, b[off:off+32]))
print(f"tables: {len(tables)} files under Broiler.ECMAScript/data/intl/generated/; probes: {len(probes)} windows of 32 bytes")
for name in ("Broiler.ECMAScript.Intl", "Broiler.ECMAScript.Algorithms", "Broiler.JavaScript.BuiltIns"):
    pkg = next(feed.glob(name + ".0.*.nupkg"))
    with zipfile.ZipFile(pkg) as z:
        nuspec = next(n for n in z.namelist() if n.endswith(".nuspec"))
        deps = sorted(set(re.findall(r'<dependency id="([^"]+)" version="([^"]+)"', z.read(nuspec).decode())))
        hits = []
        for n in z.namelist():
            data = z.read(n)
            for tname, where, window in probes:
                if window in data: hits.append((n, tname, where))
        print(f"\n{pkg.name} (sha256 {hashlib.sha256(pkg.read_bytes()).hexdigest()})")
        print("  dependencies: " + ", ".join(f"{i} {v}" for i, v in deps))
        print("  entries: " + ", ".join(sorted(n for n in z.namelist() if not n.startswith(("_rels/", "package/", "[Content_Types]")))))
        print(f"  entries holding a window of a generated table: {len(hits)}")
        tablesin = sorted({t for _, t, _ in hits})
        if hits: print("  tables found: " + ", ".join(tablesin) + f" ({len(tablesin)} of {len(tables)}), in " + ", ".join(sorted({n for n, _, _ in hits})))
