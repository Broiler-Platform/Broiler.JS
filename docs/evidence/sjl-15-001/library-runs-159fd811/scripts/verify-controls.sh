#!/usr/bin/env bash
# part-f-tools.sh <full-sha> <outdir>: part F's checklist and site listing (bundle sjl-15-001's rule, part F; decision
# D-js-part-f-tools), and bundle sjl-13-001's part H's two controls over scratch archives, at the commit, from a clean
# `git archive` under /home/user/js-s158-lib-build/tools/. The scripts are the commit's own
# docs/evidence/sjl-15-001/tools/model_check.py and row_sites.py; the Broiler.VM inputs are `git show` of the commit
# tools/broiler-vm-inputs.sha256 names, checked against it (--vm-digests). Control 1: the member
# SemanticModel.NamedEvaluationKey removed from SemanticModel.cs (its documentation, attributes and body) and its line
# from PublicAPI.txt; control 2: a public Broiler.VM.Witness.EngineStub added to the code and its line to PublicAPI.txt
# (both predeclared at stage 8 before its rehearsal). Written 2026-11-28; through run.sh.
# verify-controls.sh (fixer, 2026-11-28): a verification copy of part-f-tools.sh, NOT retained; it archives only the paths
# the tools read (PS below), checks 8 GiB plus the archive's size before each extraction, and builds under verify-controls/.
set -u
source /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-s158-lib/scripts/lib.sh
SHA=$1; OUT=$2; BLD=$BROOT/verify-controls; L=$OUT/logs
mkdir -p $OUT $L; setenv $BLD
PS=(Broiler.ECMAScript docs/evidence/sjl-15-001/tools docs/evidence/sjl-15-001/decision-rule.md conformance/vectors/semantics)
chk() { local size; size=$(git -C $REPO archive --format=tar $SHA -- "${PS[@]}" | wc -c); while [ "$(df -B1 --output=avail / | tail -1)" -lt $((8*GIB + size)) ]; do echo "$(date -u +%FT%TZ) $1 waiting" >> $L/disk.log; sleep 30; done; echo "$(date -u +%FT%TZ) $1 disk ok: $(df -B1 --output=avail / | tail -1) bytes free, archive $size bytes" >> $L/disk.log; }
newtree $BLD $SHA $L/disk.log "${PS[@]}" || exit 3
{ echo "commit: $(git -C $REPO rev-parse $SHA)"; echo "tree: $(git -C $REPO rev-parse $SHA^{tree})"; echo "started: $(date -u +%FT%TZ)"; } > $L/commit.txt
FE=Broiler.ECMAScript/Broiler.ECMAScript.Frontend
JSD=src/Broiler.VM.Profile.JavaScript/docs/decisions/0080-the-shared-frontend-contract-and-the-in-place-preparation.md
VMS=docs/javascript-shared-libraries/milestones/sjl-15-static-semantics.md
mkdir -p $BLD/vm
VMC=$(grep -o 'Broiler.VM [0-9a-f]\{40\}' $BLD/src/docs/evidence/sjl-15-001/tools/broiler-vm-inputs.sha256 | head -1 | cut -d' ' -f2)
echo "Broiler.VM commit named by tools/broiler-vm-inputs.sha256: $VMC" >> $L/commit.txt
git -C $VMREPO show $VMC:$JSD > $BLD/vm/jsd-0080.md || exit 4
git -C $VMREPO show $VMC:$VMS > $BLD/vm/sjl-15-static-semantics.md || exit 4
( cd $BLD/vm && sha256sum jsd-0080.md sjl-15-static-semantics.md ) > $OUT/vm-inputs.sha256
mc() { # mc <tree> <label> [extra args]: model_check.py over the tree
  local t=$1 label=$2; shift 2
  ( cd $t && T $L $label "${RUNENV[@]}" python3 -I docs/evidence/sjl-15-001/tools/model_check.py --jsd0080 $BLD/vm/jsd-0080.md \
      --milestone $BLD/vm/sjl-15-static-semantics.md --public-api $FE/PublicAPI.txt --project $FE/Broiler.ECMAScript.Frontend.csproj \
      --rows $FE/Rows.cs --semantic-rows $FE/SemanticRows.cs --stages Broiler.ECMAScript/docs/frontend/stages.tsv \
      --vm-digests docs/evidence/sjl-15-001/tools/broiler-vm-inputs.sha256 "$@" --out $OUT/$label.txt ); echo "$label rc=$?" >> $L/commit.txt; }
