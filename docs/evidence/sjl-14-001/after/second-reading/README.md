# The second readings of parts A, D and K, under the rule's revision of 2026-11-24 (a reading aid)

**Read:** 2026-11-24, on branch `sjl-14-after`, by [`read_2.py`](read_2.py), from retained tables alone: part A's
comparison ([`../part-a/comparison/comparison.json`](../part-a/comparison/comparison.json), `classifications.json` and
`named-rows.log`), part D's classifier tables ([`../part-d/parser/`](../part-d/parser/classes.txt) and
[`../part-d/pass/`](../part-d/pass/classes.txt): `differences.tsv.gz`, `unclassified.tsv`, `unclassified-lines.txt`),
and part K's [`../part-k/hits.tsv`](../part-k/hits.tsv) with the listing aid
[`../part-k/listing.tsv`](../part-k/listing.tsv). Nothing was built or run, and no retained file changes. Each part's
rows are read under the classes [`../../decision-rule-revised-2026-11-24.md`](../../decision-rule-revised-2026-11-24.md)
adds, each check written out in the script's docstring; [`reading.txt`](reading.txt) regenerates byte for byte
(`python3 -I read_2.py ../.. > reading.txt`, run from this folder). Part J's corrected session is read in
[`../part-j-corrected/findings.txt`](../part-j-corrected/findings.txt). [`hashes.txt`](hashes.txt) gives every file's
SHA-256. The readings are the bundle README's ("W2's after commit, records part 2, recorded on 2026-11-24").
