#!/usr/bin/env bash
# The test262 run's shell was built at b598028; the after commit is ef67693. This shows that the two
# commits compile the shell to the same binaries but for the commit each one is stamped with, by
# rebuilding it in the directory the run built it in, and hashing its binaries each time:
#   0. at b598028, `dotnet build -c Release`, as the run built it: every binary hashes as the run's
#      (shell-binaries.sha256, the hashes the run's driver took before its first shard), so the build
#      is reproducible here and the run's binaries are what b598028 builds;
#   1. at ef67693, the same: the first-party assemblies differ, since the SDK writes the commit into
#      each one's informational version (+<commit>) and SourceLink writes it into each one's PDB, whose
#      identity the assembly carries;
#   2. and 3. at ef67693 and at b598028, the same build with the source-control queries off
#      (EnableSourceControlManagerQueries=false, EnableSourceLink=false), so that neither commit is
#      written into any binary: if the two hash alike, the commits compile the shell identically.
# A first attempt pinned only SourceRevisionId and still differed, because of the PDBs; its script and
# log are kept beside this one. The run's binaries are put back, and the checkout returned to b598028,
# at the end.
set -u
S=<scratch>
RUN=$S/brjs-test262-after
export PATH=/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 TMPDIR=$S/tmp
cd $S/jsafter
BIN=Broiler.JS/Broiler.JavaScript/bin/Release/net10.0
PROJECT=Broiler.JS/Broiler.JavaScript/Broiler.JavaScript.csproj
RUN_COMMIT=$(git rev-parse b598028)
AFTER_COMMIT=$(git rev-parse ef67693)
NO_SCM="-p:EnableSourceControlManagerQueries=false -p:EnableSourceLink=false"

hashes() { (cd $BIN && sha256sum *.dll | sort -k2); }
build() {  # <label> <commit> [extra properties]
  local label=$1 commit=$2; shift 2
  git checkout -q $commit || exit 1
  echo
  echo "## $label: checkout $(git rev-parse --short HEAD) ($(git status --short | wc -l) changed file(s)), dotnet build -c Release --no-incremental $*"
  dotnet build -c Release -nologo --no-incremental "$@" $PROJECT > $S/shell-rebuild-$label.log 2>&1
  echo "build exit $?"
  hashes > $S/shell-rebuild-$label.sha256
  echo "binaries: $(wc -l < $S/shell-rebuild-$label.sha256); informational version of BroilerJS.dll: $(strings -el $BIN/BroilerJS.dll | grep -m1 '^0\.0\.0' )"
}
differing() {  # <a.sha256> <b.sha256>
  local names
  names=$(join -j 2 <(sort -k2 $1) <(sort -k2 $2) | awk '$2 != $3 {print $1}')
  if [ -z "$names" ]; then echo "every binary hashes alike"; else echo "differing:"; echo "$names" | sed 's/^/  /'; fi
}

echo "# shell rebuild check $(date -u +%FT%TZ)"
echo "# the run's binaries, as built at $RUN_COMMIT, against the driver's hashes"
hashes | diff -q - <(sort -k2 $RUN/shell-binaries.sha256) && echo "same as shell-binaries.sha256"
rm -rf $S/shell-rebuild-run-bin && cp -a $BIN $S/shell-rebuild-run-bin
echo "# changed between the two commits:"
git diff --stat $RUN_COMMIT $AFTER_COMMIT | sed 's/^/#   /'

build 0-b598028 $RUN_COMMIT
echo "against the run's binaries: $(differing $RUN/shell-binaries.sha256 $S/shell-rebuild-0-b598028.sha256)"
build 1-ef67693 $AFTER_COMMIT
echo "against the run's binaries: $(differing $RUN/shell-binaries.sha256 $S/shell-rebuild-1-ef67693.sha256)"
build 2-ef67693-no-commit $AFTER_COMMIT $NO_SCM
build 3-b598028-no-commit $RUN_COMMIT $NO_SCM
echo
echo "## 2 against 3, neither stamped with its commit: $(differing $S/shell-rebuild-2-ef67693-no-commit.sha256 $S/shell-rebuild-3-b598028-no-commit.sha256)"
for label in 0-b598028 1-ef67693 2-ef67693-no-commit 3-b598028-no-commit; do
  echo "# hashes, $label:"; sed 's/^/#   /' $S/shell-rebuild-$label.sha256
done

git checkout -q $RUN_COMMIT
rm -rf $BIN && cp -a $S/shell-rebuild-run-bin $BIN
echo
echo "# restored: checkout $(git rev-parse --short HEAD), the run's binaries put back"
hashes | diff -q - <(sort -k2 $RUN/shell-binaries.sha256) && echo "same as shell-binaries.sha256"
echo "# end $(date -u +%FT%TZ)"
