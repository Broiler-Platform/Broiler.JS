# Part B's runs at the fourth model commit, `56272150`

**Taken:** 2026-11-25, after the merge of SJL-15.2's stage 4 (Broiler-Platform/Broiler.JS#1139), for
[`../decision-rule.md`](../decision-rule.md)'s part B. These are the rule's runs at its fourth model commit, taken as
part B's runs at the first, second and third model commits were taken, in
[`../part-b-be6c4a5c/`](../part-b-be6c4a5c/README.md), [`../part-b-6c61879b/`](../part-b-6c61879b/README.md) and
[`../part-b-d87044c9/`](../part-b-d87044c9/README.md), and laid out as those folders are. Their reading is in the
bundle's [`README.md`](../README.md), the note of 2026-11-25 on part B's runs at `56272150`. Nothing here changes the
rule, a tool or a product file.

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
| [`56272150/`](56272150/logs/commit.txt) | `56272150fe23725a0e70978fb18e3c25f2548548` | the fourth model commit after W2: the merge of Broiler-Platform/Broiler.JS#1139 (head `2c5028432026feefeb8a842cf99830b6afc45c59`), SJL-15.2's stage 4 |
| [`9ae6aee0/`](9ae6aee0/logs/commit.txt) | `9ae6aee09012d6b7139856f253b43f7701cf7b8b` | its first parent, the merge of Broiler-Platform/Broiler.JS#1138 (stage 3's part B records), no model commit |

`git rev-parse 56272150^1` is `9ae6aee09012d6b7139856f253b43f7701cf7b8b`. `9ae6aee0` is no model commit: nothing from
the third model commit `d87044c9` to it changes a path under `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*`
(`git diff --stat d87044c9 9ae6aee0 -- 'Broiler.ECMAScript/Broiler.ECMAScript.Frontend*'` is empty; `logs/notes.log`).
Bundle `sjl-12-001`'s tool, `docs/evidence/sjl-12-001/tools/parse-corpus`, is the same tree at both commits,
`5ede61145de01136368c49354acc1de1c469fcb6`, the tree it had at the first, second and third model commits and their
first parents, so it ran byte-unchanged; the manifest `conformance/parse/corpus.manifest` is the same blob at both,
`2419aa2aabda30a9d3ca9aa703d38c6455859441` (SHA-256
`1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc`), and so is
`scripts/compliance/compare_parse_corpus.py`, `c0685c65899e74e5e2b0a11a270ef3ee65f3c669`
([`identity/inputs.txt`](identity/inputs.txt)).

## What was run

- **The local runs**, per commit, by [`scripts/pb.sh`](scripts/pb.sh), called once per commit, one at a time, the first
  parent first, through the machine's run script, which holds the build lock behind its idle gate (the same `run.sh`,
  `gate.sh`, `machine.sh` and `timed.py` as bundle `sjl-14-001`'s
  [`bridge/scripts/`](../../sjl-14-001/bridge/scripts/run.sh), byte for byte, as at the first, second and third model
  commits): a clean `git archive` of the commit on disk, outside every working tree; the tool built from it in Release
  with no build server, outside the tree; and run with `env -i`, so with `TZ=UTC` and no `LANG`, `LC_*` or `BROILER_*`
  variable, on the tool's own thread of 256 MB:
  - `parse-corpus-c.tsv.gz`, the default mode: entry points P and K for every input and variant of the manifest;
  - `parse-corpus-d1.tsv.gz`, `--dump --order manifest`: P's tree and the for-loop pass's tree, hashed by the canonical
    dump.
  The suite the manifest's test262 rows read is Broiler.VM's archive of test262 at the manifest's pin, `ccaac100`,
  SHA-256 `f58ce79141529c9fa33592e22ff3ff0d83b83830ac8e372ecd32e1623db1c040`, checked against the manifest's
  `pin-test262-archive` line before each run and extracted afresh for it to `/dev/shm` (`logs/suite.log`). The tool
  built with no warning and no error at both commits (`logs/build-parse-corpus.stdout.txt`). Both runs exited 0, and
  neither was stopped, re-run or set aside.
