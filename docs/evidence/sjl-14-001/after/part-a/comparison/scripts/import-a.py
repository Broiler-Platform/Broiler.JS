#!/usr/bin/env python3
"""import-a.py: part A's after run 45264304825 into after/part-a/, as base/part-a/ holds the base run: every entry of the
artifact test262-evidence unchanged (each compared with its zip entry), but test262-results.json.gz, over 2 MB, which the
base kept by digest only and which is kept the same way here; runner-images.txt by the base's procedure
(js-w2baseA/mk_images.py); the run's artifacts as GitHub records them (part-a-artifacts.txt). imported.txt, hashes.txt and
.gitattributes are written separately."""
import hashlib, json, os, zipfile
R = "/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2read/parta"
OUT = "/home/user/js-w2read/docs/evidence/sjl-14-001/after/part-a"
READ = open(R + "/read-at.txt").read().strip()[:16] + "Z"
LOGREAD = open(R + "/joblogs/read-at.txt").read().strip()[:16] + "Z"
os.makedirs(OUT, exist_ok=False)
z = zipfile.ZipFile(R + "/after/test262-evidence.zip")
names = sorted(i.filename for i in z.infolist() if not i.is_dir())
kept = 0
for n in names:
    if n == "test262-results.json.gz":
        continue
    p = os.path.join(OUT, n); os.makedirs(os.path.dirname(p), exist_ok=True)
    data = z.read(n); open(p, "wb").write(data)
    assert open(p, "rb").read() == data
    kept += 1
print("artifact entries:", len(names), "kept whole:", kept)
jobs = json.load(open(R + "/after/jobs-api.json"))["jobs"]
by = {j["name"]: j["id"] for j in jobs}
out = ["# runner-images.txt: the runner image of each of the nine jobs that ran a shard or the merge (job collect), as the head of",
       "# the job's own log reads it (GitHub's job logs, read %s), copied verbatim from its first line to the end of its \"Runner Image\" group." % LOGREAD,
       "# Added at import; not a file of the artifact."]
for n in [f"shard ({i})" for i in range(8)] + ["collect"]:
    lines = open(f"{R}/joblogs/{by[n]}.log", encoding="utf-8-sig").read().split("\n")
    out.append(""); out.append(f"## job: {n}")
    ingroup = False
    for l in lines:
        out.append(l.rstrip("\r"))
        if l.rstrip("\r").endswith("##[group]Runner Image"): ingroup = True
        elif ingroup and l.rstrip("\r").endswith("##[endgroup]"): break
    else:
        raise SystemExit("no Runner Image group in " + n)
open(OUT + "/runner-images.txt", "w", encoding="utf-8", newline="\n").write("\n".join(out) + "\n")
arts = json.load(open(R + "/after/artifacts.json"))["artifacts"]
lines = [f"# The run's artifacts as GitHub records them (read {READ}): id, name, bytes, digest; each zip was downloaded and its SHA-256 equals the digest."]
for a in sorted(arts, key=lambda a: a["name"]):
    f = R + "/after/" + a["name"] + ".zip"
    h = hashlib.sha256(open(f, "rb").read()).hexdigest()
    assert a["digest"] == "sha256:" + h and os.path.getsize(f) == a["size_in_bytes"], a["name"]
    lines.append(f"{a['id']} {a['name']} {a['size_in_bytes']} {a['digest']}")
open(OUT + "/part-a-artifacts.txt", "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
print("artifacts:", len(arts))
