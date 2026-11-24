# Part B's runs at the first model commit, `be6c4a5c`

**Taken:** 2026-11-24, after the merge of SJL-15.2's stage 1 (Broiler-Platform/Broiler.JS#1132), for
[`../decision-rule.md`](../decision-rule.md)'s part B. These are the rule's first runs. Their reading is in the bundle's
[`README.md`](../README.md), the note of 2026-11-24 on part B's runs at `be6c4a5c`. Nothing here changes the rule, a tool
or a product file.

## What part B asks

"population: every input and variant of conformance/parse/corpus.manifest"; "configuration: bundle sjl-12-001's tool,
byte-unchanged and built as bundle sjl-14-001's rule says, in Release against each commit's projects, on one thread with
a 256 MB stack, no LANG and TZ=UTC: entry points P and K, and the --dump mode, at each model commit after W2 and at its
first parent; and every test project of Broiler.JS.slnx at the model commit by its CI run"; "comparison: per input,
variant and entry point, the outcome, the exception type and the message; per input, the dumps' hashes"; "admitted
difference classes: none". "MET if and only if, at every model commit after W2, both runs are complete and identical in
every field, and the commit's CI run of every test project passed."

## The commits

| Folder | Commit | What it is |
|---|---|---|
| [`be6c4a5c/`](be6c4a5c/logs/commit.txt) | `be6c4a5c3e389c7eb2aff71c34e31bdae5efe276` | the first model commit after W2: the merge of Broiler-Platform/Broiler.JS#1132 (head `f0418c364fdfd4b82d91e90aebfe3a3d86285bc5`), SJL-15.2's stage 1 |
| [`8b1fa311/`](8b1fa311/logs/commit.txt) | `8b1fa311da6af71baac6378e8b4f8597a99c1312` | its first parent, the merge of Broiler-Platform/Broiler.JS#1131 (corpus revision 44) |
| [`8b1fa311-attempt1-disk-gate/`](8b1fa311-attempt1-disk-gate/logs/commit.txt) | `8b1fa311da6af71baac6378e8b4f8597a99c1312` | a first attempt at the first parent, stopped at the disk gate before its dump run: **set aside, not compared** |

