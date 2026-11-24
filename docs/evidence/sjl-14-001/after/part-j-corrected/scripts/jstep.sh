#!/usr/bin/env bash
# jstep.sh <step>: part J's corrected session at the after commit 83a9bd82 (bundle sjl-14-001, the owner's answer 14 of
# 2026-11-24), run through run.sh's lock and idle gate (run.sh <label> <logdir> /tmp -- bash jstep.sh <step>), exactly as
# the after driver's `fuzz` and `fcontrol` steps ran the first session (js-w2after/scripts/astep.sh, retained as
# docs/evidence/sjl-14-001/after/local/scripts/), with one change of input: a clean `git archive` of the after commit on
# disk, with the corrected tool's files (docs/evidence/sjl-14-001/tools/fuzz/ at the tool commit $TOOL) written over
# their paths from their blobs and recorded (overlay.txt); Release, no build server; the session under the drivers'
# RUNENV (env -i, TZ=UTC, no LANG, LC_* or BROILER_*), with the same seed list, test262 suite, bound and session seed
# (the program's default; no --session-seed, as the first session).
#   fuzz      the corrected session over inputs/fuzz-seeds.txt (session/)
#   fcontrol  part J's control, as the first: in a scratch copy, BridgeTreeBuilder.Template throws on a template
#             literal (control.patch), built and run as fuzz; the file written again from the after commit's blob, its
#             digest checked, and the copy compared with a clean archive of the after commit (revert-check.txt).
#             One departure of order from astep.sh, for the disk rule (8 GiB free before any build): the clean archive
#             is taken after the session and the build output's removal, not before the build; the comparison is the
#             same, diff -r -q -x bin -x obj of the reverted copy against a clean archive of the after commit.
set -u
STEP=$1
SP=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad
R=$SP/sjl-rest/js-w2read2; REPO=/home/user/js-w2read; BLD=/home/user/js-w2read2-build
SHA=83a9bd82a5d258d3001468095ebf43f9efc3e9a7; C8=83a9bd82; TOOL=${TOOL:?the tool commit}
SUITE=/home/user/js-w2after-build/suite/test262-ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
TD=docs/evidence/sjl-14-001/tools/fuzz
export PATH=/opt/node22/bin:/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false
RUNENV=(env -i PATH=/opt/node22/bin:/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC)
BUILD=(dotnet build -c Release -nologo --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false)
S=$R/runs/$STEP; L=$S/logs; mkdir -p $L
W=$BLD/$C8/$STEP; ART=$BLD/$C8/art-$STEP; CLEAN=$BLD/$C8/fcontrol-clean
T() { local label=$1; shift; local s=$(date -u +%FT%TZ); printf '%s %s: %s\n' "$s" "$label" "$*" | cut -c1-3000 >> $L/commands.log
      python3 -I $SP/sjl-rest/s2/ev2/scripts/timed.py $L/$label.time.txt "$@" > $L/$label.stdout.txt 2> $L/$label.stderr.txt; local rc=$?
      printf '%s\trc=%s\t%s\tstarted=%s\tended=%s\n' "$label" "$rc" "$(cat $L/$label.time.txt)" "$s" "$(date -u +%FT%TZ)" >> $L/runs.tsv; return $rc; }
archive() { local d=$1; rm -rf $d; mkdir -p $d; git -C $REPO archive --format=tar $SHA | tar -xf - -C $d || exit 3
  { echo "commit: $(git -C $REPO rev-parse $SHA)"; echo "tree: $(git -C $REPO rev-parse $SHA^{tree})"; echo "path: $d"; echo "files: $(find $d -type f | wc -l)"; echo "archived: $(date -u +%FT%TZ)"; echo "free after: $(avail)G"; } >> $L/archive.txt; }
