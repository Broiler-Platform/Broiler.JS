#!/usr/bin/env bash
# home.sh <label> <commit>
# SJL-17.6 (Broiler.JS): the home clauses 1 and 2 of SJL-17, read by the tests SJL-17.3 committed, at one commit.
# Under flock /dev/shm/s2-gate.lock behind the hard idle gate: a clean `git archive` of the commit on /dev/shm; the
# shared libraries' filter built in Release (--disable-build-servers); every shared test project run, each with a trx;
# the Native AOT probe published for linux-x64 and run over the commit's corpus, and the same probe published without
# AOT and run, the two reports compared. The tree is removed at the end.
set -u
label=$1; commit=$2
SC=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest
NOTES=$SC/js-s176
repo=/home/user/js-s176
tree=/dev/shm/js-s176-home-$label
out=$NOTES/home/$label
. $SC/s2/ev2/scripts/gate.sh
export PATH=/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
B="--disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false"
mkdir -p "$out"
exec 9>/dev/shm/s2-gate.lock
flock 9
gate "home-$label" "$NOTES/home/gate.log"
st() { echo "$(date -u +%FT%TZ) $*" >> "$out/progress.txt"; }
machine_record > "$out/machine.txt"
full=$(git -C "$repo" rev-parse "$commit")
rm -rf "$tree"; mkdir -p "$tree"
git -C "$repo" archive --format=tar -o "$out/archive.tar" "$full"
st "commit $full archive sha256 $(sha256sum "$out/archive.tar" | cut -d' ' -f1) tar commit id $(git get-tar-commit-id < "$out/archive.tar")"
tar -x -C "$tree" -f "$out/archive.tar"; rm -f "$out/archive.tar"
cd "$tree"
st "load $(cut -d' ' -f1-3 /proc/loadavg); SDK $(dotnet --version)"
dotnet build Broiler.ECMAScript.slnf -c Release $B > "$out/build-slnf.log" 2>&1; st "build slnf rc=$?"
st "build slnf warnings: $(grep -E 'warning [A-Za-z]+[0-9]+' "$out/build-slnf.log" | sort -u | wc -l), errors: $(grep -E ' error [A-Za-z]+[0-9]+' "$out/build-slnf.log" | sort -u | wc -l)"
env -u LANG -u LANGUAGE -u LC_ALL TZ=UTC dotnet test Broiler.ECMAScript.slnf -c Release --no-build --logger "trx" \
  --results-directory "$out/trx" > "$out/tests.log" 2>&1; st "tests shared rc=$?"
dotnet publish Broiler.ECMAScript/Broiler.ECMAScript.Probe -c Release -r linux-x64 $B > "$out/probe-publish.log" 2>&1; st "probe aot publish rc=$?"
st "probe aot publish warnings: $(grep -E 'warning [A-Za-z]+[0-9]+' "$out/probe-publish.log" | sort -u | wc -l)"
pub=Broiler.ECMAScript/Broiler.ECMAScript.Probe/bin/Release/net10.0/linux-x64/publish
if [ -x "$pub/Broiler.ECMAScript.Probe" ]; then
  st "probe publish managed dlls: $(ls "$pub"/*.dll 2>/dev/null | wc -l)"
  env -u LANG -u LANGUAGE -u LC_ALL TZ=UTC "$pub/Broiler.ECMAScript.Probe" conformance/vectors --report "$out/probe-aot.tsv" > "$out/probe-aot.log" 2>&1; st "probe aot run rc=$?"
  dotnet publish Broiler.ECMAScript/Broiler.ECMAScript.Probe -c Release -p:PublishAot=false -o "$tree/jit-publish" $B \
    > "$out/probe-publish-jit.log" 2>&1; st "probe jit publish rc=$? warnings: $(grep -E 'warning [A-Za-z]+[0-9]+' "$out/probe-publish-jit.log" | sort -u | wc -l)"
  env -u LANG -u LANGUAGE -u LC_ALL TZ=UTC dotnet "$tree/jit-publish/Broiler.ECMAScript.Probe.dll" conformance/vectors --report "$out/probe-jit.tsv" \
    > "$out/probe-jit.log" 2>&1; st "probe jit run rc=$?"
  cmp "$out/probe-aot.tsv" "$out/probe-jit.tsv" && st "probe aot and jit reports identical" || st "probe aot and jit reports DIFFER"
else
  st "no AOT probe binary"
fi
cd /; rm -rf "$tree"
st "done; tree removed"