- **The comparison**, in [`identity/`](identity/commands.txt), with the commands as run (the scratch command log, with
  a note beginning "# Notes, 2026-11-25" added at its end when it was retained; the commands above it are unchanged):
  - `scripts/compliance/compare_parse_corpus.py acceptance`, base `9ae6aee0`, after `56272150`, the manifest at
    `56272150`, no classification file: [`identity/acceptance.txt`](identity/acceptance.txt) and `acceptance.json`. The
    script refuses runs that do not report every manifest input and variant at P and K, so it checks the default
    mode's completeness.
  - [`scripts/identity.py`](scripts/identity.py) over both modes' reports, every row line byte for byte and each
    report's end line: [`identity/rows-c.txt`](identity/rows-c.txt) (P's and K's outcome, exception type and message)
    and [`identity/rows-d1.txt`](identity/rows-d1.txt) (per input and variant, P's outcome and both dumps' hashes).
  - [`scripts/completeness.py`](scripts/completeness.py): each of the four reports holds exactly the manifest's inputs
    and variants, at P and K or once for the dumps, with its end line, and its outcomes counted, so that a run that read
    no input would show ([`identity/completeness.txt`](identity/completeness.txt)).
  - [`identity/model-private.txt`](identity/model-private.txt): why the lines stage 4 adds to the model section of the
    library's canonical dump, and the private names the declarations walk now reads from a request, cannot reach this
    tool's `--dump` output, read by read-only git with each command's output.
- **The CI runs at `56272150`**, in [`ci/`](ci/commands.txt): the runs GitHub Actions made on the push of `56272150` to
  the default branch, read by read-only API calls once both had concluded and kept as the API returned them, as
  [`../part-b-d87044c9/ci/`](../part-b-d87044c9/ci/commands.txt) kept the third model commit's: each run's record, its
  jobs with their steps, its artifacts with their digests, and its job logs, with
  [`ci/test-projects.tsv`](ci/test-projects.tsv), every test project of `Broiler.JS.slnx` at `56272150` with the result
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
during the stage up to the retention (`main-checks.log`, with its own note on the times of the merge, the push runs and
the checks), the gate's log, the outer commands and runs, and the stage's notes (`notes.log`).

## What the runs say

- **Both runs are complete.** At each commit the default mode reports every input and variant of the manifest at P and
  K and the dump mode every input and variant once, each report ending with its end line; the outcomes are counted the
  same at both commits ([`identity/completeness.txt`](identity/completeness.txt)).
- **They are identical in every field.** `compare_parse_corpus.py acceptance` finds no difference, with nothing to
  admit; `identity.py` finds every row line of both modes the same bytes in the same order ([`identity/`](identity/)).
- **The dumps are unchanged by stage 4's private-name lines.** Stage 4 adds `model-private-environment`,
  `model-private-name` and `model-private` lines to the model section of the library's canonical dump
  (`Broiler.ECMAScript/runners/SyntaxDump.cs`), as stage 3 added `model-target` lines
  ([`../part-b-d87044c9/identity/model-target.txt`](../part-b-d87044c9/identity/model-target.txt)); that section is
  written only when a parse builds the model, that is when the request sets `ParseRequest.BuildSemanticModel`, `false`
  by default. Stage 4 also has the declarations walk read the private names a request says are visible, which
  Broiler.JS's bridge passes; the walk reads them only with that member on. This tool writes its own canonical dump of
  Broiler.JS's tree and reaches the Frontend only through Broiler.JS's parser; nothing under `Broiler.JS/` or in bundle
  `sjl-12-001` changes between the two commits, and nothing there sets `BuildSemanticModel` or uses the library's
  `SyntaxDump` ([`identity/model-private.txt`](identity/model-private.txt)). So the `--dump` output stays identical, as
  every row of [`identity/rows-d1.txt`](identity/rows-d1.txt) shows.
- **The headers differ only in each engine assembly's module version id**, five lines per report, every other header
  line the same. The module version id is a build's name, not an answer: two builds of one commit, `9ae6aee0`, SJL-15.2
  stage 4's part B rehearsal's in another build tree (a rehearsal, not a run of the rule, its reports in scratch) and
  part B's here, differ in the same five lines in the same way, with every row line identical
  ([`identity/two-builds-9ae6aee0/`](identity/two-builds-9ae6aee0/commands.txt)), as two builds of `77fe27aa` did at the
  third model commit ([`../part-b-d87044c9/identity/two-builds-77fe27aa/`](../part-b-d87044c9/identity/two-builds-77fe27aa/commands.txt)),
  two builds of `2259fd4a` at the second and two builds of `8b1fa311` at the first.
