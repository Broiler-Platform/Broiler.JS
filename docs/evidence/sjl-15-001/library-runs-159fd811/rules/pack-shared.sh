#!/usr/bin/env bash
# A bash rendering of eng/pack.ps1 -ProjectSet Shared -Configuration Release (no -Version): every project of
# Broiler.ECMAScript.slnf evaluated, the packable ones packed with package validation into one empty folder, and each
# package checked as pack.ps1 checks it (identity, description, README.md and icon.png, internal dependency versions,
# lib/*.dll and lib/*.xml, symbols).
# Usage: pack-shared.sh <repository root> <output folder>
#
# Copied 2026-11-28, for bundle sjl-13-001's part E taken at stage 8's commit (bundle sjl-15-001), from bundle
# sjl-13-001's docs/evidence/sjl-13-001/library-commit/rules/pack-shared.sh, there is no pwsh on the host. Re-checked
# against eng/pack.ps1 at 159fd811f74d0f43e7ed7327081267a96f8f1196, which is unchanged since that bundle's library commit
# d4b330d8 (`git diff --quiet d4b330d8 159fd811 -- eng/pack.ps1`). Three of pack.ps1's checks that the copy left out
# are added, each as pack.ps1 writes it: "Duplicate package IDs."; "Incorrect identity in <path>." (the nuspec's id and
# version against the project's PackageId and PackageVersion); and the assembly, documentation and symbols checks only
# where IncludeBuildOutput is not false, the symbols check only where IncludeSymbols is true; and, as pack.ps1 does,
# every project is evaluated, and the versions and identifiers checked, before any is packed. Nothing else changes.
# (Revised 2026-11-28 after its first run, which stopped before packing: the packing loop evaluated each project again
# for IsPackable alone, which msbuild prints as a bare value and not as JSON, so the parse failed; it now packs the
# projects the first loop found packable.)
set -euo pipefail
root=$1
out=$2
cd "$root"
mkdir -p "$out"
if ls "$out"/*.nupkg "$out"/*.snupkg >/dev/null 2>&1; then echo "Package output must be empty: $out" >&2; exit 1; fi
mapfile -t paths < <(python3 -I -c 'import json,sys; [print(p) for p in json.load(open("Broiler.ECMAScript.slnf"))["solution"]["projects"]]')
declare -A version
ids=()
meta=()
packables=()
for path in "${paths[@]}"; do
  case "$path" in Broiler.ECMAScript/*) ;; *) echo "The shared filter names $path, outside Broiler.ECMAScript/." >&2; exit 1;; esac
  json=$(dotnet msbuild "$path" -nologo -p:Configuration=Release '-getProperty:IsPackable,PackageId,PackageVersion,IncludeBuildOutput,IncludeSymbols')
  packable=$(python3 -I -c 'import json,sys; print(json.loads(sys.argv[1])["Properties"]["IsPackable"])' "$json")
  if [ "$packable" = "true" ]; then
    id=$(python3 -I -c 'import json,sys; print(json.loads(sys.argv[1])["Properties"]["PackageId"])' "$json")
    ver=$(python3 -I -c 'import json,sys; print(json.loads(sys.argv[1])["Properties"]["PackageVersion"])' "$json")
    m=$(python3 -I -c 'import json,sys; p=json.loads(sys.argv[1])["Properties"]; print(p["PackageId"]+"="+p["PackageVersion"]+","+p["IncludeBuildOutput"]+","+p["IncludeSymbols"])' "$json")
    ids+=("$id"); version[$id]=$ver; meta+=("$m"); packables+=("$path")
    echo "packable: $path -> $id $ver"
  fi
done
[ ${#ids[@]} -gt 0 ] || { echo "No packable projects found." >&2; exit 1; }
[ "$(printf '%s\n' "${version[@]}" | sort -u | wc -l)" = 1 ] || { echo "Package versions must agree." >&2; exit 1; }
[ "$(printf '%s\n' "${ids[@]}" | sort -u | wc -l)" = "${#ids[@]}" ] || { echo "Duplicate package IDs." >&2; exit 1; }
for path in "${packables[@]}"; do
  dotnet pack "$path" -c Release --nologo -p:Configuration=Release -o "$out"
done
count=$(ls "$out"/*.nupkg | wc -l)
[ "$count" = "${#ids[@]}" ] || { echo "Expected ${#ids[@]} packages; found $count." >&2; exit 1; }
python3 -I - "$out" "${meta[@]}" <<'PY'
import sys, zipfile, re, os, xml.etree.ElementTree as ET
out = sys.argv[1]
meta = {}
for m in sys.argv[2:]:
    pid, rest = m.split("=", 1)
    ver, build_output, symbols = rest.split(",")
    meta[pid] = (ver, build_output, symbols)
ids = list(meta)
expected = {pid: meta[pid][0] for pid in ids}
for pid in ids:
    ver, build_output, symbols = meta[pid]
    path = os.path.join(out, f'{pid}.{ver}.nupkg')
    with zipfile.ZipFile(path) as z:
        names = z.namelist()
        spec = [n for n in names if n.endswith('.nuspec')]
        assert len(spec) == 1, f'Expected one nuspec in {path}.'
        root = ET.fromstring(z.read(spec[0]))
        ns = {'n': root.tag[1:].split('}')[0]}
        md = root.find('n:metadata', ns)
        if md.find('n:id', ns).text != pid or md.find('n:version', ns).text != ver:
            raise SystemExit(f'Incorrect identity in {path}.')
        desc = (md.find('n:description', ns).text or '').strip()
        assert desc and desc != 'Package Description', f'Set a <Description> for {pid}.'
        for asset in ('README.md', 'icon.png'):
            assert asset in names, f'Missing {asset} in {path}.'
        for dep in root.iter('{%s}dependency' % ns['n']):
            if dep.get('id') in expected and dep.get('version') != expected[dep.get('id')]:
                raise SystemExit(f"Incorrect internal dependency {dep.get('id')} {dep.get('version')} in {path}.")
        if build_output != 'false':
            assert any(re.match(r'lib/.*\.dll$', n) for n in names), f'Missing assembly in {path}.'
            assert any(re.match(r'lib/.*\.xml$', n) for n in names), f'Missing API documentation in {path}.'
            if symbols == 'true':
                assert os.path.exists(path[:-6] + '.snupkg'), f'Missing symbols for {path}.'
        deps = sorted((d.get('id'), d.get('version')) for d in root.iter('{%s}dependency' % ns['n']))
        print(f'{pid} {ver}: IncludeBuildOutput {build_output}, IncludeSymbols {symbols}; dependencies {deps}')
print(f'Verified {len(ids)} Shared packages at {expected[ids[0]]}.')
PY
