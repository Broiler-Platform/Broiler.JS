# Part B's runs at the sixth model commit, `84d84cc3`

**Taken:** 2026-11-26, after the merge of SJL-15.2's stage 5b (Broiler-Platform/Broiler.JS#1143), for
[`../decision-rule.md`](../decision-rule.md)'s part B. These are the rule's runs at its sixth model commit, taken as
part B's runs at the first five model commits were taken, in [`../part-b-be6c4a5c/`](../part-b-be6c4a5c/README.md),
[`../part-b-6c61879b/`](../part-b-6c61879b/README.md), [`../part-b-d87044c9/`](../part-b-d87044c9/README.md),
[`../part-b-56272150/`](../part-b-56272150/README.md) and [`../part-b-4d8aa93a/`](../part-b-4d8aa93a/README.md), and
laid out as those folders are. Their reading is in the bundle's [`README.md`](../README.md), the note of 2026-11-26 on
part B's runs at `84d84cc3`. Nothing here changes the rule, a tool or a product file.

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
| [`84d84cc3/`](84d84cc3/logs/commit.txt) | `84d84cc3f68e3120299eb9faa44c6b9f0f2e7b8b` | the sixth model commit after W2: the merge of Broiler-Platform/Broiler.JS#1143 (head `3332375435e554124c16307ff3f6ff32c2de3ca1`), SJL-15.2's stage 5b |
| [`d3e64b4d/`](d3e64b4d/logs/commit.txt) | `d3e64b4df2c35acf706fca91707c3f198b53e6cd` | its first parent, the merge of Broiler-Platform/Broiler.JS#1142 (stage 5a's part B records), no model commit |

`git rev-parse 84d84cc3^1` is `d3e64b4df2c35acf706fca91707c3f198b53e6cd`. `d3e64b4d` is no model commit: nothing from
the fifth model commit `4d8aa93a` to it changes a path under `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*`
(`git diff --stat 4d8aa93a d3e64b4d -- 'Broiler.ECMAScript/Broiler.ECMAScript.Frontend*'` is empty; `logs/notes.log`).
Bundle `sjl-12-001`'s tool, `docs/evidence/sjl-12-001/tools/parse-corpus`, is the same tree at both commits,
`5ede61145de01136368c49354acc1de1c469fcb6`, the tree it had at the first five model commits and their first parents, so
it ran byte-unchanged; the manifest `conformance/parse/corpus.manifest` is the same blob at both,
`2419aa2aabda30a9d3ca9aa703d38c6455859441` (SHA-256
`1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc`), and so is
`scripts/compliance/compare_parse_corpus.py`, `c0685c65899e74e5e2b0a11a270ef3ee65f3c669`
([`identity/inputs.txt`](identity/inputs.txt)).

## What was run

- **The local runs**, per commit, by [`scripts/pb.sh`](scripts/pb.sh), called once per commit, one at a time, the first
  parent first, through the machine's run script, which holds the build lock behind its idle gate (the same `run.sh`,
  `gate.sh`, `machine.sh` and `timed.py` as bundle `sjl-14-001`'s
  [`bridge/scripts/`](../../sjl-14-001/bridge/scripts/run.sh), byte for byte, as at the first five model commits): a
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
  a note beginning "# Notes, 2026-11-26" added at its end when it was retained; the commands above it are unchanged):
  - `scripts/compliance/compare_parse_corpus.py acceptance`, base `d3e64b4d`, after `84d84cc3`, the manifest at
    `84d84cc3`, no classification file: [`identity/acceptance.txt`](identity/acceptance.txt) and `acceptance.json`. The
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
  - [`identity/model-annexb.txt`](identity/model-annexb.txt): why the `model-annexb` lines and the `alias:` form stage
    5b adds to the model section of the library's canonical dump, and the Annex B aliases its declarations walk now
    computes, cannot reach this tool's `--dump` output, read by read-only git with each command's output.
- **The CI runs at `84d84cc3`**, in [`ci/`](ci/commands.txt): the runs GitHub Actions made on the push of `84d84cc3` to
  the default branch, read by read-only API calls once both had concluded and kept as the API returned them, as
  [`../part-b-4d8aa93a/ci/`](../part-b-4d8aa93a/ci/commands.txt) kept the fifth model commit's: each run's record, its
  jobs with their steps, its artifacts with their digests, and its job logs, with
  [`ci/test-projects.tsv`](ci/test-projects.tsv), every test project of `Broiler.JS.slnx` at `84d84cc3` with the result
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
the checks, and the stage's raw log of those checks it was rewritten from, `main-checks.raw.log`), the gate's log, the
outer commands and runs, the status read of the push runs as the API answered it (`status-*.tsv`), and the stage's notes
(`notes.log`).

## What the runs say

