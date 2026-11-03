#!/usr/bin/env bash
# A bash rendering of eng/pack.ps1 -ProjectSet Shared -Configuration Release (no -Version): every project of
# Broiler.ECMAScript.slnf evaluated, the packable ones packed with package validation into one empty folder, and each
# package checked as pack.ps1 checks it (identity, description, README.md and icon.png, internal dependency versions,
# lib/*.dll and lib/*.xml, symbols).
# Usage: pack-shared.sh <repository root> <output folder>
set -euo pipefail
root=$1
out=$2
cd "$root"
mkdir -p "$out"
if ls "$out"/*.nupkg "$out"/*.snupkg >/dev/null 2>&1; then echo "Package output must be empty: $out" >&2; exit 1; fi
mapfile -t paths < <(python3 -I -c 'import json,sys; [print(p) for p in json.load(open("Broiler.ECMAScript.slnf"))["solution"]["projects"]]')
declare -A version
ids=()
for path in "${paths[@]}"; do
  case "$path" in Broiler.ECMAScript/*) ;; *) echo "The shared filter names $path, outside Broiler.ECMAScript/." >&2; exit 1;; esac
  json=$(dotnet msbuild "$path" -nologo -p:Configuration=Release '-getProperty:IsPackable,PackageId,PackageVersion,IncludeBuildOutput,IncludeSymbols')
  packable=$(python3 -I -c 'import json,sys; print(json.loads(sys.argv[1])["Properties"]["IsPackable"])' "$json")
  if [ "$packable" = "true" ]; then
    id=$(python3 -I -c 'import json,sys; print(json.loads(sys.argv[1])["Properties"]["PackageId"])' "$json")
    ver=$(python3 -I -c 'import json,sys; print(json.loads(sys.argv[1])["Properties"]["PackageVersion"])' "$json")
    ids+=("$id"); version[$id]=$ver
    echo "packable: $path -> $id $ver"
    dotnet pack "$path" -c Release --nologo -p:Configuration=Release -o "$out"
  fi
done
[ ${#ids[@]} -gt 0 ] || { echo "No packable projects found." >&2; exit 1; }
[ "$(printf '%s\n' "${version[@]}" | sort -u | wc -l)" = 1 ] || { echo "Package versions must agree." >&2; exit 1; }
count=$(ls "$out"/*.nupkg | wc -l)
[ "$count" = "${#ids[@]}" ] || { echo "Expected ${#ids[@]} packages; found $count." >&2; exit 1; }
python3 -I - "$out" "${ids[@]}" <<'PY'
import sys, zipfile, re, os, xml.etree.ElementTree as ET
out, ids = sys.argv[1], sys.argv[2:]
versions = {}
for name in os.listdir(out):
    if name.endswith('.nupkg'):
        with zipfile.ZipFile(os.path.join(out, name)) as z:
            spec = [n for n in z.namelist() if n.endswith('.nuspec')]
            root = ET.fromstring(z.read(spec[0]))
            ns = {'n': root.tag[1:].split('}')[0]}
            versions[root.find('n:metadata/n:id', ns).text] = root.find('n:metadata/n:version', ns).text
for pid in ids:
    path = os.path.join(out, f'{pid}.{versions[pid]}.nupkg')
    with zipfile.ZipFile(path) as z:
        names = z.namelist()
        spec = [n for n in names if n.endswith('.nuspec')]
        assert len(spec) == 1, f'Expected one nuspec in {path}.'
        root = ET.fromstring(z.read(spec[0]))
        ns = {'n': root.tag[1:].split('}')[0]}
        md = root.find('n:metadata', ns)
        desc = (md.find('n:description', ns).text or '').strip()
        assert desc and desc != 'Package Description', f'Set a <Description> for {pid}.'
        for asset in ('README.md', 'icon.png'):
            assert asset in names, f'Missing {asset} in {path}.'
        for dep in root.iter('{%s}dependency' % ns['n']):
            if dep.get('id') in versions and dep.get('version') != versions[dep.get('id')]:
                raise SystemExit(f"Incorrect internal dependency {dep.get('id')} {dep.get('version')} in {path}.")
        assert any(re.match(r'lib/.*\.dll$', n) for n in names), f'Missing assembly in {path}.'
        assert any(re.match(r'lib/.*\.xml$', n) for n in names), f'Missing API documentation in {path}.'
        assert os.path.exists(path[:-6] + '.snupkg'), f'Missing symbols for {path}.'
        deps = sorted((d.get('id'), d.get('version')) for d in root.iter('{%s}dependency' % ns['n']))
        print(f'{pid} {versions[pid]}: dependencies {deps}')
print(f'Verified {len(ids)} Shared packages at {versions[ids[0]]}.')
PY
