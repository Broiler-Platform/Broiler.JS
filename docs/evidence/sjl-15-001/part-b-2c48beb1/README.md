# Part B's runs at the eighth model commit, `2c48beb1`

**Taken:** 2026-11-27, after the merge of SJL-15.2's stage 6 (Broiler-Platform/Broiler.JS#1147), for
[`../decision-rule.md`](../decision-rule.md)'s part B. These are the rule's runs at its eighth model commit, taken as
part B's runs at the first seven model commits were taken, in [`../part-b-be6c4a5c/`](../part-b-be6c4a5c/README.md),
[`../part-b-6c61879b/`](../part-b-6c61879b/README.md), [`../part-b-d87044c9/`](../part-b-d87044c9/README.md),
[`../part-b-56272150/`](../part-b-56272150/README.md), [`../part-b-4d8aa93a/`](../part-b-4d8aa93a/README.md),
[`../part-b-84d84cc3/`](../part-b-84d84cc3/README.md) and [`../part-b-30af0dad/`](../part-b-30af0dad/README.md), and
laid out as those folders are. Their reading is in the bundle's [`README.md`](../README.md), the note of 2026-11-27 on
part B's runs at `2c48beb1`. Nothing here changes the rule, a tool or a product file.

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
| [`2c48beb1/`](2c48beb1/logs/commit.txt) | `2c48beb1e332dd7c9fa1577705ed5458b5686c1f` | the eighth model commit after W2: the merge of Broiler-Platform/Broiler.JS#1147 (head `d00083319297a8f7e559da29f159c834c84f0dd9`), SJL-15.2's stage 6 |
| [`59ccb11c/`](59ccb11c/logs/commit.txt) | `59ccb11cc1279fe2a6f015acbccd7f6dbff185f2` | its first parent, the merge of Broiler-Platform/Broiler.JS#1146 (stage 5c's part B records), no model commit |

`git rev-parse 2c48beb1^1` is `59ccb11cc1279fe2a6f015acbccd7f6dbff185f2`. `59ccb11c` is no model commit: nothing from
the seventh model commit `30af0dad` to it changes a path under `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*`
(`git diff --stat 30af0dad 59ccb11c -- 'Broiler.ECMAScript/Broiler.ECMAScript.Frontend*'` is empty; `logs/notes.log`).
Bundle `sjl-12-001`'s tool, `docs/evidence/sjl-12-001/tools/parse-corpus`, is the same tree at both commits,
`5ede61145de01136368c49354acc1de1c469fcb6`, the tree it had at the first seven model commits and their first parents,
so it ran byte-unchanged; the manifest `conformance/parse/corpus.manifest` is the same blob at both,
`2419aa2aabda30a9d3ca9aa703d38c6455859441` (SHA-256
`1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc`), and so is
`scripts/compliance/compare_parse_corpus.py`, `c0685c65899e74e5e2b0a11a270ef3ee65f3c669`
([`identity/inputs.txt`](identity/inputs.txt)).

## What was run

- **The local runs**, per commit, by [`scripts/pb.sh`](scripts/pb.sh), called once per commit, one at a time, the first
  parent first, through the machine's run script, which holds the build lock behind its idle gate (the same `run.sh`,
  `gate.sh`, `machine.sh` and `timed.py` as bundle `sjl-14-001`'s
  [`bridge/scripts/`](../../sjl-14-001/bridge/scripts/run.sh), byte for byte, as at the first seven model commits): a
  clean `git archive` of the commit on disk, outside every working tree; the tool built from it in Release with no build
  server, outside the tree; and run with `env -i`, so with `TZ=UTC` and no `LANG`, `LC_*` or `BROILER_*` variable, on the
  tool's own thread of 256 MB:
  - `parse-corpus-c.tsv.gz`, the default mode: entry points P and K for every input and variant of the manifest;
  - `parse-corpus-d1.tsv.gz`, `--dump --order manifest`: P's tree and the for-loop pass's tree, hashed by the canonical
    dump.
  The suite the manifest's test262 rows read is Broiler.VM's archive of test262 at the manifest's pin, `ccaac100`,
  SHA-256 `f58ce79141529c9fa33592e22ff3ff0d83b83830ac8e372ecd32e1623db1c040`, checked against the manifest's
  `pin-test262-archive` line before each run and extracted afresh for it to `/dev/shm` (`logs/suite.log`). The tool
  built with no warning and no error at both commits (`logs/build-parse-corpus.stdout.txt`). Both runs exited 0, and
  neither was stopped, re-run or set aside.