- **Both runs are complete.** At each commit the default mode reports every input and variant of the manifest at P and
  K and the dump mode every input and variant once, each report ending with its end line; the outcomes are counted the
  same at both commits ([`identity/completeness.txt`](identity/completeness.txt); for P and K,
  [`identity/acceptance.txt`](identity/acceptance.txt)).
- **They are identical in every field.** `compare_parse_corpus.py acceptance` finds no difference, with nothing to
  admit; `identity.py` finds every row line of both modes the same bytes in the same order ([`identity/`](identity/)).
- **The dumps are unchanged by stage 5b's Annex B lines.** Stage 5b adds `model-annexb` lines to the model section of
  the library's canonical dump (`Broiler.ECMAScript/runners/SyntaxDump.cs`), one per block function with its alias
  answer, and names an alias declaration there by `alias:` and its node, as stage 5a added its `model-environment` and
  `model-resolution` lines
  ([`../part-b-4d8aa93a/identity/model-resolution.txt`](../part-b-4d8aa93a/identity/model-resolution.txt)), stage 4 its
  private-name lines ([`../part-b-56272150/identity/model-private.txt`](../part-b-56272150/identity/model-private.txt))
  and stage 3 its `model-target` lines
  ([`../part-b-d87044c9/identity/model-target.txt`](../part-b-d87044c9/identity/model-target.txt)); that section is
  written only when a parse builds the model, that is when the request sets `ParseRequest.BuildSemanticModel`, `false` by
  default. Stage 5b also has the declarations walk compute the Annex B aliases inside the resolutions it finishes after
  the walk, only when the walk builds the model, and adds `SemanticModel.AnnexBAlias`, `BindingKind.BlockFunctionAlias`
  and `DynamicReason.BlockFunction`. This tool writes its own canonical dump of Broiler.JS's tree and reaches the
  Frontend only through Broiler.JS's parser; nothing under `Broiler.JS/` or in bundle `sjl-12-001` changes between the
  two commits, and nothing there sets `BuildSemanticModel`, uses the library's `SyntaxDump` or reads a member stage 5b
  adds ([`identity/model-annexb.txt`](identity/model-annexb.txt)). So the `--dump` output stays identical, as every row
  of [`identity/rows-d1.txt`](identity/rows-d1.txt) shows.
- **The headers differ only in each engine assembly's module version id**, five lines per report, every other header
  line the same. The module version id is a build's name, not an answer: two builds of one commit, `d3e64b4d`, SJL-15.2
  stage 5b's rehearsal's in another build tree (a rehearsal, not a run of the rule, its reports in scratch) and part B's
  here, differ in the same five lines in the same way, with every row line identical
  ([`identity/two-builds-d3e64b4d/`](identity/two-builds-d3e64b4d/commands.txt)), as two builds of `746591d2` did at the
  fifth model commit
  ([`../part-b-4d8aa93a/identity/two-builds-746591d2/`](../part-b-4d8aa93a/identity/two-builds-746591d2/commands.txt)),
  two builds of `9ae6aee0` at the fourth, two builds of `77fe27aa` at the third, two builds of `2259fd4a` at the second
  and two builds of `8b1fa311` at the first.
