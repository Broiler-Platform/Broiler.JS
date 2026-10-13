#!/usr/bin/env bash
# The test262 run's shell was built at b598028; the after commit is ef67693. This rebuilds the shell
# at ef67693 in the same directory the run built it in, twice, and hashes its binaries each time
# against shell-binaries.sha256, the hashes the run's driver took before its first shard:
#   1. as the run built it, `dotnet build -c Release` - the first-party assemblies then differ, since
#      the SDK writes the commit into each one's informational version (+<commit>);
#   2. the same, with SourceRevisionId set to b598028's commit - the one input of the build that
#      names the commit; if every binary then hashes as the run's did, ef67693's sources compile to
#      exactly the shell the run used.
# The run's binaries are kept aside and put back, and the checkout returned to b598028, at the end.
set -u
S=<scratch>
RUN=$S/brjs-test262-after
export PATH=/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 TMPDIR=$S/tmp
cd $S/jsafter
BIN=Broiler.JS/Broiler.JavaScript/bin/Release/net10.0
PROJECT=Broiler.JS/Broiler.JavaScript/Broiler.JavaScript.csproj
RUN_COMMIT=$(git rev-parse b598028)

echo "# shell rebuild check $(date -u +%FT%TZ)"
echo "# the run's binaries, as built at $RUN_COMMIT, against the driver's hashes"
(cd $BIN && sha256sum *.dll) | diff -q - $RUN/shell-binaries.sha256 && echo "same as shell-binaries.sha256"
rm -rf $S/shell-rebuild-run-bin && cp -a $BIN $S/shell-rebuild-run-bin

git checkout -q ef67693 || exit 1
echo "# checkout $(git rev-parse HEAD), status: $(git status --short | wc -l) changed file(s)"
echo "# changed between the two commits:"
git diff --stat $RUN_COMMIT HEAD | sed 's/^/#   /'

echo
echo "## 1. dotnet build -c Release --no-incremental, as the run built it"
dotnet build -c Release -nologo --no-incremental $PROJECT > $S/shell-rebuild-1.log 2>&1
echo "build exit $? ($(grep -c 'warning' $S/shell-rebuild-1.log) line(s) naming a warning)"
(cd $BIN && sha256sum *.dll) > $S/shell-rebuild-1.sha256
echo "binaries: $(wc -l < $S/shell-rebuild-1.sha256); differing from the run's:"
diff <(sort -k2 $RUN/shell-binaries.sha256) <(sort -k2 $S/shell-rebuild-1.sha256) | grep '^>' | awk '{print "  " $3}'
echo "informational version of BroilerJS.dll:"
strings -el $BIN/BroilerJS.dll | grep -m1 '^0\.0\.0+' | sed 's/^/  /'

echo
echo "## 2. the same, with -p:SourceRevisionId=$RUN_COMMIT"
dotnet build -c Release -nologo --no-incremental -p:SourceRevisionId=$RUN_COMMIT $PROJECT > $S/shell-rebuild-2.log 2>&1
echo "build exit $?"
(cd $BIN && sha256sum *.dll) > $S/shell-rebuild-2.sha256
echo "binaries: $(wc -l < $S/shell-rebuild-2.sha256)"
if diff <(sort -k2 $RUN/shell-binaries.sha256) <(sort -k2 $S/shell-rebuild-2.sha256); then
  echo "EVERY BINARY HASHES AS THE RUN'S (shell-binaries.sha256)"
else
  echo "DIFFERENT from the run's binaries"
fi

git checkout -q $RUN_COMMIT
rm -rf $BIN && cp -a $S/shell-rebuild-run-bin $BIN
echo
echo "# restored: checkout $(git rev-parse --short HEAD), the run's binaries put back"
(cd $BIN && sha256sum *.dll) | diff -q - $RUN/shell-binaries.sha256 && echo "same as shell-binaries.sha256"
echo "# end $(date -u +%FT%TZ)"
