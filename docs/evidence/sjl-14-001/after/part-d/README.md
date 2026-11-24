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
- **The pass trees' unclassified differences** are counted line by line, with each input variant's class at the parser
  trees, in [`pass/unclassified-lines.txt`](pass/unclassified-lines.txt).

[`hashes.txt`](hashes.txt) gives the SHA-256 of every file here; [`work.sha256.txt`](work.sha256.txt) those of the
inputs `make_inputs.py` and `spancheck.py` wrote, kept outside the repository. The reading is the bundle README's.