- **The comparison**, in [`identity/`](identity/commands.txt), with the commands as run (the scratch command log, with
  a note beginning "# Notes, 2026-11-27" added at its end when it was retained; the commands above it are unchanged):
  - `scripts/compliance/compare_parse_corpus.py acceptance`, base `59ccb11c`, after `2c48beb1`, the manifest at
    `2c48beb1`, no classification file: [`identity/acceptance.txt`](identity/acceptance.txt) and `acceptance.json`. The
    script refuses runs that do not report every manifest input and variant at P and K, so it checks the default
    mode's completeness.
  - [`scripts/identity.py`](scripts/identity.py) over both modes' reports, every row line byte for byte and each
    report's end line: [`identity/rows-c.txt`](identity/rows-c.txt) (P's and K's outcome, exception type and message)
    and [`identity/rows-d1.txt`](identity/rows-d1.txt) (per input and variant, P's outcome and both dumps' hashes).
  - [`scripts/completeness.py`](scripts/completeness.py): each of the four reports holds exactly the manifest's inputs
    and variants, twice each in the default mode or once for the dumps, with its end line, and its outcomes counted, so
    that a run that read no input would show ([`identity/completeness.txt`](identity/completeness.txt)). It counts each
    input and variant's rows but does not read which entry points they name; that the two are P and K is
    `compare_parse_corpus.py`'s check above, which refuses a row that appears twice and runs missing any input and
    variant at P or at K, and compared both runs' 213,926 rows with rc 0.
  - [`identity/model-module.txt`](identity/model-module.txt): why the module-record lines stage 6 adds to the model
    section of the library's canonical dump, and the rows EE0170 and EE0171 its declarations walk now reports, cannot
    reach this tool's `--dump` output or its P and K answers, read by read-only git with each command's output.
- **The CI runs at `2c48beb1`**, in [`ci/`](ci/commands.txt): the runs GitHub Actions made on the push of `2c48beb1` to
  the default branch, read by read-only API calls once both had concluded and kept as the API returned them, as
  [`../part-b-30af0dad/ci/`](../part-b-30af0dad/ci/commands.txt) kept the seventh model commit's: each run's record, its
  jobs with their steps, its artifacts with their digests, and its job logs, with
  [`ci/test-projects.tsv`](ci/test-projects.tsv), every test project of `Broiler.JS.slnx` at `2c48beb1` with the result
  line each test job wrote for it.

Each commit's `logs/` holds its record (`commit.txt`: the commit, its tree and first parent, the tool's tree, the
manifest's blob, the suite and its SHA-256, the SDK and the machine), the commands as run (`commands.log`), each step's
exit, time and peak memory (`runs.tsv`, `*.time.txt`), the build's and the runs' output, the disk checks
(`disk.log`), the built binaries' SHA-256 (`binaries.sha256.txt`), and the gate's records of the machine before and after
(`gate-*.txt`, `gate-machine-sh.txt.gz` compressed because its lines end in spaces); the folder holds the reports'
SHA-256 (`reports.sha256.txt`). Each report's header is the tool's own record of its run: the mode, the manifest's digest
and rows, the order, one thread and its stack, the environment (`TZ=UTC`, `LANG`, `LC_*` and `BROILER_*` unset), the
invariant culture, the time zone `UTC`, the runtime, the system, the processors, and each engine assembly's name,
version and module version id. [`logs/`](logs/runs.tsv) holds the stage's logs: the checks of the default branch logged
during the stage up to the retention (`main-checks.log`), the gate's log, the outer commands and runs, the two status
reads of the push runs as the API answered them (`status-*.tsv`), GitHub's activity list of the default branch read
before the retention (`activity-main-*.tsv`), and the stage's notes (`notes.log`).

## What the runs say

- **Both runs are complete.** At each commit the default mode reports every input and variant of the manifest at P and
  K and the dump mode every input and variant once, each report ending with its end line; the outcomes are counted the
  same at both commits ([`identity/completeness.txt`](identity/completeness.txt); for P and K,
  [`identity/acceptance.txt`](identity/acceptance.txt)).