cd $BLD/src
mc $BLD/src model-check
T $L row-sites "${RUNENV[@]}" python3 -I docs/evidence/sjl-15-001/tools/row_sites.py --stages Broiler.ECMAScript/docs/frontend/stages.tsv \
  --frontend $FE --semantic-rows $FE/SemanticRows.cs --vectors conformance/vectors/semantics \
  --rule docs/evidence/sjl-15-001/decision-rule.md --out $OUT/row-sites.txt; echo "row-sites rc=$?" >> $L/commit.txt
# Control 1, a scratch archive.
chk before-control-1; C1=$BLD/control-1; mkdir -p $C1; git -C $REPO archive --format=tar $SHA -- "${PS[@]}" | tar -xf - -C $C1; diskok control-1 $L/disk.log
python3 -I - $C1/$FE/SemanticModel.cs $C1/$FE/PublicAPI.txt <<'PY'
import sys
cs, api = sys.argv[1], sys.argv[2]
lines = open(cs, encoding="utf-8").read().split("\n")
sig = [i for i, l in enumerate(lines) if "public ComputedPropertyName? NamedEvaluationKey(SyntaxNode node)" in l]
assert len(sig) == 1, sig
i = sig[0]
start = i
while start > 0 and (lines[start - 1].lstrip().startswith("///") or lines[start - 1].lstrip().startswith("// Hairs-")):
    start -= 1
assert lines[start].lstrip().startswith("/// <summary>"), lines[start]
end = i + 1
assert lines[end].strip() == "{"
depth = 0
while True:
    depth += lines[end].count("{") - lines[end].count("}")
    if depth == 0:
        break
    end += 1
del lines[start:end + 1]
open(cs, "w", encoding="utf-8", newline="").write("\n".join(lines))
al = open(api, encoding="utf-8").read().split("\n")
hit = [l for l in al if l.startswith("Broiler.ECMAScript.Semantics.SemanticModel.NamedEvaluationKey(")]
assert len(hit) == 1, hit
open(api, "w", encoding="utf-8", newline="").write("\n".join(l for l in al if l not in hit))
print("control 1: removed SemanticModel.cs lines", start + 1, "to", end + 1, "and the PublicAPI.txt line", hit[0])
PY
echo "control-1 edit rc=$?" >> $L/commit.txt
( cd $BLD && diff -u src/$FE/SemanticModel.cs control-1/$FE/SemanticModel.cs > $OUT/control-1-SemanticModel.diff; diff -u src/$FE/PublicAPI.txt control-1/$FE/PublicAPI.txt > $OUT/control-1-PublicAPI.diff )
mc $C1 control-1 --control-absent "Broiler.ECMAScript.Semantics.SemanticModel.NamedEvaluationKey("
# Control 2, a scratch archive.
chk before-control-2; C2=$BLD/control-2; mkdir -p $C2; git -C $REPO archive --format=tar $SHA -- "${PS[@]}" | tar -xf - -C $C2; diskok control-2 $L/disk.log
cat > $C2/$FE/EngineStub.cs <<'CS'
namespace Broiler.VM.Witness
{
    /// <summary>A public stub type in an engine namespace (part H control 2).</summary>
    public sealed class EngineStub
    {
    }
}
CS
cp $C2/$FE/EngineStub.cs $OUT/control-2-EngineStub.cs.txt
printf 'Broiler.VM.Witness.EngineStub sealed class\n' >> $C2/$FE/PublicAPI.txt
( cd $BLD && diff -u src/$FE/PublicAPI.txt control-2/$FE/PublicAPI.txt > $OUT/control-2-PublicAPI.diff )
mc $C2 control-2 --control-engine "Broiler.VM.Witness.EngineStub"
echo "ended: $(date -u +%FT%TZ)" >> $L/commit.txt
deltree $BLD $L/disk.log
exit 0
