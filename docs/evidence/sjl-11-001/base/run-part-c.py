#!/usr/bin/env python3
"""partc.py <tree> <shell-dll> <out-dir>: bundle sjl-11-001's part C at one commit.

Each case of <tree>/docs/evidence/sjl-11-001/inputs/adversarial/manifest.tsv is run alone, one process of the shell
BroilerJS with --script-host, its program read from <tree> (the commit's own copy) and checked against the manifest's
SHA-256 first, under the manifest header's wall-clock limit, with LANG, LANGUAGE and every LC_* variable removed (the
invariant culture) and TZ=UTC. For each case: exit code, outcome, wall-clock seconds and the whole standard output and
error are retained. Outcome: the 'adversarial-outcome <id> ...' line's text (completed ... / threw ...); 'abnormal end'
when the process ends without that line or with a nonzero exit; 'past the wall-clock limit' when the limit is reached
(the process is then killed by this script, which started it).
"""
import hashlib, os, subprocess, sys, time

tree, shell, out = sys.argv[1], sys.argv[2], sys.argv[3]
folder = os.path.join(tree, "docs/evidence/sjl-11-001/inputs/adversarial")
os.makedirs(os.path.join(out, "cases"), exist_ok=True)
header, rows = {}, []
with open(os.path.join(folder, "manifest.tsv"), encoding="utf-8") as f:
    lines = f.read().split("\n")
for line in lines:
    if line.startswith("# ") and ": " in line:
        k, v = line[2:].split(": ", 1); header[k] = v
cols = None
for line in lines:
    if not line or line.startswith("#"): continue
    if cols is None: cols = line.split("\t"); continue
    rows.append(dict(zip(cols, line.split("\t"))))
limit = int(header["wall-clock-limit-seconds"])
env = {k: v for k, v in os.environ.items() if not (k in ("LANG", "LANGUAGE") or k.startswith("LC_"))}
env["TZ"] = "UTC"
res = open(os.path.join(out, "results.tsv"), "w", encoding="utf-8")
res.write("id\tclass\tdeclared-outcome\tprogram-sha256-checked\texit\twall-seconds\toutcome\tmatches-declared\n")
for r in rows:
    prog = os.path.join(folder, r["program"])
    digest = hashlib.sha256(open(prog, "rb").read()).hexdigest()
    if digest != r["program-sha256"]:
        res.write(f"{r['id']}\t{r['class']}\t{r['declared-outcome']}\tMISMATCH {digest}\t-\t-\tnot run\tno\n"); res.flush(); continue
    cmd = ["dotnet", shell, "--script-host", prog]
    t0 = time.monotonic()
    p = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=env, cwd=tree, start_new_session=True)
    timed_out = False
    try:
        so, se = p.communicate(timeout=limit)
    except subprocess.TimeoutExpired:
        timed_out = True
        p.kill(); so, se = p.communicate()
    wall = time.monotonic() - t0
    so_t, se_t = so.decode("utf-8", "replace"), se.decode("utf-8", "replace")
    with open(os.path.join(out, "cases", r["id"] + ".out.txt"), "w", encoding="utf-8") as g:
        g.write(f"# command (cwd {tree}): env -u LANG -u LANGUAGE -u LC_* TZ=UTC {' '.join(cmd)}\n")
        g.write(f"# program-sha256: {digest} (manifest: {r['program-sha256']})\n# exit: {p.returncode}\n# wall-seconds: {wall:.1f}\n# timed-out: {timed_out}\n")
        g.write("## stdout\n" + so_t + ("" if so_t.endswith("\n") or not so_t else "\n"))
        g.write("## stderr\n" + se_t + ("" if se_t.endswith("\n") or not se_t else "\n"))
    prefix = f"adversarial-outcome {r['id']} "
    found = [l[len(prefix):] for l in so_t.splitlines() if l.startswith(prefix)]
    if timed_out: outcome = "past the wall-clock limit"
    elif p.returncode != 0 or len(found) != 1: outcome = f"abnormal end (exit {p.returncode})"
    else: outcome = found[0]
    ok = "yes" if outcome == r["declared-outcome"] else "no"
    res.write(f"{r['id']}\t{r['class']}\t{r['declared-outcome']}\tok\t{p.returncode}\t{wall:.1f}\t{outcome}\t{ok}\n"); res.flush()
res.close()
print("cases", len(rows), "limit", limit)