- **They are identical in every field.** `compare_parse_corpus.py acceptance` finds no difference, with nothing to
  admit; `identity.py` finds every row line of both modes the same bytes in the same order ([`identity/`](identity/)).
- **The dumps and the answers are unchanged by stage 6's module records and rows.** Stage 6 adds, for a Module only, a
  `model-module` line, per request a `model-request` line and its `model-attribute` lines, and `model-import`,
  `model-export-local`, `model-export-indirect` and `model-export-star` lines to the model section of the library's
  canonical dump (`Broiler.ECMAScript/runners/SyntaxDump.cs`), as stage 5c added its `model-eval-call` and
  `model-caller-scope` lines
  ([`../part-b-30af0dad/identity/model-caller-scope.txt`](../part-b-30af0dad/identity/model-caller-scope.txt)), stage 5b
  its `model-annexb` lines and the `alias:` form
  ([`../part-b-84d84cc3/identity/model-annexb.txt`](../part-b-84d84cc3/identity/model-annexb.txt)), stage 5a its
  `model-environment` and `model-resolution` lines
  ([`../part-b-4d8aa93a/identity/model-resolution.txt`](../part-b-4d8aa93a/identity/model-resolution.txt)), stage 4
  its private-name lines ([`../part-b-56272150/identity/model-private.txt`](../part-b-56272150/identity/model-private.txt))
  and stage 3 its `model-target` lines
  ([`../part-b-d87044c9/identity/model-target.txt`](../part-b-d87044c9/identity/model-target.txt)); that section is written only when a parse builds the model, that is when the request sets
  `ParseRequest.BuildSemanticModel`, `false` by default, and the new lines only when that model holds a module record.
  Stage 6 also adds `SemanticModel.Module` and the public types `ModuleRecord`, `ModuleRequest`,
  `ModuleRequestAttribute`, `ImportEntry`, `ExportEntry` and `ImportNameKind`; the declarations walk builds the record
  only when it builds the model, on the Module goal, and reports the rows EE0170 and EE0171 only in its full walk, with
  the member on and on the Module goal. This tool writes its own canonical dump of Broiler.JS's tree and reaches the
  Frontend only through Broiler.JS's parser; nothing under `Broiler.JS/` or in bundle `sjl-12-001` changes between the
  two commits, and nothing there sets `BuildSemanticModel`, uses the library's `SyntaxDump`, or names the model, the
  module record or a type stage 6 adds: the `ModuleRequest`, `ImportEntry` and `ExportEntry` that Broiler.JS's engine
  uses are its own internal records in `Broiler.JavaScript.Modules`, outside the Frontend's `Semantics` namespace, which
  only the bridge's two files use, and neither of them names the model or a member stage 6 adds
  ([`identity/model-module.txt`](identity/model-module.txt)). So the `--dump` output stays identical, as every row of
  [`identity/rows-d1.txt`](identity/rows-d1.txt) shows, and so do P's and K's answers, as every row of
  [`identity/rows-c.txt`](identity/rows-c.txt) shows.
- **The headers differ only in each engine assembly's module version id**, five lines per report, every other header
  line the same. The module version id is a build's name, not an answer: two builds of one commit, `59ccb11c`, SJL-15.2
  stage 6's rehearsal's in another build tree (a rehearsal, not a run of the rule, its reports in scratch) and part B's
  here, differ in the same five lines in the same way, with every row line identical
  ([`identity/two-builds-59ccb11c/`](identity/two-builds-59ccb11c/commands.txt)), as two builds of `4e27f13d` did at the
  seventh model commit
  ([`../part-b-30af0dad/identity/two-builds-4e27f13d/`](../part-b-30af0dad/identity/two-builds-4e27f13d/commands.txt)),
  two builds of `d3e64b4d` at the sixth, two builds of `746591d2` at the fifth, two builds of `9ae6aee0` at the fourth,
  two builds of `77fe27aa` at the third, two builds of `2259fd4a` at the second and two builds of `8b1fa311` at the
  first.