avail() { df -BG --output=avail / | tail -1 | tr -dc 0-9; }
cleanup() { cd /; rm -rf $W $ART $CLEAN; }
[ "$(avail)" -ge 8 ] || { echo "less than 8 GiB free: $(avail)G" >&2; exit 9; }
echo "free before: $(avail)G" >> $L/archive.txt
rm -rf $ART $CLEAN; archive $W
# The corrected tool over the archive: each file of tools/fuzz/ at the tool commit, from its blob.
{ echo "tool commit: $(git -C $REPO rev-parse $TOOL)"
  echo "the tool's files written over the archive of $SHA, each from its blob at the tool commit (blob, then the blob at $SHA, then the path):"
  for f in $(git -C $REPO ls-tree --name-only $TOOL $TD/); do
    git -C $REPO show $TOOL:$f > $W/$f || exit 3
    echo "$(git -C $REPO rev-parse $TOOL:$f) $(git -C $REPO rev-parse $SHA:$f 2>/dev/null || echo absent) $f"
  done
  echo "the archive's tool folder against the tool commit's tree (git hash-object of each file):"
  for f in $(cd $W && find $TD -type f | LC_ALL=C sort); do echo "$(git -C $REPO hash-object $W/$f) $f"; done
  echo "the tool commit's tree differs from $SHA only under docs/ (git diff --name-only, paths outside docs/):"
  git -C $REPO diff --name-only $SHA $TOOL | grep -v '^docs/' || echo "(none)"; } > $S/overlay.txt
cd $W; F=Broiler.JS/Broiler.JavaScript.Parser/Bridge/BridgeTreeBuilder.Expressions.cs
if [ $STEP = fcontrol ]; then
  cp $W/$F $BLD/$C8/fcontrol-before.cs
  # The control's edit, as astep.sh's: the bridge throws on one node kind the seeds produce, a template literal.
  python3 -I - $W/$F <<'PY' || { cleanup; exit 3; }
import sys
p = sys.argv[1]; s = open(p, "rb").read()
old = (b"    private AstTemplateExpression Template(TemplateLiteral template)\n    {\n"
       b"        var parts = TemplateParts(template);\n")
new = (b"    private AstTemplateExpression Template(TemplateLiteral template)\n    {\n"
       b"        if (template is not null) throw new System.InvalidOperationException(\"part J control: the bridge throws on a template literal\");\n"
       b"        var parts = TemplateParts(template);\n")
assert s.count(old) == 1, "the method's head is not found once"
open(p, "wb").write(s.replace(old, new))
PY
  diff -u --label a/$F --label b/$F $BLD/$C8/fcontrol-before.cs $W/$F > $S/control.patch; rm -f $BLD/$C8/fcontrol-before.cs
fi
echo "free before the build: $(avail)G" >> $L/archive.txt
[ "$(avail)" -ge 8 ] || { echo "less than 8 GiB free before the build: $(avail)G" >&2; cleanup; exit 9; }
T build-fuzz "${BUILD[@]}" --artifacts-path $ART/fz $TD/fuzz.csproj || { cleanup; exit 4; }
FZ=$(find $ART/fz/bin -name 'sjl-14-fuzz.dll' -path '*release*' | head -1)
( find $ART -name '*.dll' -path '*release*' | sort | xargs -r sha256sum ) > $L/binaries.sha256.txt
T fuzz-session "${RUNENV[@]}" dotnet $FZ --seeds docs/evidence/sjl-14-001/inputs/fuzz-seeds.txt --test262 $SUITE --out $S/session; rc=$?
if [ $STEP = fcontrol ]; then
  rm -rf $ART
  git -C $REPO show $SHA:$F > $W/$F
  archive $CLEAN
  for f in $(git -C $REPO ls-tree --name-only $TOOL $TD/); do git -C $REPO show $TOOL:$f > $CLEAN/$f; done
  { echo "revert: the file written again from the after commit's blob (git show $SHA:$F)"
    echo "sha256 of $F after the revert: $(sha256sum < $W/$F | cut -d' ' -f1)"
    echo "sha256 of $F at the after commit: $(git -C $REPO show $SHA:$F | sha256sum | cut -d' ' -f1)"
    echo "diff -r -q -x bin -x obj against a clean archive of the after commit with the same tool files written over it (overlay.txt), taken after the session:"
    ( cd $BLD/$C8 && diff -r -q -x bin -x obj fcontrol-clean fcontrol ); echo "diff exit $?"; } > $S/revert-check.txt
fi
cleanup; exit $rc
