#!/usr/bin/env bash
# ctest.sh <full-sha> <outdir> <label> [patch-file|-] [unpatched listing]: part F's library-level run (bundle
# sjl-15-001's rule, part F: "Broiler.ECMAScript.Conformance.Tests' library-level run"), with the per-vector listing of
# decision D-js-vector-listing kept in the run's trx log. From a clean `git archive` of the commit under
# /home/user/js-s158-lib-build/<label>/src, an optional patch applied (patch -p1; a scratch copy, never committed):
# Broiler.ECMAScript.Conformance.Tests built in Release with no build server and tested with a trx logger (run/run.trx),
# and the listing extracted from the listing test's output in the trx (listing.txt). With a patch (the rule's part F
# control), the patched files are then restored from the commit (git show), Broiler.ECMAScript/ compared with a fresh
# archive's (revert-check.txt), the project built again and the run taken again (revert/), and the reverted run's whole
# listing compared byte for byte with the unpatched run's (the fifth argument). Adapted 2026-11-28 from stage 8's
# rehearsal script SCRATCH/sjl-rest/js-s158/scripts/ctest.sh: every command now under `env -i` (lib.sh), the worktree,
# build tree and output root this stage's, and the revert's listing compared here. Through run.sh.
set -u
source /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-s158-lib/scripts/lib.sh
SHA=$1; OUT=$2; LABEL=$3; PATCHFILE=${4:--}; UNPATCHED=${5:-}
BLD=$BROOT/$LABEL; SRC=$BLD/src; L=$OUT/logs
mkdir -p $OUT $L; setenv $BLD
newtree $BLD $SHA $L/disk.log || exit 3
{ echo "commit: $(git -C $REPO rev-parse $SHA)"; echo "tree: $(git -C $REPO rev-parse $SHA^{tree})"; echo "patch: $PATCHFILE"; echo "sdk: $("${RUNENV[@]}" dotnet --version)"; echo "started: $(date -u +%FT%TZ)"; } > $L/commit.txt
cd $SRC
if [ "$PATCHFILE" != "-" ]; then cp $PATCHFILE $OUT/applied.patch; patch -p1 --no-backup-if-mismatch < $PATCHFILE > $L/patch.txt 2>&1 || { echo "patch failed" >> $L/commit.txt; deltree $BLD $L/disk.log; exit 9; }; fi
C=Broiler.ECMAScript/Broiler.ECMAScript.Conformance.Tests
extract() { "${RUNENV[@]}" python3 -I - "$1" "$2" <<'PY'
import sys, xml.etree.ElementTree as ET
trx, out = sys.argv[1], sys.argv[2]
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
root = ET.parse(trx).getroot()
found = None
for r in root.iter("{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult"):
    if r.get("testName", "").endswith("Each_answered_semantics_vector_s_verdict_at_the_parse_is_listed"):
        so = r.find("t:Output/t:StdOut", ns)
        found = (r.get("outcome"), so.text if so is not None else None)
lines = []
if found and found[1]:
    take = False
    for line in found[1].replace("\r\n", "\n").split("\n"):
        if line.startswith("# begin: the answered semantics vectors"):
            take = True; continue
        if line.startswith("# end: the answered semantics vectors"):
            break
        if take: lines.append(line)
with open(out, "w", newline="\n") as h:
    h.write("".join(l + "\n" for l in lines))
print("listing test outcome:", found[0] if found else "not found", "lines:", len(lines))
PY
}
rc=0
diskok build $L/disk.log
T $L build "${RUNENV[@]}" dotnet build $C -c Release --nologo --disable-build-servers || rc=1
grep -E " warning [A-Z]+[0-9]+" $L/build.stdout.txt | sort -u > $OUT/build-warnings.txt
mkdir -p $OUT/run
diskok run $L/disk.log
T $L test "${RUNENV[@]}" dotnet test $C -c Release --no-build --nologo --logger "trx;LogFileName=run.trx" --logger "console;verbosity=normal" --results-directory $OUT/run; echo "test rc=$?" >> $L/commit.txt
extract $OUT/run/run.trx $OUT/listing.txt > $OUT/extract.txt 2>&1
if [ "$PATCHFILE" != "-" ]; then
  for f in $(grep '^+++ ' $PATCHFILE | awk '{print $2}' | sed 's|^b/||'); do git -C $REPO show $SHA:$f > $f; echo "restored $f from $SHA" >> $OUT/revert-check.txt; done
  mkdir -p $BLD/fresh; git -C $REPO archive --format=tar $SHA Broiler.ECMAScript | tar -xf - -C $BLD/fresh
  diff -r -q -x bin -x obj -x TestResults Broiler.ECMAScript $BLD/fresh/Broiler.ECMAScript >> $OUT/revert-check.txt 2>&1 && echo "Broiler.ECMAScript/ equals a fresh archive's (bin, obj, TestResults aside)" >> $OUT/revert-check.txt
  rm -rf $BLD/fresh
  diskok revert-build $L/disk.log
  T $L revert-build "${RUNENV[@]}" dotnet build $C -c Release --nologo --disable-build-servers || rc=2
  mkdir -p $OUT/revert
  diskok revert-run $L/disk.log
  T $L revert-test "${RUNENV[@]}" dotnet test $C -c Release --no-build --nologo --logger "trx;LogFileName=run.trx" --logger "console;verbosity=normal" --results-directory $OUT/revert; echo "revert test rc=$?" >> $L/commit.txt
  extract $OUT/revert/run.trx $OUT/revert/listing.txt > $OUT/revert/extract.txt 2>&1
  if [ -n "$UNPATCHED" ]; then
    if cmp $UNPATCHED $OUT/revert/listing.txt > $OUT/revert-listing-against-run.txt 2>&1; then
      echo "the reverted run's listing equals the unpatched run's ($UNPATCHED) byte for byte" >> $OUT/revert-listing-against-run.txt
    else echo "DIFFERENT" >> $OUT/revert-listing-against-run.txt; rc=5; fi
  fi
fi
echo "ended: $(date -u +%FT%TZ) rc=$rc" >> $L/commit.txt
deltree $BLD $L/disk.log
exit $rc
