#!/usr/bin/env bash
# commit.sh <full-sha> <modes>: one commit's runs for bundle sjl-14-001's part B (and the side-by-side run at W1).
# modes, comma-separated: pc  = sjl-12-001's parse-corpus tool, default mode (P, K) and --dump --order manifest;
#                         son = the side-by-side tool, entries B,K-on (the switch on), --dump;
#                         sbs = the side-by-side tool, entries P,B,K,K-on, --dump (part C's tool run, at W1).
# The source is a clean `git archive` of the commit on /dev/shm; builds are Release, outside the tree, no build servers.
# Each tool run gets a minimal environment: TZ=UTC, no LANG, no LC_*, no BROILER_* (env -i with PATH, HOME, DOTNET_ROOT).
set -u
SHA=$1; MODES=$2; C8=${SHA:0:8}
SCR=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-s14b
SHM=/dev/shm/js-s14b
SUITE=$SHM/suite/test262-ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
SRC=$SHM/src/$C8; ART=$SHM/art/$C8; OUT=$SCR/runs/$C8; L=$OUT/logs
export PATH=/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false
mkdir -p $OUT $L
T() { local label=$1; shift; local s=$(date -u +%FT%TZ); printf '%s %s: %s\n' "$s" "$label" "$*" >> $L/commands.log
      python3 -I $SCR/scripts/timed.py $L/$label.time.txt "$@" > $L/$label.stdout.txt 2> $L/$label.stderr.txt; local rc=$?
      printf '%s\trc=%s\t%s\tstarted=%s\tended=%s\n' "$label" "$rc" "$(cat $L/$label.time.txt)" "$s" "$(date -u +%FT%TZ)" >> $L/runs.tsv; return $rc; }
rm -rf $SRC $ART; mkdir -p $SRC
{ echo "commit: $SHA"; echo "tree: $(git -C /home/user/js-s14b rev-parse $SHA^{tree})"; echo "first-parent: $(git -C /home/user/js-s14b rev-parse $SHA^1)";
  echo "parse-corpus tool tree: $(git -C /home/user/js-s14b rev-parse $SHA:docs/evidence/sjl-12-001/tools/parse-corpus)";
  echo "side-by-side tool tree: $(git -C /home/user/js-s14b rev-parse $SHA:docs/evidence/sjl-14-001/tools/side-by-side 2>/dev/null || echo absent)";
  echo "manifest blob: $(git -C /home/user/js-s14b rev-parse $SHA:conformance/parse/corpus.manifest)";
  echo "suite: $SUITE (Broiler.VM's archive src/tests/conformance/pins/test262-ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e.tar.gz, sha256 f58ce79141529c9fa33592e22ff3ff0d83b83830ac8e372ecd32e1623db1c040)";
  echo "sdk: $(dotnet --version)"; echo "machine: $(uname -srm), $(nproc) processors"; } > $L/commit.txt
git -C /home/user/js-s14b archive --format=tar $SHA | tar -xf - -C $SRC || { echo "archive failed" >> $L/commit.txt; exit 3; }
cd $SRC
M=conformance/parse/corpus.manifest
rc_all=0
case ",$MODES," in *,pc,*)
  T build-parse-corpus dotnet build -c Release -nologo --disable-build-servers --artifacts-path $ART/pc docs/evidence/sjl-12-001/tools/parse-corpus/parse-corpus.csproj || exit 4
  PC=$ART/pc/bin/parse-corpus/release/parse-corpus.dll
  T parse-corpus-c env -i PATH=/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC dotnet $PC --manifest $M --test262 $SUITE --out $OUT/parse-corpus-c.tsv.gz || rc_all=5
  T parse-corpus-d1 env -i PATH=/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC dotnet $PC --dump --order manifest --manifest $M --test262 $SUITE --out $OUT/parse-corpus-d1.tsv.gz || rc_all=6
  ;; esac
case ",$MODES," in *,son,*|*,sbs,*)
  T build-side-by-side dotnet build -c Release -nologo --disable-build-servers --artifacts-path $ART/sbs docs/evidence/sjl-14-001/tools/side-by-side/side-by-side.csproj || exit 7
  SB=$ART/sbs/bin/side-by-side/release/side-by-side.dll
  case ",$MODES," in *,son,*)
    T switch-on-b-kon env -i PATH=/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC dotnet $SB --manifest $M --test262 $SUITE --entries B,K-on --dump --out $OUT/switch-on-b-kon.tsv.gz || rc_all=8 ;; esac
  case ",$MODES," in *,sbs,*)
    T side-by-side env -i PATH=/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC dotnet $SB --manifest $M --test262 $SUITE --entries P,B,K,K-on --dump --out $OUT/side-by-side.tsv.gz || rc_all=9 ;; esac
  ;; esac
( cd $OUT && find . -path ./logs -prune -o -name '*.tsv.gz' -print | sort | xargs -r sha256sum > binaries-and-reports.sha256.txt )
find $ART -name '*.dll' -path '*release*' | sort | xargs -r sha256sum > $L/binaries.sha256.txt
cd /; rm -rf $SRC $ART
exit $rc_all