- **The CI runs.** Both workflows that run the test projects of `Broiler.JS.slnx` made one push run at `2c48beb1`
  ([`ci/runs-at-2c48beb1.json`](ci/runs-at-2c48beb1.json)), and each concluded `success` in its first attempt, every
  job `success`:
  - **Shared libraries** (`.github/workflows/shared-libraries.yml`), run 45977304205, attempt 1, head
    `2c48beb1e332dd7c9fa1577705ed5458b5686c1f`: Build and SL rules (ubuntu-24.04) 140432148891 and (windows-latest)
    140432148521, which test `Broiler.ECMAScript.slnf`, so its six `Broiler.ECMAScript.*` test projects; Native AOT
    probe (linux-x64) 140432148982 and (win-x64) 140432148938; Assurance check (SL8) 140432148866; Pack, validate and
    restore the shared stream (dry run) 140432148909.
  - **CI** (`.github/workflows/ci.yml`), run 45977304084, attempt 1, head `2c48beb1e332dd7c9fa1577705ed5458b5686c1f`:
    Changed paths 140432147810; ubuntu-24.04 / Release 140432441428 and windows-latest / Release 140432441427, whose
    Test step tests every project of `Broiler.JS.slnx` but those of `Broiler.ECMAScript.slnf`
    (`eng/run-tests.sh --engine-only`), so the fifteen `Broiler.JavaScript.*` test projects, and whose step "Test
    compiler scaling in isolation" ran on both systems; engine vectors (ubuntu-24.04) 140432441444, (windows-latest)
    140432441461 and (Windows against Linux) 140441200971; CI result 140441200965. No job was re-run.
  - **The jobs that carry the test projects of `Broiler.JS.slnx`**: the six `Broiler.ECMAScript.*` projects of
    `Broiler.ECMAScript.slnf` in Shared libraries' two Build and SL rules jobs, each "Passed!" with no test failed on
    both systems; the fifteen `Broiler.JavaScript.*` projects in ci.yml's ubuntu-24.04 / Release and windows-latest /
    Release, each "Passed!" with no test failed in each, and CompilerScaling's isolated line on both systems; and the
    engine corpus (`Category=EngineCorpus` of `Broiler.JavaScript.Vectors.Tests`) in the two engine vectors jobs, each
    passed ([`ci/test-projects.tsv`](ci/test-projects.tsv)). No `Broiler.ECMAScript` test project is in ci.yml's jobs.
    Two vectors jobs raised the warnings the workflow raises without failing: engine vectors (ubuntu-24.04) the
    vectors' timing, which also says that the slowest vector, `semantics/super/ee0131/0003`, took 5.000 s of the 5 s
    limit and was the one vector run again alone after finding no completion in the parallel pass (the seventh model
    commit's run 45894440417 warned the same of the same vector), and engine vectors (Windows against Linux) the
    Windows report against the Linux one; engine vectors (windows-latest) raised none. The logs hold them.
  - **The artifacts**, every one both runs list, downloaded and checked against the API's digest and size, every one
    matching ([`ci/artifact-digests.tsv`](ci/artifact-digests.tsv)): CI's `engine-vectors-ubuntu-24.04` 15188846435,
    `engine-vectors-windows-latest` 15188558222 and `nuget-packages` 15187014350; Shared libraries' `shared-packages`
    15186834880, `native-aot-probe-linux-x64` 15187712541 and `native-aot-probe-win-x64` 15187996606. The zips are not
    kept here, as the first seven model commits' `ci/` kept none.
- **The default branch** stayed at `2c48beb1` from the merge until after both CI runs had concluded
  ([`logs/main-checks.log`](logs/main-checks.log)): the merge was made at 09:52:12Z; Shared libraries' run concluded at
  09:59:57Z and ci.yml's at 10:11:52Z (each record's `updated_at`); the stage's first check, at 10:09:20Z, while
  ci.yml's run was still in progress, finds the default branch at `2c48beb1`, and so does every later check, among them
  the one at 10:10:57Z, before ci.yml's run concluded, and those at 10:17:47Z (whose stale label the method's notes
  below explain), 10:18:10Z, 10:24:58Z and 10:25:46Z, after it had; a ref that is not force-pushed cannot come back to a
  commit it has left; and GitHub's activity list of the default branch, read at 10:24:58Z
  ([`logs/activity-main-20261127T102458Z.tsv`](logs/activity-main-20261127T102458Z.tsv)), has as its newest entry the
  merge from `59ccb11c` to `2c48beb1` at 09:52:12Z and nothing after it. The CI import was read after both runs had
  concluded (the CI read at 10:18:29Z-10:18:36Z and the artifacts' downloads at 10:18:47Z-10:18:54Z,
  [`ci/commands.txt`](ci/commands.txt)). That both runs had concluded rests on each run record's `updated_at`, which the
  imported [`ci/run-45977304084.json`](ci/run-45977304084.json) and
  [`ci/run-45977304205.json`](ci/run-45977304205.json) hold.

