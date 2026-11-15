# The side-by-side run at W1 (SJL-14.2; read, not judged)

**Taken:** 2026-11-15, at W1, `8e5519d8cf81808856e32f2275fcb4d3b98a73f3` (the merge of Broiler-Platform/Broiler.JS#1102),
for [`../../decision-rule.md`](../../decision-rule.md)'s part C, which takes the same runs at W1 "retained and read, not
judged" and judges only the runs at the base commit. "The run at W1 is retained so that a library fix found there can be
traced to its dark merge." Nothing here is a verdict or a class: the classes C1, C3, E, T, M and S are part C's, at the
base commit.

## What was run

- **The side-by-side tool**, [`../../tools/side-by-side/`](../../tools/side-by-side/README.md), as its README says: built
  in Release from a clean `git archive` of W1 on `/dev/shm`, outside the tree, with no build server, and run with
  `env -i`, so with `TZ=UTC` and no `LANG`, `LC_*` or `BROILER_*` variable, over every input and variant of
  `conformance/parse/corpus.manifest` at entry points P, B, K and K-on with `--dump`:
  [`side-by-side.tsv.gz`](side-by-side.tsv.gz). It was run by bundle part B's driver,
  [`../../bridge/scripts/commit.sh`](../../bridge/scripts/commit.sh), in the same hold of the machine's build lock as
  W1's part B runs; its build and run logs are in [`logs/`](logs/), and the commit's record in
  [`../../bridge/8e5519d8/logs/`](../../bridge/8e5519d8/logs/).
- **The dumps' text**, for class S's check: the tool again at W1, entry points P and B with `--dump` and `--text`, by
  [`scripts/w1text.sh`](scripts/w1text.sh) (its logs are `logs/text-*`). Its rows are the first run's at P and B, byte
  for byte ([`text-run-identity.txt`](text-run-identity.txt)). The dumps' text is too large to keep here; its SHA-256,
  and that of the span listing written from it, are in [`not-retained.sha256.txt`](not-retained.sha256.txt), and both
  are written again by the commands in the logs.
- **The readings**, each by a script that classifies nothing:
  - [`counts.txt`](counts.txt): `compare.py`'s counts of input variants by the pair of outcomes, P against B, K against
    K-on, and P against K;
  - [`messages-p-b.tsv.gz`](messages-p-b.tsv.gz) and [`messages-k.tsv.gz`](messages-k.tsv.gz): `compare.py --messages`,
    the generated tables of refusals at both entry points with the same type and another text (class M's table,
    decision D46);
  - [`outcome-moves.tsv.gz`](outcome-moves.tsv.gz) and [`outcome-moves-counts.txt`](outcome-moves-counts.txt):
    [`scripts/read_w1.py`](scripts/read_w1.py), every input variant whose outcome moves between P and B or between K and
    K-on, with whether [`../../corrections/test262-paths.tsv`](../../corrections/test262-paths.tsv) lists it, its
    expected outcome and the correction that explains it, or [`../../corrections/k-paths.tsv`](../../corrections/k-paths.tsv)
    names its id;
  - [`spancheck-counts.txt`](spancheck-counts.txt) and [`spans-not-s.tsv.gz`](spans-not-s.tsv.gz): `spancheck.py` over
    the dumps' text, its counts by class and node kind, and every row of its listing that is not `S` (`both-agree` and
    `not-span`, with both texts).
- [`hashes.txt`](hashes.txt) gives the SHA-256 of every file here.

**Not run at W1:** part C's other runs, every test project with the switch on through its internal options member,
and `scripts/compliance/run_test262.py` locally with the shell's hidden switch off and on. The rule takes them at W1 too,
read and not judged; this stage took the tool's run, which is what traces a library fix to its dark merge, and leaves
those to the base commit, where part C judges them (the bundle's README, "The side-by-side run at W1, read on
2026-11-15", records the choice).

## What it finds

- **Outcomes, P against B and K against K-on.** Every input variant whose outcome moves and which the corrections list
  moves to the outcome the list expects. The moves the list does not hold are of these kinds: corpus inputs P accepts
  and B refuses while K refuses on both sides, which is class E's form (B's messages among them a strict-mode `with`,
  `eval` or `arguments` as a binding, a duplicate parameter and a legacy octal); corpus inputs the switch-on compile
  refuses for nesting past the library's bound, which is class C3's form; and inputs from this engine's own parser
  tests that the old path accepted and the text refuses (`var a = b % c ^ d | e & f ~ 0;`, `let await` in a class static block, and
  `yield` in a class field initialiser inside a generator). Those are corrections that no row lists, for part C
  at the base commit to classify, as W1a to W1c were added; none is a library defect.
- **Refusals with another text** are many, at both pairs, as decision D46 expects; their tables are generated here.
- **Trees, P against B.** Most accepted variants' dumps differ, and `spancheck.py` reads every difference as one of
  spans alone, either class S's form (P's span disagrees with the node's text and B's agrees) or both spans agreeing
  with the text (`both-agree`, two readings of one construct's extent), but for the inputs whose trees differ outside
  spans, which are exactly the inputs the corrections W1a, W1b and W1c cite (T042384, T052945, and T047171,
  T047173 and T031800). No span difference is unclassified.
- **Crashes.** K and K-on crash alike on the module inputs that read `import.meta`, which the tool's compile gives no
  module record; the switch-on compile refuses, with a SyntaxError, inputs the old compile crashed on, each of them
  listed with its correction.

**No library defect is found**, so no dark merge is asked for by this run. Each count is in the files above, not in this
README.