`git rev-parse be6c4a5c^1` is `8b1fa311da6af71baac6378e8b4f8597a99c1312`. Bundle `sjl-12-001`'s tool,
`docs/evidence/sjl-12-001/tools/parse-corpus`, is the same tree at both commits, `5ede61145de01136368c49354acc1de1c469fcb6`,
the tree it had at W1 and its dark commits (bundle `sjl-14-001`'s `bridge/README.md`), so it ran byte-unchanged; the
manifest `conformance/parse/corpus.manifest` is the same blob at both, `2419aa2aabda30a9d3ca9aa703d38c6455859441`
(SHA-256 `1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc`), and so is
`scripts/compliance/compare_parse_corpus.py`, `c0685c65899e74e5e2b0a11a270ef3ee65f3c669`
([`identity/inputs.txt`](identity/inputs.txt)).

## What was run

- **The local runs**, per commit, by [`scripts/pb.sh`](scripts/pb.sh), called once per commit, one at a time, through
  the machine's run script, which holds the build lock behind its idle gate (the same `run.sh`, `gate.sh`, `machine.sh`
  and `timed.py` as bundle `sjl-14-001`'s [`bridge/scripts/`](../../sjl-14-001/bridge/scripts/run.sh), byte for byte): a
  clean `git archive` of the commit on disk, outside every working tree; the tool built from it in Release with no build
  server, outside the tree; and run with `env -i`, so with `TZ=UTC` and no `LANG`, `LC_*` or `BROILER_*` variable, on
  the tool's own thread of 256 MB:
  - `parse-corpus-c.tsv.gz`, the default mode: entry points P and K for every input and variant of the manifest;
  - `parse-corpus-d1.tsv.gz`, `--dump --order manifest`: P's tree and the for-loop pass's tree, hashed by the canonical
    dump.
  The suite the manifest's test262 rows read is Broiler.VM's archive of test262 at the manifest's pin, `ccaac100`,
  SHA-256 `f58ce79141529c9fa33592e22ff3ff0d83b83830ac8e372ecd32e1623db1c040`, checked against the manifest's
  `pin-test262-archive` line before each run and extracted afresh for it (`logs/suite.log`). The tool built with no
  warning and no error at both commits (`logs/build-parse-corpus.stdout.txt`).
- **The comparison**, in [`identity/`](identity/commands.txt), with the commands as run:
  - `scripts/compliance/compare_parse_corpus.py acceptance`, base `8b1fa311`, after `be6c4a5c`, the manifest at
    `be6c4a5c`, no classification file: [`identity/acceptance.txt`](identity/acceptance.txt) and `acceptance.json`. The
    script refuses runs that do not report every manifest input and variant at P and K, so it checks the default
    mode's completeness.
  - [`scripts/identity.py`](scripts/identity.py) over both modes' reports, every row line byte for byte and each
    report's end line: [`identity/rows-c.txt`](identity/rows-c.txt) (P's and K's outcome, exception type and message)
    and [`identity/rows-d1.txt`](identity/rows-d1.txt) (per input and variant, P's outcome and both dumps' hashes).
  - [`scripts/completeness.py`](scripts/completeness.py): each of the four reports holds exactly the manifest's inputs
    and variants, at P and K or once for the dumps, with its end line, and its outcomes counted, so that a run that read
    no input would show ([`identity/completeness.txt`](identity/completeness.txt)).
- **The CI runs at `be6c4a5c`**, in [`ci/`](ci/commands.txt): the runs GitHub Actions made on the push of `be6c4a5c` to
  the default branch, read by read-only API calls and kept as the API returned them, as bundle `sjl-14-001`'s
  [`bridge/w1-ci/`](../../sjl-14-001/bridge/w1-ci/commands.txt) kept W1's: each run's record, its jobs with their steps,
  its artifacts with their digests, and its job logs, with [`ci/test-projects.tsv`](ci/test-projects.tsv), every test
  project of `Broiler.JS.slnx` at `be6c4a5c` with the result line each test job wrote for it.

Each commit's `logs/` holds its record (`commit.txt`: the commit, its tree and first parent, the tool's tree, the
manifest's blob, the suite and its SHA-256, the SDK and the machine), the commands as run (`commands.log`), each step's
exit, time and peak memory (`runs.tsv`, `*.time.txt`), the build's and the runs' output, the disk checks
(`disk.log`), the built binaries' SHA-256 (`binaries.sha256.txt`), and the gate's records of the machine before and after
(`gate-*.txt`, `gate-machine-sh.txt.gz` compressed because its lines end in spaces); the folder holds the reports' SHA-256 (`reports.sha256.txt`). Each report's header is the tool's own
record of its run: the mode, the manifest's digest and rows, the order, one thread and its stack, the environment
(`TZ=UTC`, `LANG`, `LC_*` and `BROILER_*` unset), the invariant culture, the time zone `UTC`, the runtime, the system,
the processors, and each engine assembly's name, version and module version id. [`logs/`](logs/runs.tsv) holds the
stage's logs: the checks of the default branch before each step (`main-checks.log`), the gate's log, the outer
commands and runs, and the stage's notes (`notes.log`).

## What the runs say

- **Both runs are complete.** At each commit the default mode reports every input and variant of the manifest at P and
  K and the dump mode every input and variant once, each report ending with its end line; the outcomes are counted the
  same at both commits ([`identity/completeness.txt`](identity/completeness.txt)).
- **They are identical in every field.** `compare_parse_corpus.py acceptance` finds no difference, with nothing to
  admit; `identity.py` finds every row line of both modes the same bytes in the same order ([`identity/`](identity/)).
- **The headers differ only in each engine assembly's module version id**, five lines per report, every other header
  line the same. The module version id is a build's name, not an answer: two builds of one commit, `8b1fa311`, the
  stage-1 rehearsal's in another build tree and part B's here, differ in the same five lines in the same way, with every
  row line identical ([`identity/two-builds-8b1fa311/`](identity/two-builds-8b1fa311/commands.txt)); and the stage's
  review rebuilt both commits in part B's own build tree, getting reports byte-identical to part B's, module version
  ids included (the reports' SHA-256 in [`identity/review-rebuilds/`](identity/review-rebuilds/runs.tsv) equal each
  commit's `reports.sha256.txt` here; those reports stay in scratch).
- **The CI runs.** Both workflows that run the test projects of `Broiler.JS.slnx` made one push run at `be6c4a5c`
  ([`ci/runs-at-be6c4a5c.json`](ci/runs-at-be6c4a5c.json)):
  - **Shared libraries** (`.github/workflows/shared-libraries.yml`), run 45455636086, attempt 1, head
    `be6c4a5c3e389c7eb2aff71c34e31bdae5efe276`, concluded `success`, every job `success`: Build and SL rules
    (ubuntu-24.04) 138664188516 and (windows-latest) 138664188152, which run the six `Broiler.ECMAScript.*` test
    projects; Native AOT probe (linux-x64) 138664188345 and (win-x64) 138664187668; Assurance check (SL8) 138664188377;
    Pack, validate and restore the shared stream (dry run) 138664188431.
  - **CI** (`.github/workflows/ci.yml`), run 45455636089, head `be6c4a5c3e389c7eb2aff71c34e31bdae5efe276`. **Its
    first attempt concluded `failure`**: Changed paths 138664187692, ubuntu-24.04 / Release 138664605408, engine
    vectors (ubuntu-24.04) 138664605531, (windows-latest) 138664605462 and (Windows against Linux) 138674086595 were
    `success`; windows-latest / Release 138664605556 was `failure`, and CI result 138674086204 `failure` with it. That
    job failed in its Test step. Every `Broiler.JavaScript.*` project it reached wrote "Passed!" with no test failed;
    then the test host of `Broiler.ECMAScript.Algorithms.Tests`, a project the step's filter selects no test of, died
    before running any: "The active test run was aborted. Reason: Test host process crashed : Process path:
    D:\a\Broiler.JS\Broiler.JS\Broiler.ECMAScript\Broiler.ECMAScript.Algorithms.Tests\bin\Release\net10.0\testhost.exe",
    "Unhandled exception. Microsoft.VisualStudio.TestPlatform.ObjectModel.TestPlatformException: testhost process
    failed to connect to datacollector process after 90 seconds. This may occur due to machine slowness, please set
    environment variable VSTEST_CONNECTION_TIMEOUT", "Test Run Aborted." (its log, `3_windows-latest _ Release.txt`, in
    [`ci/read-2026-11-24T1826Z/logs-45455636089.zip`](ci/read-2026-11-24T1826Z/commands.txt)). The step "Test compiler
    scaling in isolation" was then skipped, so that job has no CompilerScaling line. **The failed jobs were re-run once;
    the second attempt concluded `success`**: windows-latest / Release 138692855522 and CI result 138699028280
    `success`, every other job carried from the first attempt as `success` under a new id (Changed paths 138692954372,
    ubuntu-24.04 / Release 138692916475, engine vectors (ubuntu-24.04) 138692858046, (windows-latest) 138692859355,
    (Windows against Linux) 138692860924; [`ci/jobs-45455636089-attempt-2.json`](ci/jobs-45455636089-attempt-2.json)).
    The run's record now reads attempt 2, `completed`, `success` ([`ci/run-45455636089.json`](ci/run-45455636089.json)).
  - **The jobs that carry the test projects of `Broiler.JS.slnx`**: the six `Broiler.ECMAScript.*` projects in Shared
    libraries' two Build and SL rules jobs, each "Passed!" with no test failed on both systems; the fifteen
    `Broiler.JavaScript.*` projects in ci.yml's ubuntu-24.04 / Release (first attempt, carried) and windows-latest /
    Release (both attempts), each "Passed!" with no test failed in each, and CompilerScaling's isolated line on Linux
    and in the second attempt on Windows; and the engine corpus (`Category=EngineCorpus` of
    `Broiler.JavaScript.Vectors.Tests`) in the two engine vectors jobs, each passed
    ([`ci/test-projects.tsv`](ci/test-projects.tsv)). The vectors jobs raised the warnings the workflow raises without
    failing (the vectors' timing, and the Windows report against the Linux one); the logs hold them.
  - **The artifacts**, every one both runs list, downloaded and checked against the API's digest and size, every one
    matching ([`ci/artifact-digests.tsv`](ci/artifact-digests.tsv)): CI's `engine-vectors-ubuntu-24.04` 14950207772,
    `engine-vectors-windows-latest` 14951602002 and, from the second attempt, `nuget-packages` 14954743224; Shared
    libraries' `shared-packages` 14950257091, `native-aot-probe-linux-x64` 14949264676 and `native-aot-probe-win-x64`
    14949718488. The zips are not kept here, as `bridge/w1-ci/` kept none.
- **The default branch** stayed at `be6c4a5c` from before the first run until after both CI runs had concluded
  ([`logs/main-checks.log`](logs/main-checks.log)), so nothing merged while ci.yml's push run was going.

## What was set aside, and the method's notes

- **The first attempt at `8b1fa311`** stopped at 18:14:21Z by its author's single `kill` of the run's own script, at the
  disk gate before the dump run: the gate needs 8 GiB free before each run and found less, the shortfall being the run's
  own build tree, with the suite then extracted on disk beside it (`logs/notes.log`;
  [`8b1fa311-attempt1-disk-gate/logs/disk.log`](8b1fa311-attempt1-disk-gate/logs/disk.log)). Its default-mode report is
  kept as it was written and is not compared.
- **The suite on `/dev/shm`.** From the second attempt on, `pb.sh` checks the archive's SHA-256 and extracts the suite
  afresh before each run to `/dev/shm`, as bundle `sjl-14-001`'s `commit.sh` kept its suite, rather than on disk beside
  the build tree (the script's header). The suite is the same archive by its SHA-256 in every run.
- `identity.py` is the stage-1 rehearsal's script byte-unchanged, and its docstring's words "part B rehearsal (not a
  run of the rule)" are the rehearsal's label, left as written so that the script is the one that produced
  `identity/` (`logs/notes.log`).
- [`ci/first-parent-8b1fa311/`](ci/first-parent-8b1fa311/run-45393390605.json) holds, for the bundle README's note on
  ci.yml's Test step and not for part B, ci.yml's push run at the first parent, 45393390605, whose windows-latest /
  Release job failed when `Broiler.ECMAScript.Frontend.Tests`' test process "did not respond within 60 seconds".

[`../hashes.txt`](../hashes.txt) gives the SHA-256 of every file here.