- **The CI runs.** Both workflows that run the test projects of `Broiler.JS.slnx` made one push run at `56272150`
  ([`ci/runs-at-56272150.json`](ci/runs-at-56272150.json)), and each concluded `success` in its first attempt, every
  job `success`:
  - **Shared libraries** (`.github/workflows/shared-libraries.yml`), run 45707437312, attempt 1, head
    `56272150fe23725a0e70978fb18e3c25f2548548`: Build and SL rules (ubuntu-24.04) 139532890933 and (windows-latest)
    139532891092, which test `Broiler.ECMAScript.slnf`, so its six `Broiler.ECMAScript.*` test projects; Native AOT
    probe (linux-x64) 139532890623 and (win-x64) 139532890971; Assurance check (SL8) 139532890898; Pack, validate and
    restore the shared stream (dry run) 139532890705.
  - **CI** (`.github/workflows/ci.yml`), run 45707436823, attempt 1, head `56272150fe23725a0e70978fb18e3c25f2548548`:
    Changed paths 139532889443; ubuntu-24.04 / Release 139533186041 and windows-latest / Release 139533185961, whose
    Test step tests every project of `Broiler.JS.slnx` but those of `Broiler.ECMAScript.slnf`
    (`eng/run-tests.sh --engine-only`), so the fifteen `Broiler.JavaScript.*` test projects, and whose step "Test
    compiler scaling in isolation" ran on both systems; engine vectors (ubuntu-24.04) 139533186119, (windows-latest)
    139533186129 and (Windows against Linux) 139542820994; CI result 139542820985. No job was re-run.
  - **The jobs that carry the test projects of `Broiler.JS.slnx`**: the six `Broiler.ECMAScript.*` projects of
    `Broiler.ECMAScript.slnf` in Shared libraries' two Build and SL rules jobs, each "Passed!" with no test failed on
    both systems; the fifteen `Broiler.JavaScript.*` projects in ci.yml's ubuntu-24.04 / Release and windows-latest /
    Release, each "Passed!" with no test failed in each, and CompilerScaling's isolated line on both systems; and the
    engine corpus (`Category=EngineCorpus` of `Broiler.JavaScript.Vectors.Tests`) in the two engine vectors jobs, each
    passed ([`ci/test-projects.tsv`](ci/test-projects.tsv)). No `Broiler.ECMAScript` test project is in ci.yml's jobs.
    Two vectors jobs raised the warnings the workflow raises without failing: engine vectors (ubuntu-24.04) the
    vectors' timing, and engine vectors (Windows against Linux) the Windows report against the Linux one; engine
    vectors (windows-latest) raised none. The logs hold them.
  - **The artifacts**, every one both runs list, downloaded and checked against the API's digest and size, every one
    matching ([`ci/artifact-digests.tsv`](ci/artifact-digests.tsv)): CI's `engine-vectors-ubuntu-24.04` 15065473699,
    `engine-vectors-windows-latest` 15066188508 and `nuget-packages` 15065718396; Shared libraries' `shared-packages`
    15065194534, `native-aot-probe-linux-x64` 15065623246 and `native-aot-probe-win-x64` 15065253573. The zips are not
    kept here, as the first, second and third model commits' `ci/` kept none.
- **The default branch** stayed at `56272150` from the merge until after both CI runs had concluded
  ([`logs/main-checks.log`](logs/main-checks.log)): the merge was made at 21:46:42Z; Shared libraries' run concluded at
  21:53:38Z and ci.yml's at 22:06:37Z (each record's `updated_at`); the checks at 21:56:59Z, 21:57:53Z and 22:03:54Z,
  while ci.yml's run was still in progress, and the check at 22:09:53Z, after both had concluded, each find the default
  branch at `56272150`, and a ref that is not force-pushed cannot come back to a commit it has left; every later check
  up to the retention finds it there still.

## The method's notes

- [`scripts/`](scripts/pb.sh) holds the scripts as run: copies of the third model commit's
  [`../part-b-d87044c9/scripts/`](../part-b-d87044c9/scripts/pb.sh), the same method as the first and second model
  commits'. `pb.sh`, `identity.py` and `completeness.py` differ from those only in the worktree, the build tree and the
  suite folder each names and in their header comments; `test_projects.py` also names this commit's runs, each with one
  attempt, as its log sources, and the header lines it prints are rewritten for this commit; its matching of result
  lines is unchanged (`logs/notes.log`).
- `identity/model-private.txt` answers the question this commit raises (stage 4's private-name lines in the canonical
  dump, and the visible private names the walk now reads), as `../part-b-d87044c9/identity/model-target.txt` answered
  stage 3's; it is no part of the comparison's method, which is unchanged.
- Nothing was set aside at this commit: no run was stopped or re-taken, and no CI job was re-run.

[`../hashes.txt`](../hashes.txt) gives the SHA-256 of every file here.
