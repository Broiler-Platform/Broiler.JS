#!/usr/bin/env bash
# hosted.sh <outdir>: part G's hosted half of bundle sjl-13-001's part D (decision D-js-library-readings: Shared libraries'
# push run at the commit, both native-aot-probe legs, its first attempt deciding, every attempt retained), imported by GET
# only, as bundle sjl-13-001's library-commit/probe/workflow/ was: the run, its first attempt, the attempt's jobs, every
# job of every attempt (jobs?filter=all), the artifacts' listing, every job's log (gzip -n -9), and each artifact's zip
# checked against the API's digest and size (or, if the API's copy has expired, the copy downloaded and checked on
# 2026-11-28 into SCRATCH/sjl-rest/js-s158-pb/hosted/), its files kept as the shared workflow uploads them (each .tsv with
# gzip -n -9). Nothing is dispatched, cancelled or re-run. Written 2026-11-28.
set -u
OUT=$1; R=Broiler-Platform/Broiler.JS; RUN=46082299097; SHA=159fd811f74d0f43e7ed7327081267a96f8f1196
PB=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-s158-pb/hosted
W=$OUT/workflow; mkdir -p $W/jobs $W/artifacts $W/zips
L=$W/commands.txt
get() { local p=$1 f=$2 i; echo "$(date -u +%FT%TZ) gh api $p > $f" >> $L
        for i in 2 4 8 16 0; do gh api "$p" > "$f" && return 0; [ $i = 0 ] && return 1; sleep $i; done; }
get repos/$R/actions/runs/$RUN $W/run-$RUN.json || exit 2
get repos/$R/actions/runs/$RUN/attempts/1 $W/attempt-1.json || exit 2
get "repos/$R/actions/runs/$RUN/attempts/1/jobs?per_page=100" $W/attempt-1-jobs.json || exit 2
get "repos/$R/actions/runs/$RUN/jobs?filter=all&per_page=100" $W/jobs-all.json || exit 2
get "repos/$R/actions/runs/$RUN/artifacts?per_page=100" $W/artifacts.json || exit 2
python3 -I - $W <<'PY' > $W/run.txt
import json, sys
w = sys.argv[1]
run = json.load(open(f"{w}/run-46082299097.json"))
att = json.load(open(f"{w}/attempt-1.json"))
jobs = json.load(open(f"{w}/jobs-all.json"))["jobs"]
print(f"# run {run['id']} of {run['name']}: {run['html_url']}")
for k in ("head_sha", "head_branch", "event", "status", "conclusion", "run_attempt", "created_at", "updated_at"):
    print(f"{k}\t{run[k]}")
print(f"attempt-1\tstatus {att['status']}\tconclusion {att['conclusion']}\trun_attempt {att['run_attempt']}")
print("# job\tid\tname\tattempt\tconclusion\tstarted\tcompleted")
for j in jobs:
    print(f"job\t{j['id']}\t{j['name']}\t{j['run_attempt']}\t{j['conclusion']}\t{j['started_at']}\t{j['completed_at']}")
    for s in j.get("steps", []):
        print(f"step\t{j['id']}\t{s['number']}\t{s['name']}\t{s['conclusion']}")
PY
for id in $(python3 -I -c 'import json,sys; [print(j["id"]) for j in json.load(open(sys.argv[1]))["jobs"]]' $W/jobs-all.json); do
  get repos/$R/actions/jobs/$id/logs $W/jobs/$id.log || { echo "job $id log: not available" >> $L; continue; }
  gzip -n -9 -f $W/jobs/$id.log
done
python3 -I -c 'import json,sys
for a in json.load(open(sys.argv[1]))["artifacts"]:
    print(a["id"], a["name"], a.get("digest") or "-", a["size_in_bytes"], a["expired"])' $W/artifacts.json > $W/artifacts.list
printf 'artifact_id\tname\tapi_digest\tsource\tsha256\tbytes\tapi_size_in_bytes\tcheck\n' > $W/artifact-digests.tsv
while read id name digest size expired; do
  z=$W/zips/$id.zip; src=api
  if [ "$expired" = "False" ] && get repos/$R/actions/artifacts/$id/zip $z; then :; else src="js-s158-pb/hosted (downloaded 2026-11-28)"; cp $PB/$id.zip $z; fi
  got=sha256:$(sha256sum $z | cut -d' ' -f1); bytes=$(stat -c %s $z)
  chk=$([ "$got" = "$digest" ] && [ "$bytes" = "$size" ] && echo match || echo DIFFERENT)
  printf '%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\n' "$id" "$name" "$digest" "$src" "$got" "$bytes" "$size" "$chk" >> $W/artifact-digests.tsv
  mkdir -p $W/artifacts/$name; ( cd $W/artifacts/$name && unzip -o -q $z )
done < $W/artifacts.list
( cd $W/artifacts && find . -type f | LC_ALL=C sort | xargs sha256sum ) > $W/artifacts/sha256.txt
for f in $(find $W/artifacts -name '*.tsv'); do gzip -n -9 -f $f; done
rm -rf $W/artifacts/shared-packages $W/zips
echo "$(date -u +%FT%TZ) done; shared-packages' files listed in artifacts/sha256.txt and not kept (as bundle sjl-13-001's library commit kept them); the zips not kept" >> $L
