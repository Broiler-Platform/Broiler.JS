# The base commit's local runs: parts C, D, E, G and H (retained; part C classified in ../part-c/)

**Taken:** 2026-11-22 to 2026-11-23, at the base commit `69a8a13bc446a1714d3c8be9be3cfd742aebee6e` (the merge of
Broiler-Platform/Broiler.JS#1128), during W2's hold, with the default branch's head read as that commit before every
step. **Retained:** 2026-11-23, on branch `sjl-14-w2-base`. These are the local base runs
[`../../decision-rule.md`](../../decision-rule.md) names: part C's side-by-side tool, its test projects with the switch
off and on and its test262 runs with the shell's hidden switch off and on ("at the base commit (W2's first parent, the
last state before the cut-over, every dark commit included)"); part D's tool at the base commit; part E's collector
"run locally on linux-x64 at the base commit"; part G's base candidate feed and both suites against it; and part H's
API lister at the base commit. Part A's base run is the hosted one in [`../part-a/`](../part-a/imported.txt). Part C's
classes are read in [`../part-c/`](../part-c/README.md); parts D, E, G and H compare the base with the after commit,
which does not exist, so their runs here are read and not judged.

## What was run

[`retained.txt`](retained.txt) names every step, its command, its outputs and its exit. In short, from a clean
`git archive` of the base commit on disk, outside every working tree, each step through the machine-wide lock and idle
gate ([`scripts/run.sh`](scripts/run.sh)), every run under `env -i` with `TZ=UTC` and no `LANG`, `LC_*` or `BROILER_*`
variable:
- **Part C** (by [`scripts/step.sh`](scripts/step.sh), the same script as W1's): the builds of `Broiler.JS.slnx` in
  Release with the compiler's test-only switch off ([`build-off.log`](build-off.log)) and on, by
  `-p:BroilerParseThroughBridge=true` ([`build-on.log`](build-on.log)); every test project with each
  ([`tests-off/`](tests-off/summary.txt), [`tests-on/`](tests-on/summary.txt)); test262 in shards with negative tests,
  both variants and one worker per processor, through the switch-off build's shell with its hidden switch off
  ([`t262-off/`](t262-off/merge.log)) and on ([`t262-on/`](t262-on/merge.log)), merged, each timed-out path run again
  alone. By [`scripts/bstep.sh`](scripts/bstep.sh)'s `tools` step: the side-by-side tool at P, B, K and K-on with the
  dumps ([`part-c-tool/`](part-c-tool/spancheck-counts.txt); the report itself by digest), the dumps' text at P and B,
  and `spancheck.py` over it.
- **Part D**: bundle `sjl-12-001`'s tool in its default mode, entries P and K
  ([`part-d/parse-corpus-c.tsv.gz`](part-d/parse-corpus-c.tsv.gz)), and with `--dump` (by digest), as part B's runs
  were taken ([`../../bridge/`](../../bridge/README.md)).
- **Part E**: `scripts/performance/collect_phase5.py --rid linux-x64` ([`part-e/`](part-e/phase5-report.json)).
- **Part G**: the base candidate feed packed by `eng/pack.ps1` and checked by `eng/verify-feed.ps1`
  ([`logs-pack/`](logs-pack/runs.tsv), [`part-g/candidate-feed.sha256.txt`](part-g/candidate-feed.sha256.txt));
  Broiler.JSeal's `eng/test-package-consumer.ps1` against it ([`part-g/jseal/`](part-g/jseal/report.json), with its
  earlier attempts kept); Broiler.HtmlBridge's suite against it in a scratch copy
  ([`part-g/htmlbridge/`](part-g/htmlbridge/scratch-overrides.diff)); at the commits the bundle named on 2026-11-19.
- **Part H**: the API lister over every assembly the packages carry ([`part-h/api/`](part-h/projects.txt)).

The drivers, the chain's log and the memory log are here as [`driver.log`](driver.log), [`chain.log`](chain.log) and
[`memwatch.log`](memwatch.log), with the scripts in [`scripts/`](scripts/). Container restarts interrupted some steps;
each was taken again from its start ([`retained.txt`](retained.txt)).

## What is retained

