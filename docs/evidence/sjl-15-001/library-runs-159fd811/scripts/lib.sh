#!/usr/bin/env bash
# lib.sh: sourced by the scripts of SJL-15.2's runs of parts F and G and of bundle sjl-13-001's part E at stage 8's commit
# (written 2026-11-28). Every build and run goes through SCRATCH/sjl-rest/s2/ev2/scripts/run.sh (the build lock and the
# hard idle gate); each script makes one build tree under /home/user/js-s158-lib-build/<label>/ from a clean
# `git archive` of the commit and deletes it at its end. Every dotnet and python command runs under `env -i` (RUNENV): no
# LANG, LC_* or BROILER_* variable, TZ=UTC, the SDK at /root/.dotnet, no build server, and TMPDIR in the build tree.
SP=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad
NOTES=$SP/sjl-rest/js-s158-lib
REPO=/home/user/js-s158-lib
BROOT=/home/user/js-s158-lib-build
M8=159fd811f74d0f43e7ed7327081267a96f8f1196
SUITEREF=ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
SUITEDIR=/dev/shm/js-s158-lib-suite
SUITE=$SUITEDIR/test262-$SUITEREF
VMREPO=/home/user/Broiler.VM
VMCOMMIT=c1b33a71db0e91494e8e588a6ad48c2f11dd4c22
A262=src/tests/conformance/pins/test262-$SUITEREF.tar.gz
GIB=$((1024*1024*1024))

# setenv <build dir>: the run environment, for `"${RUNENV[@]}" <command>`.
setenv() {
  mkdir -p "$1/tmp"
  RUNENV=(env -i PATH=/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1
          DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false TZ=UTC
          TMPDIR="$1/tmp" S2_RUN_TAG="${S2_RUN_TAG:-none}")
}

# diskok <what> <log>: wait until at least 8 GiB are free on /, then log it.
diskok() {
  while [ "$(df -B1 --output=avail / | tail -1)" -lt $((8*GIB)) ]; do
    echo "$(date -u +%FT%TZ) $1 waiting: $(df -h / | tail -1)" >> "$2"; sleep 30; done
  echo "$(date -u +%FT%TZ) $1 disk ok: $(df -B1 --output=avail / | tail -1) bytes free" >> "$2"
}

# T <logdir> <label> <command...>: a timed step, its output in <logdir>/<label>.{stdout,stderr,time}.txt.
FIRST=0
T() {
  local L=$1 label=$2; shift 2; local s; s=$(date -u +%FT%TZ)
  printf '%s %s: %s\n' "$s" "$label" "$*" | cut -c1-3000 >> "$L/commands.log"
  python3 -I $SP/sjl-rest/s2/ev2/scripts/timed.py "$L/$label.time.txt" "$@" > "$L/$label.stdout.txt" 2> "$L/$label.stderr.txt"
  local rc=$?
  printf '%s\trc=%s\t%s\tstarted=%s\tended=%s\n' "$label" "$rc" "$(cat "$L/$label.time.txt" 2>/dev/null)" "$s" "$(date -u +%FT%TZ)" >> "$L/runs.tsv"
  [ $rc -ne 0 ] && [ $FIRST -eq 0 ] && FIRST=$rc
  return $rc
}

# newtree <build dir> <sha> <log> [pathspec...]: a clean archive of the commit into <build dir>/src, and the run
# environment's TMPDIR, <build dir>/tmp, made again after the delete. (Revised 2026-11-28 after the first attempt: it
# read `rm -rf "$1"; mkdir -p "$1/src"`, so that TMPDIR named a folder the delete had removed and MSBuild could not
# start; no build, test or run of that attempt produced an output.) (Revised again 2026-11-28, after part D's first step
# waited on its own tree: with pathspecs the archive holds only those paths of the commit, and before the extraction
# 8 GiB plus the archive's own size must be free, so that the check before the build can pass with the tree on disk.)
newtree() {
  local d=$1 sha=$2 log=$3; shift 3
  local size; size=$(git -C $REPO archive --format=tar "$sha" -- "$@" | wc -c)
  while [ "$(df -B1 --output=avail / | tail -1)" -lt $((8*GIB + size)) ]; do
    echo "$(date -u +%FT%TZ) before-archive waiting (archive $size bytes): $(df -h / | tail -1)" >> "$log"; sleep 30; done
  echo "$(date -u +%FT%TZ) before-archive disk ok: $(df -B1 --output=avail / | tail -1) bytes free, archive $size bytes${*:+, paths: $*}" >> "$log"
  rm -rf "$d"; mkdir -p "$d/src" "$d/tmp"
  git -C $REPO archive --format=tar "$sha" -- "$@" | tar -xf - -C "$d/src" || return 3
}

# deltree <build dir> <log>: delete the build tree, logging the disk around it.
deltree() {
  echo "$(date -u +%FT%TZ) before delete: $(df -B1 --output=avail / | tail -1)" >> "$2"
  cd /; rm -rf "$1"
  echo "$(date -u +%FT%TZ) after delete: $(df -B1 --output=avail / | tail -1)" >> "$2"
}

# suite <log>: test262 at ccaac100 from Broiler.VM's commit (git show, read only), its SHA-256 checked against the
# manifest's pin-test262-archive line (in the archive's tree, cwd), then extracted afresh to /dev/shm.
suite() {
  local want got
  want=$(awk -F'\t' '$1=="#% pin-test262-archive"{print $3}' conformance/parse/corpus.manifest)
  got=$(git -C $VMREPO show $VMCOMMIT:$A262 | sha256sum | cut -d' ' -f1)
  echo "$(date -u +%FT%TZ) suite: Broiler.VM $VMCOMMIT:$A262 sha256 $got; manifest's pin-test262-archive $want; $([ "$got" = "$want" ] && echo equal || echo DIFFERENT)" >> "$1"
  [ -n "$want" ] && [ "$got" = "$want" ] || return 7
  rm -rf $SUITEDIR; mkdir -p $SUITEDIR
  git -C $VMREPO show $VMCOMMIT:$A262 | tar -xzf - -C $SUITEDIR || return 8
  echo "$(date -u +%FT%TZ) suite: extracted to $SUITE, $(find $SUITE -type f | wc -l) files, $(du -sm $SUITEDIR | cut -f1) MB in /dev/shm" >> "$1"
}