## The method's notes

- [`scripts/`](scripts/pb.sh) holds the scripts as run: copies of the seventh model commit's
  [`../part-b-30af0dad/scripts/`](../part-b-30af0dad/scripts/pb.sh), the same method as the first six model commits'.
  `pb.sh`, `identity.py` and `completeness.py` differ from those only in the worktree, the build tree and the suite
  folder each names and in their header comments; `test_projects.py` also names this commit's runs, each with one
  attempt, as its log sources, and the header lines it prints are rewritten for this commit; its matching of result
  lines is unchanged (`logs/notes.log`).
- `identity/model-module.txt` answers the question this commit raises (stage 6's module-record lines in the canonical
  dump, the record the walk now builds and its rows EE0170 and EE0171), as
  `../part-b-30af0dad/identity/model-caller-scope.txt` answered stage 5c's; it is no part of the comparison's method,
  which is unchanged. It was written by a scratch script that runs each command it shows and writes its output and
  exit; the script is not kept. It is a copy of the scratch script that wrote `model-caller-scope.txt`, with the commits,
  its header and notes, and its commands' patterns changed to stage 6's line names, members and rows (stage 5c's, 5b's
  and 5a's members kept in the search of Broiler.JS and the bundle), stage 5c's two commands on the bridge's
  `InheritedContext` and `CallerScopes`' declaration dropped, and three commands added: the files of Broiler.JS and the
  bundle that use the Frontend's `Semantics` namespace, stage 6's member names in those files, and Broiler.JS's own
  module records of the same names. Its runner and command printer are unchanged.
- Changed from the seventh model commit's folder: `logs/main-checks.log`'s first line, at 10:09:20Z, was written by hand
  before the stage's check script was copied, its label `step0-start` without the colon the script writes; every later
  line is the script's, one labelled line per check, so there is no raw log to keep beside it. Its line at 10:17:47Z is
  labelled "CI in progress at last status read" from the status read at 10:11:30Z; ci.yml's run had concluded at
  10:11:52Z, before that check, as the next read showed; the line is left as written (`logs/notes.log`). This folder
  keeps both status reads taken through the API, at 10:11:30Z, during the base run, when ci.yml's run was still in
  progress, and at 10:18:09Z, during the after run, when both had concluded; between them ci.yml's run and its jobs were
  read once through the API at about 10:18Z, those answers not kept (`logs/notes.log`). That both runs had concluded
  rests on neither, but on each run record's `updated_at` ([`ci/commands.txt`](ci/commands.txt)). The CI import was
  read while the after run's build step (10:17:55Z-10:19:07Z,
  [`2c48beb1/logs/runs.tsv`](2c48beb1/logs/runs.tsv)) held the run lock; its reads change no file the build or the
  runs read.
  *(Added 2026-11-27 after review:)* the line above replaces "The CI import was read during the after run's build
  step, which reads no network": that build step begins with a NuGet restore
  ([`2c48beb1/logs/build-parse-corpus.stdout.txt`](2c48beb1/logs/build-parse-corpus.stdout.txt)), and nothing kept
  shows it read no network. `logs/notes.log`'s line at 10:25:14Z, "the after run reads no network", is left as
  written, and this note takes its place. `logs/main-checks.log`'s line at 10:24:58Z is labelled "both CI runs
  concluded at 10:18:09Z": 10:18:09Z is the time of the status read that found them concluded
  ([`logs/status-20261127T101809Z.tsv`](logs/status-20261127T101809Z.tsv)), not when they concluded; Shared
  libraries' run concluded at 09:59:57Z and ci.yml's at 10:11:52Z (each record's `updated_at`). The line is left as
  written.
- Nothing was set aside at this commit: no run was stopped or re-taken, and no CI job was re-run.

[`../hashes.txt`](../hashes.txt) gives the SHA-256 of every file here.
