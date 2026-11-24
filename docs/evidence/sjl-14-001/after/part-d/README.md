# Part D across the cut-over: every difference with its class, or unclassified

**Compared:** 2026-11-24, on branch `sjl-14-after`, by [`scripts/read_d.sh`](scripts/read_d.sh), from retained reports
alone: the base commit's [`../../base/local/part-d/`](../../base/local/part-d/parse-corpus-c.tsv.gz) and the after
commit's [`../local/partd/`](../local/partd/parse-corpus-c.tsv.gz), the after commit's reading aid
[`../local/dcodes/bridge-codes.tsv`](../local/dcodes/bridge-codes.tsv), and the two commits' dump texts, kept outside
the repository by digest ([`texts.sha256.txt`](texts.sha256.txt): the base's from
[`../owed-runs/dtextbase/`](../owed-runs/dtextbase/identity-with-base-d1.txt), the after's from
[`../local/dtext/`](../local/dtext/identity-with-partd.txt)). Nothing was built or run for it. Run twice, with the same
bytes both times. [`../../decision-rule.md`](../../decision-rule.md) and its revision of 2026-11-23 are not edited.

## How it is read

- **The comparison the rule names**, `scripts/compliance/compare_parse_corpus.py acceptance`, over the two default-mode
  reports with the manifest: [`acceptance.log`](acceptance.log) and [`acceptance.json.gz`](acceptance.json.gz). The script
  admits only bundle `sjl-12-001`'s classes, so it is given no classification file and lists every difference; the
  classes of this rule are read below. The control the after run took is read again in [`control.log`](control.log).
- **The classes**, by part C's own classifier, [`../../base/part-c/scripts/classify_c.py`](../../base/part-c/scripts/classify_c.py),
  unchanged, with the after commit's codes and the word `revised-2026-11-23`, over inputs written in the form of part
  C's side-by-side report by [`scripts/make_inputs.py`](scripts/make_inputs.py): its pair "P/B" reads P at the base
  commit against P at the after commit, and "K/K-on" K at the base commit against K at the after commit. The revision
  words part D's rev-code, rev-eof and rev-bracket as part C's with the base commit's P in place of P and the after
  commit's P in place of B, which is how the classifier reads them here. [`parser/`](parser/classes.txt) reads the
  parser trees' dumps; [`pass/`](pass/classes.txt) the dumps after the compiler's for-loop pass, which the dump report
  also hashes (`pass-sha256`; part B's configuration, "the --dump mode (before and after the compiler's for-loop
  pass)", which part D's names), and whose outcome rows are `parser/`'s. Each folder has the classifier's
  `differences.tsv.gz`, `span-readings.tsv.gz`, `unclassified.tsv` and `classes.txt`, and `spancheck.py`'s counts.
- **The two readings agree**: [`cross-check.txt`](cross-check.txt) finds every difference `acceptance.json.gz` lists
  among `parser/`'s outcome and message rows and the converse, and every input variant whose `parser-sha256` or
  `pass-sha256` differs among the dump rows of `parser/` or `pass/` and the converse.
- **The reports against part C's**: [`identity-with-part-c.txt`](identity-with-part-c.txt), by
  [`scripts/identity_with_part_c.py`](scripts/identity_with_part_c.py), finds the base commit's P and K rows and parser
  dump hashes equal to the base side-by-side report's P and K, and the after commit's to its B and K-on, row by row.
- **The pass trees' unclassified differences** are counted line by line, with each input variant's class at the parser
  trees, in [`pass/unclassified-lines.txt`](pass/unclassified-lines.txt).

[`hashes.txt`](hashes.txt) gives the SHA-256 of every file here; [`work.sha256.txt`](work.sha256.txt) those of the
inputs `make_inputs.py` and `spancheck.py` wrote, kept outside the repository. The reading is the bundle README's.

*(Noted 2026-11-24, after the comparison, in review of this record.)* The paragraphs above name the two dump texts as
"kept outside the repository by digest" ([`texts.sha256.txt`](texts.sha256.txt)) and `make_inputs.py`'s and
`spancheck.py`'s inputs as "kept outside the repository" ([`work.sha256.txt`](work.sha256.txt)). On 2026-11-24, once
this comparison was done, both texts (`/home/user/js-w2read-build/69a8a13b/text/dumps-p.txt.gz`, the base commit's,
and `/home/user/js-w2after-build/83a9bd82/text/dumps-p.txt.gz`, the after commit's), those work files and the after
commit's candidate feed were removed with the build folders that held them; they remain named by digest only. So
`parser/`'s and `pass/`'s tables regenerate only after the after driver's `dtext` step and the owed `dtextbase` step
are taken again (through `run.sh`, a build at each commit) and `read_d.sh` is run on their texts; `acceptance.log` and
`acceptance.json.gz`, which need no text, regenerate from the retained reports. The texts' own bytes were not shown
reproducible: `../local/dtext/identity-with-partd.txt` and `../owed-runs/dtextbase/identity-with-base-d1.txt` compare
the dump reports' rows with the `#` lines left out (among them each assembly's identifier), not the texts.
