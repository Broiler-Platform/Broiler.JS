#!/usr/bin/env bash
# identity.sh <first-parent dir> <merge dir> : part B's comparison of one pair (bundle sjl-14-001's rule, part B).
# Default-mode reports (P, K): outcome, exception type and message per input, variant and entry, by the side-by-side
# tool's compare.py --same (P,K); --dump reports (slot 1): P's outcome and the dump hashes before and after the
# for-loop pass, by compare.py --same (1). Completeness: each report ends with "#% end", names the manifest digest the
# repository's conformance/parse/corpus.manifest has, and both runs hold the same rows in the same order.
CMP=/home/user/js-s14b/docs/evidence/sjl-14-001/tools/side-by-side/compare.py
A=$1; B=$2; rc=0
MD=$(sha256sum /home/user/js-s14b/conformance/parse/corpus.manifest | cut -d' ' -f1)
echo "manifest sha256 (conformance/parse/corpus.manifest): $MD"
for f in parse-corpus-c.tsv.gz parse-corpus-d1.tsv.gz; do
  for d in $A $B; do
    end=$(zcat $d/$f | tail -1); md=$(zcat $d/$f | grep -m1 '^#% manifest-sha256' | cut -f2)
    rows=$(zcat $d/$f | grep -vc '^#%\|^id	')
    echo "$(basename $d)/$f: last line [$end], manifest-sha256 $md, rows $rows"
    case "$end" in "#% end	$rows") ;; *) echo "  INCOMPLETE"; rc=1;; esac
    [ "$md" = "$MD" ] || { echo "  MANIFEST DIGEST DIFFERS"; rc=1; }
  done
done
echo "--- default mode, entries P and K: compare.py --same"
python3 -I $CMP --same $A/parse-corpus-c.tsv.gz $B/parse-corpus-c.tsv.gz P,K || rc=1
echo "--- --dump mode, slot 1: compare.py --same"
python3 -I $CMP --same $A/parse-corpus-d1.tsv.gz $B/parse-corpus-d1.tsv.gz 1 || rc=1
echo "--- every row line (headers aside), byte for byte"
for f in parse-corpus-c.tsv.gz parse-corpus-d1.tsv.gz; do
  if cmp -s <(zcat $A/$f | grep -v '^#%') <(zcat $B/$f | grep -v '^#%'); then echo "$f: rows identical"; else echo "$f: rows DIFFER"; rc=1; fi
done
echo "--- header lines that differ (each product assembly's module version id, which identifies its build)"
diff <(zcat $A/parse-corpus-c.tsv.gz | grep '^#%') <(zcat $B/parse-corpus-c.tsv.gz | grep '^#%') | sed 's/^/  /'
echo "identity: $([ $rc = 0 ] && echo identical || echo NOT-identical)"
exit $rc