Every file of the run folder, unchanged, at the same path, but the files over 2 MB, which are retained by digest only
([`retained.txt`](retained.txt), [`not-retained.sha256.txt`](not-retained.sha256.txt)). [`hashes.txt`](hashes.txt)
gives the SHA-256 of every file here, and a [`.gitattributes`](.gitattributes) keeps the bytes as committed. Three of
the files kept by digest only are ones the after comparisons read, and their only copy is in the run folder, which is
not durable: part C's report (which [`../part-c/scripts/read_base.sh`](../part-c/scripts/read_base.sh) reads there),
part D's dump report and Broiler.HtmlBridge's TRX; the run folder is to be kept until those comparisons are taken,
unless the orchestrator has them kept whole here. The build trees' `bin` and `obj` folders, outside the bundle, were
deleted by this stage before review, against its brief; nothing retained lived there, but the shell binaries' digests
can no longer be checked against the binaries that ran, and any further run needs a build behind the gate
([`retained.txt`](retained.txt)). *(Both added the same day, after review.)* *(Noted 2026-11-23, records part 4:
those three files are now kept whole here at their paths, [`part-c-tool/side-by-side.tsv.gz`](part-c-tool/side-by-side.tsv.gz),
[`part-d/parse-corpus-d1.tsv.gz`](part-d/parse-corpus-d1.tsv.gz) and
[`part-g/htmlbridge/trx/htmlbridge.trx`](part-g/htmlbridge/trx/htmlbridge.trx), copied unchanged from the run folder
and each matching the digest `not-retained.sha256.txt` gave it; their lines leave that file, their digests are in
`hashes.txt`, and `.gitattributes` marks them binary. A decision taken under the owner's direction; the alternative not
taken was to keep them only in the run folder, which the session's container does not keep. The sentence "the run
folder is to be kept until those comparisons are taken, unless the orchestrator has them kept whole here" read the
folder before it. The other files over 2 MB are still kept by digest only. The build trees are not rebuilt here: part
C's records part 4 took a fresh build of the base commit for its reading aids, recorded in
[`../part-c/README.md`](../part-c/README.md).)*

## What it shows

A reading; part C's classes are in [`../part-c/`](../part-c/README.md), and parts D, E, G and H are not judged.
- **Every step completed** but two, as the chain's log records: part E's collector exited 1, its report reading
  "offline compiler output differs from the checked-in program", and Broiler.JSeal's consumer exited 1 in its first
  attempts, each stopped by an omission of the session's harness, before the attempt that passed. Both builds succeeded with no warning. Every test262 shard of both runs wrote its report and
  passed its self-check, with the same shell binaries in every shard.
- **Part E.** The regenerated program differs from the checked-in `FibonacciProgram.g.cs` only in the form of its
  constants: by the repository's full history, the copy checked in was last written at `3a8f3022`
  (`3a8f3022b54c490973ecb134d7fdd4be0fde8cdf`, 2026-07-17), before `ef676932`
  (`ef6769322b2189d413f1c3649df3e7aa6aaffcda`, SJL-3.7, 2026-10-13), since which the portable compiler writes each number constant as
  `System.BitConverter.UInt64BitsToDouble(0x...UL)`, and the regenerated program has that form. The sample published
  with Native AOT and ran, with no dynamic code (the report's `nativeAot` fields). Part E is judged at the after commit
  only; this is its base record. *(Changed the same day, before this record merged: the bullet's first sentence named
  "`68f9a87a` (Broiler-Platform/Broiler.JS#1061)" and "`588ca262` (SJL-12.5), whose emitter writes each constant as" that
  form, names read from this machine's shallow clone, at whose graft boundaries both commits stand; the chain's log
  records the correction at 2026-11-23T12:28:11Z, and `scripts/chain2.sh`, retained as it ran, keeps the earlier
  names.)* *(Changed again the same day, after review: the forty-digit ids are added beside the eight-digit names,
  which this machine's shallow clone cannot resolve; GitHub's commit pages give them.)*
- **Part G.** Both suites ran against the base candidate: Broiler.JSeal's consumer completed, and every test of
  Broiler.HtmlBridge that ran passed (its TRX by digest). **Not taken: every test's result of Broiler.JSeal's suites.**
  The consumer's test commands ran with no test logger, so its report keeps each suite's summary and its skipped tests'
  names, not each passed test, while part G asks every test's result retained and compares per test; whether the base
  run is taken again with per-test output or read by summary is the orchestrator's to decide, and the after run's
  harness should keep a TRX for the same suites. *(Added the same day, after review.)* The after run must use the same harness: Broiler.JSeal restored
  in Release-VM first, and a candidate directory that holds every package the after commit's packages introduce, at the
  versions they declare.
- **Parts D and H**: their reports, read and not compared, since each compares with the after commit.