- **The CI runs.** Both workflows that run the test projects of `Broiler.JS.slnx` made one push run at `84d84cc3`
  ([`ci/runs-at-84d84cc3.json`](ci/runs-at-84d84cc3.json)), and each concluded `success` in its first attempt, every
  job `success`:
  - **Shared libraries** (`.github/workflows/shared-libraries.yml`), run 45843919956, attempt 1, head
    `84d84cc3f68e3120299eb9faa44c6b9f0f2e7b8b`: Build and SL rules (ubuntu-24.04) 139992499657 and (windows-latest)
    139992499827, which test `Broiler.ECMAScript.slnf`, so its six `Broiler.ECMAScript.*` test projects; Native AOT
    probe (linux-x64) 139992499655 and (win-x64) 139992499642; Assurance check (SL8) 139992499403; Pack, validate and
    restore the shared stream (dry run) 139992499693.
  - **CI** (`.github/workflows/ci.yml`), run 45843920047, attempt 1, head `84d84cc3f68e3120299eb9faa44c6b9f0f2e7b8b`:
    Changed paths 139992499345; ubuntu-24.04 / Release 139992846327 and windows-latest / Release 139992846442, whose
    Test step tests every project of `Broiler.JS.slnx` but those of `Broiler.ECMAScript.slnf`
    (`eng/run-tests.sh --engine-only`), so the fifteen `Broiler.JavaScript.*` test projects, and whose step "Test
    compiler scaling in isolation" ran on both systems; engine vectors (ubuntu-24.04) 139992846402, (windows-latest)
    139992846480 and (Windows against Linux) 140001157269; CI result 140001157482. No job was re-run.
  - **The jobs that carry the test projects of `Broiler.JS.slnx`**: the six `Broiler.ECMAScript.*` projects of
    `Broiler.ECMAScript.slnf` in Shared libraries' two Build and SL rules jobs, each "Passed!" with no test failed on
    both systems; the fifteen `Broiler.JavaScript.*` projects in ci.yml's ubuntu-24.04 / Release and windows-latest /
    Release, each "Passed!" with no test failed in each, and CompilerScaling's isolated line on both systems; and the
    engine corpus (`Category=EngineCorpus` of `Broiler.JavaScript.Vectors.Tests`) in the two engine vectors jobs, each
    passed ([`ci/test-projects.tsv`](ci/test-projects.tsv)). No `Broiler.ECMAScript` test project is in ci.yml's jobs.
    Two vectors jobs raised the warnings the workflow raises without failing: engine vectors (ubuntu-24.04) the
    vectors' timing, which also says that the slowest vector, `semantics/super/ee0131/0003`, took 5.000 s of the 5 s
    limit and was the one vector run again alone after finding no completion in the parallel pass (the fifth model
    commit's run 45796359262 warned the same of the same vector), and engine vectors (Windows against Linux) the
    Windows report against the Linux one; engine vectors (windows-latest) raised none. The logs hold them.
  - **The artifacts**, every one both runs list, downloaded and checked against the API's digest and size, every one
    matching ([`ci/artifact-digests.tsv`](ci/artifact-digests.tsv)): CI's `engine-vectors-ubuntu-24.04` 15130383411,
    `engine-vectors-windows-latest` 15131612385 and `nuget-packages` 15131541452; Shared libraries' `shared-packages`
    15131820108, `native-aot-probe-linux-x64` 15131675196 and `native-aot-probe-win-x64` 15131795301. The zips are not
    kept here, as the first five model commits' `ci/` kept none.
- **The default branch** stayed at `84d84cc3` from the merge until after both CI runs had concluded
  ([`logs/main-checks.log`](logs/main-checks.log)): the merge was made at 14:55:42Z; Shared libraries' run concluded at
  15:03:02Z and ci.yml's at 15:10:42Z (each record's `updated_at`); the stage's first check, at 15:55:08Z, after both had
  concluded, finds the default branch at `84d84cc3`, and a ref that is not force-pushed cannot come back to a commit it
  has left; every later check up to the retention finds it there still, among them the check at 15:58:44Z logged before
  the CI import (the CI read at 15:58:44Z-15:58:51Z and the artifacts' downloads at 15:58:59Z-15:59:05Z,
  [`ci/commands.txt`](ci/commands.txt)). That both runs had concluded rests on each run record's `updated_at`, which the
  imported [`ci/run-45843920047.json`](ci/run-45843920047.json) and
  [`ci/run-45843919956.json`](ci/run-45843919956.json) hold, and on the status read at 16:03:48Z, whose answer is kept
  ([`logs/status-20261126T160348Z.tsv`](logs/status-20261126T160348Z.tsv)).

## The method's notes

- [`scripts/`](scripts/pb.sh) holds the scripts as run: copies of the fifth model commit's
  [`../part-b-4d8aa93a/scripts/`](../part-b-4d8aa93a/scripts/pb.sh), the same method as the first four model commits'.
  `pb.sh`, `identity.py` and `completeness.py` differ from those only in the worktree, the build tree and the suite
  folder each names and in their header comments; `test_projects.py` also names this commit's runs, each with one
  attempt, as its log sources, and the header lines it prints are rewritten for this commit; its matching of result
  lines is unchanged (`logs/notes.log`).
- `identity/model-annexb.txt` answers the question this commit raises (stage 5b's Annex B lines in the canonical dump,
  and the aliases the walk now computes), as `../part-b-4d8aa93a/identity/model-resolution.txt` answered stage 5a's; it
  is no part of the comparison's method, which is unchanged. It was written by a scratch script that runs each command
  it shows and writes its output and exit; the script is not kept.
- Kept as at the fifth model commit: in `logs/main-checks.log`, one labelled line per check, its first two lines
  rewritten from the stage's raw log with the same times and values (its note says which lines), and that raw log
  itself, as it was written (`logs/main-checks.raw.log`). The fifth model commit's folder kept two status reads; this one
  keeps one, taken through the API during the after run: the first read of both runs, at 15:55Z before the stage's
  worktree, was by a GitHub tool whose answers are not kept, and found both already concluded `success` in their first
  attempts (`logs/notes.log`).
- Nothing was set aside at this commit: no run was stopped or re-taken, and no CI job was re-run.

[`../hashes.txt`](../hashes.txt) gives the SHA-256 of every file here.
