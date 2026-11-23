# Part C's other runs at W1: the test projects and test262, the switch off and on (read, not judged)

**Taken:** 2026-11-19 to 2026-11-22, at W1, `8e5519d8cf81808856e32f2275fcb4d3b98a73f3` (the merge of
Broiler-Platform/Broiler.JS#1102), during W2's base phase, with the default branch's head read as
`69a8a13bc446a1714d3c8be9be3cfd742aebee6e` before every step. **Retained:** 2026-11-22. These are the runs part C of
[`../../../decision-rule.md`](../../../decision-rule.md) names besides the side-by-side tool's, "every test project with
the switch on through its internal options member; scripts/compliance/run_test262.py locally on the programme's machine
through the shell built at that commit with its hidden switch off and on, in UTC, with negative tests, one worker per
core", taken as "the same runs at W1, retained and read, not judged". Its comparison reads "per test, its result off and
on; per test262 path and variant, its verdict off and on", so the test projects were run with the switch off too.
Nothing here is a verdict or a class: part C judges only the runs at the base commit. The side-by-side tool's run at W1
is the folder above, [`../README.md`](../README.md).

## What was run

[`retained.txt`](retained.txt) names every step, its command, its outputs and its exit; in short:
- **The tree**: a clean `git archive` of W1 on disk, outside every working tree ([`archive.txt`](archive.txt)), with
  the SDK, node and python it ran with and the machine.
- **The builds** of `Broiler.JS.slnx` in Release with no build server: the compiler's test-only switch at its default,
  off ([`build-off.log`](build-off.log)), and with `-p:BroilerParseThroughBridge=true`, which turns it on for every
  compile through the internal `JSCompilationOptions.BridgeByDefault`, the switch-on pass over the test projects that
  decision D43's note in [`docs/roadmap/SharedLibraries.md`](../../../../../roadmap/SharedLibraries.md) records
  ([`build-on.log`](build-on.log), where each project reports the switch on). The shell's binaries of each are in
  [`shell-binaries-off.sha256`](shell-binaries-off.sha256) and [`shell-binaries-on.sha256`](shell-binaries-on.sha256).
- **Every test project** of `Broiler.JS.slnx` under `Broiler.JS/`, one `dotnet test` each, on the switch-off build
  ([`tests-off/`](tests-off/summary.txt)) and on the switch-on build ([`tests-on/`](tests-on/summary.txt)), each with
  its projects' exits, logs and TRX reports.
- **test262** by the tree's `scripts/compliance/run_test262.py`, in shards over the whole selection part A takes, with
  negative tests, original and Terser variants, one worker per processor of the machine, through the shell of the
  switch-off build, with its hidden switch off ([`t262-off/`](t262-off/merge.log)) and on, by
  `--shell-arg=--parse-through-bridge` ([`t262-on/`](t262-on/merge.log)); each shard after the runner's self-check; the
  shards merged by `merge_test262_shards.py`; and each timed-out path run again alone with one worker, as
  `test262-evidence.yml`'s re-run does. Every shard of both runs used the same shell binaries. The shards, the
  runner's options and its timeout are in [`retained.txt`](retained.txt), and the machine's processors in
  [`archive.txt`](archive.txt).
- **The environment** of every test and test262 run: `env -i` with `TZ=UTC` and no `LANG`, `LC_*` or `BROILER_*`
  variable.
- **The drivers**: [`scripts/drive.sh`](scripts/drive.sh) ran each step of [`scripts/step.sh`](scripts/step.sh) through
  [`scripts/run.sh`](scripts/run.sh), the machine-wide lock and idle gate ([`scripts/gate.sh`](scripts/gate.sh)), with
  the machine's record before and after each ([`logs/`](logs/runs.tsv): each step's command, exit, timing and machine
  records), and read the default branch's head before each step ([`driver.log`](driver.log)); after the stage's agent
  was lost to a container restart, [`scripts/chain.sh`](scripts/chain.sh) ran the same driver again
  ([`chain.log`](chain.log)). A step whose marker is in [`done/`](done/tests-on) is skipped, so a restarted driver
  resumes after the last finished step. The retained `drive.sh` is the file as it was on disk from
  2026-11-19T18:45:47Z, which the chain ran from 23:36:24Z; the first driver started at 17:51:58Z, before that change,
  and the version it started with is not on disk. The chain runs W1's driver again first at each of its starts, so
  `driver.log` grows with each restart: it is copied as it stood on 2026-11-22T21:59Z, and its lines after W1's last
  step's exit (`tests-on`, ended 2026-11-22T14:51:14Z) are the driver's "all given steps done" and, after each later
  restart, the chain's skips of W1's finished steps.

**Interrupted and taken again from its start**, each by a container restart: the switch-on test262 shard 1 (step
`t262-on 1`), on 2026-11-19, and the switch-on test projects (step `tests-on`), begun on 2026-11-20 and taken again on
2026-11-22. `logs/commands.log` names each for both its starts; the completed run alone is in `logs/runs.tsv` and its outputs, as
`step.sh` writes a shard's files afresh and removes `tests-on/` before it starts.

## What is retained

Every file of the run folder, unchanged, at the same path, but the files over 2 MB: the shards' and the merged test262
reports, and the TRX reports of `Broiler.JavaScript.BuiltIns.Tests`, `Broiler.JavaScript.Compiler.Tests` and
`Broiler.JavaScript.Integration.Tests` in each test run, which are retained by digest only
([`retained.txt`](retained.txt), [`not-retained.sha256.txt`](not-retained.sha256.txt)). The runs' per-test and
per-case results are kept whole, in the reading below. The scripts are copied in as they were on disk;
[`retained.txt`](retained.txt) gives their times. [`hashes.txt`](hashes.txt) gives the SHA-256 of every file here, and
a [`.gitattributes`](.gitattributes) keeps the bytes as committed.

**The reading**, by [`scripts/read_local.py`](scripts/read_local.py), which classifies nothing: every test's outcomes
off and on ([`tests-off.tsv.gz`](tests-off.tsv.gz), [`tests-on.tsv.gz`](tests-on.tsv.gz)) and those that differ
([`tests-differ.tsv`](tests-differ.tsv)); every test262 path and variant's status off and on, after the re-run's rows
([`t262-off.tsv.gz`](t262-off.tsv.gz), [`t262-on.tsv.gz`](t262-on.tsv.gz)), and those that differ, with the rows of
[`../../../corrections/k-paths.tsv`](../../../corrections/k-paths.tsv) and
[`../../../corrections/test262-paths.tsv`](../../../corrections/test262-paths.tsv) that name each path
([`t262-differ.tsv.gz`](t262-differ.tsv.gz)); and the counts ([`counts.txt`](counts.txt)). A name in those tables is a
listing, not a class.

## What it shows

A reading, not a verdict; each count is in the files above, not here.
- **Both builds succeeded** with no warning, and every step's exit is 0. Every test262 shard of both runs wrote its
  report and passed its self-check; the runner's own exit is 1 in each, as it is whenever a case fails or times out.
- **The test projects.** With the switch off, a timing test of the compiler's fails,
  `DeclarationDenseSourceTests.ManyTopLevelDeclarations_CompileInTimeLinearInTheirCount`, whose message names the
  scaling bound and host contention; it passes with the switch on. With the switch on, these tests fail that pass with
  it off: the architecture test that the switch is off in the build,
  `BridgeSwitchTests.The_compilers_test_only_switch_is_off_in_this_build`, which fails in a switch-on build by its own
  message, and the compiler's deep-nesting tests `DeeplyNestedSourceTests.DeepBinaryExpressionChain_Compiles`,
  `DeepConditionalExpressionNesting_Compiles` and `DeepConditionalExpressionNesting_PastTheCompilationStack_Compiles`,
  refused with the library's nesting messages, and `DeepBinaryExpressionChain_OnAOneMebibyteThread_Compiles`, failing
  its `Assert.Null` ([`tests-differ.tsv`](tests-differ.tsv)). The same tests run in both.
- **test262.** The same paths and variants run in both. Most cases whose status moves go from failing with the switch
  off to passing with it on, and a corrections table names every such path. These move the other way: both variants of
  `test/staging/sm/regress/regress-610026.js`, which fail with the switch on with no stderr line, and the Terser variant
  of `test/language/expressions/dynamic-import/syntax/valid/new-covered-expression-is-valid.js`, which passes off and is
  skipped on, the runner reading its minified body as one no engine accepts; no corrections table names either path
  ([`t262-differ.tsv.gz`](t262-differ.tsv.gz)). `test/staging/sm/regress/regress-1507322-deep-weakmap.js` times out in
  both runs, both variants, and in its re-run alone ([`t262-off/timed-out-paths.txt`](t262-off/timed-out-paths.txt),
  [`t262-on/timed-out-paths.txt`](t262-on/timed-out-paths.txt)).
