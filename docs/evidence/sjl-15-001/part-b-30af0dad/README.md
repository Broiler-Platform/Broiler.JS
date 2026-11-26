# Part B's runs at the seventh model commit, `30af0dad`

**Taken:** 2026-11-26, after the merge of SJL-15.2's stage 5c (Broiler-Platform/Broiler.JS#1145), for
[`../decision-rule.md`](../decision-rule.md)'s part B. These are the rule's runs at its seventh model commit, taken as
part B's runs at the first six model commits were taken, in [`../part-b-be6c4a5c/`](../part-b-be6c4a5c/README.md),
[`../part-b-6c61879b/`](../part-b-6c61879b/README.md), [`../part-b-d87044c9/`](../part-b-d87044c9/README.md),
[`../part-b-56272150/`](../part-b-56272150/README.md), [`../part-b-4d8aa93a/`](../part-b-4d8aa93a/README.md) and
[`../part-b-84d84cc3/`](../part-b-84d84cc3/README.md), and laid out as those folders are. Their reading is in the
bundle's [`README.md`](../README.md), the note of 2026-11-26 on part B's runs at `30af0dad`. Nothing here changes the
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
| [`30af0dad/`](30af0dad/logs/commit.txt) | `30af0dad83f73d2a5cf5495b2d4e3716cc7ed0df` | the seventh model commit after W2: the merge of Broiler-Platform/Broiler.JS#1145 (head `628f4b3887f1117b73d595cdbc9429bd5b033754`), SJL-15.2's stage 5c |
| [`4e27f13d/`](4e27f13d/logs/commit.txt) | `4e27f13d3dc1bd3c38c5f29ea28ff4391ec8771b` | its first parent, the merge of Broiler-Platform/Broiler.JS#1144 (stage 5b's part B records), no model commit |

`git rev-parse 30af0dad^1` is `4e27f13d3dc1bd3c38c5f29ea28ff4391ec8771b`. `4e27f13d` is no model commit: nothing from
the sixth model commit `84d84cc3` to it changes a path under `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*`
(`git diff --stat 84d84cc3 4e27f13d -- 'Broiler.ECMAScript/Broiler.ECMAScript.Frontend*'` is empty; `logs/notes.log`).
Bundle `sjl-12-001`'s tool, `docs/evidence/sjl-12-001/tools/parse-corpus`, is the same tree at both commits,
`5ede61145de01136368c49354acc1de1c469fcb6`, the tree it had at the first six model commits and their first parents, so
it ran byte-unchanged; the manifest `conformance/parse/corpus.manifest` is the same blob at both,
`2419aa2aabda30a9d3ca9aa703d38c6455859441` (SHA-256
`1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc`), and so is
`scripts/compliance/compare_parse_corpus.py`, `c0685c65899e74e5e2b0a11a270ef3ee65f3c669`
([`identity/inputs.txt`](identity/inputs.txt)).

## What was run

- **The local runs**, per commit, by [`scripts/pb.sh`](scripts/pb.sh), called once per commit, one at a time, the first
  parent first, through the machine's run script, which holds the build lock behind its idle gate (the same `run.sh`,
  `gate.sh`, `machine.sh` and `timed.py` as bundle `sjl-14-001`'s
  [`bridge/scripts/`](../../sjl-14-001/bridge/scripts/run.sh), byte for byte, as at the first six model commits): a
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
  - `scripts/compliance/compare_parse_corpus.py acceptance`, base `4e27f13d`, after `30af0dad`, the manifest at
    `30af0dad`, no classification file: [`identity/acceptance.txt`](identity/acceptance.txt) and `acceptance.json`. The
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
  - [`identity/model-caller-scope.txt`](identity/model-caller-scope.txt): why the `model-eval-call` and
    `model-caller-scope` lines stage 5c adds to the model section of the library's canonical dump, and the caller's
    records its declarations walk now computes, cannot reach this tool's `--dump` output, read by read-only git with
    each command's output.
- **The CI runs at `30af0dad`**, in [`ci/`](ci/commands.txt): the runs GitHub Actions made on the push of `30af0dad` to
  the default branch, read by read-only API calls once both had concluded and kept as the API returned them, as
  [`../part-b-84d84cc3/ci/`](../part-b-84d84cc3/ci/commands.txt) kept the sixth model commit's: each run's record, its
  jobs with their steps, its artifacts with their digests, and its job logs, with
  [`ci/test-projects.tsv`](ci/test-projects.tsv), every test project of `Broiler.JS.slnx` at `30af0dad` with the result
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
during the stage up to the retention (`main-checks.log`), the gate's log, the outer commands and runs, the status read of
the push runs as the API answered it (`status-*.tsv`), GitHub's activity list of the default branch read before the
retention (`activity-main-*.tsv`), and the stage's notes (`notes.log`).

## What the runs say

- **Both runs are complete.** At each commit the default mode reports every input and variant of the manifest at P and
  K and the dump mode every input and variant once, each report ending with its end line; the outcomes are counted the
  same at both commits ([`identity/completeness.txt`](identity/completeness.txt); for P and K,
  [`identity/acceptance.txt`](identity/acceptance.txt)).
- **They are identical in every field.** `compare_parse_corpus.py acceptance` finds no difference, with nothing to
  admit; `identity.py` finds every row line of both modes the same bytes in the same order ([`identity/`](identity/)).
- **The dumps are unchanged by stage 5c's caller's-records lines.** Stage 5c adds `model-eval-call` lines to the model
  section of the library's canonical dump (`Broiler.ECMAScript/runners/SyntaxDump.cs`), one per direct-eval candidate
  with the number of its caller's records, each followed by `model-caller-scope` lines, one per record, as stage 5b
  added its `model-annexb` lines and the `alias:` form
  ([`../part-b-84d84cc3/identity/model-annexb.txt`](../part-b-84d84cc3/identity/model-annexb.txt)), stage 5a its
  `model-environment` and `model-resolution` lines
  ([`../part-b-4d8aa93a/identity/model-resolution.txt`](../part-b-4d8aa93a/identity/model-resolution.txt)), stage 4 its
  private-name lines ([`../part-b-56272150/identity/model-private.txt`](../part-b-56272150/identity/model-private.txt))
  and stage 3 its `model-target` lines
  ([`../part-b-d87044c9/identity/model-target.txt`](../part-b-d87044c9/identity/model-target.txt)); that section is
  written only when a parse builds the model, that is when the request sets `ParseRequest.BuildSemanticModel`, `false` by
  default. Stage 5c also adds `InheritedContext.CallerScopes`, which nothing reads with that member off, the public type
  `CallerScope`, and `SemanticModel.DirectEvalCalls` and `SemanticModel.CallerScopesOf`; the declarations walk records
  the direct-eval candidates and computes their records only when it builds the model, and reports EA0007 and EA0008
  only with the member on and the records given. This tool writes its own canonical dump of Broiler.JS's tree and
  reaches the Frontend only through Broiler.JS's parser; nothing under `Broiler.JS/` or in bundle `sjl-12-001` changes
  between the two commits, and nothing there sets `BuildSemanticModel` or `CallerScopes` (the bridge's
  `InheritedContext`, for the eval goal, sets five other members), uses the library's `SyntaxDump` or reads a member
  stage 5c adds ([`identity/model-caller-scope.txt`](identity/model-caller-scope.txt)). So the `--dump` output stays
  identical, as every row of [`identity/rows-d1.txt`](identity/rows-d1.txt) shows.
- **The headers differ only in each engine assembly's module version id**, five lines per report, every other header
  line the same. The module version id is a build's name, not an answer: two builds of one commit, `4e27f13d`, SJL-15.2
  stage 5c's rehearsal's in another build tree (a rehearsal, not a run of the rule, its reports in scratch) and part B's
  here, differ in the same five lines in the same way, with every row line identical
  ([`identity/two-builds-4e27f13d/`](identity/two-builds-4e27f13d/commands.txt)), as two builds of `d3e64b4d` did at the
  sixth model commit
  ([`../part-b-84d84cc3/identity/two-builds-d3e64b4d/`](../part-b-84d84cc3/identity/two-builds-d3e64b4d/commands.txt)),
  two builds of `746591d2` at the fifth, two builds of `9ae6aee0` at the fourth, two builds of `77fe27aa` at the third,
  two builds of `2259fd4a` at the second and two builds of `8b1fa311` at the first.
- **The CI runs.** Both workflows that run the test projects of `Broiler.JS.slnx` made one push run at `30af0dad`
  ([`ci/runs-at-30af0dad.json`](ci/runs-at-30af0dad.json)), and each concluded `success` in its first attempt, every
  job `success`:
  - **Shared libraries** (`.github/workflows/shared-libraries.yml`), run 45894440363, attempt 1, head
    `30af0dad83f73d2a5cf5495b2d4e3716cc7ed0df`: Build and SL rules (ubuntu-24.04) 140164739820 and (windows-latest)
    140164740261, which test `Broiler.ECMAScript.slnf`, so its six `Broiler.ECMAScript.*` test projects; Native AOT
    probe (linux-x64) 140164740187 and (win-x64) 140164740123; Assurance check (SL8) 140164740280; Pack, validate and
    restore the shared stream (dry run) 140164740358.
  - **CI** (`.github/workflows/ci.yml`), run 45894440417, attempt 1, head `30af0dad83f73d2a5cf5495b2d4e3716cc7ed0df`:
    Changed paths 140164740496; ubuntu-24.04 / Release 140164954125 and windows-latest / Release 140164954275, whose
    Test step tests every project of `Broiler.JS.slnx` but those of `Broiler.ECMAScript.slnf`
    (`eng/run-tests.sh --engine-only`), so the fifteen `Broiler.JavaScript.*` test projects, and whose step "Test
    compiler scaling in isolation" ran on both systems; engine vectors (ubuntu-24.04) 140164954153, (windows-latest)
    140164954121 and (Windows against Linux) 140172345953; CI result 140172345920. No job was re-run.
  - **The jobs that carry the test projects of `Broiler.JS.slnx`**: the six `Broiler.ECMAScript.*` projects of
    `Broiler.ECMAScript.slnf` in Shared libraries' two Build and SL rules jobs, each "Passed!" with no test failed on
    both systems; the fifteen `Broiler.JavaScript.*` projects in ci.yml's ubuntu-24.04 / Release and windows-latest /
    Release, each "Passed!" with no test failed in each, and CompilerScaling's isolated line on both systems; and the
    engine corpus (`Category=EngineCorpus` of `Broiler.JavaScript.Vectors.Tests`) in the two engine vectors jobs, each
    passed ([`ci/test-projects.tsv`](ci/test-projects.tsv)). No `Broiler.ECMAScript` test project is in ci.yml's jobs.
    Two vectors jobs raised the warnings the workflow raises without failing: engine vectors (ubuntu-24.04) the
    vectors' timing, which also says that the slowest vector, `semantics/super/ee0131/0003`, took 5.000 s of the 5 s
    limit and was the one vector run again alone after finding no completion in the parallel pass (the sixth model
    commit's run 45843920047 warned the same of the same vector), and engine vectors (Windows against Linux) the
    Windows report against the Linux one; engine vectors (windows-latest) raised none. The logs hold them.
  - **The artifacts**, every one both runs list, downloaded and checked against the API's digest and size, every one
    matching ([`ci/artifact-digests.tsv`](ci/artifact-digests.tsv)): CI's `engine-vectors-ubuntu-24.04` 15153511934,
    `engine-vectors-windows-latest` 15154660071 and `nuget-packages` 15153223250; Shared libraries' `shared-packages`
    15152024864, `native-aot-probe-linux-x64` 15151924919 and `native-aot-probe-win-x64` 15152862991. The zips are not
    kept here, as the first six model commits' `ci/` kept none.
- **The default branch** stayed at `30af0dad` from the merge until after both CI runs had concluded
  ([`logs/main-checks.log`](logs/main-checks.log)): the merge was made at 20:54:50Z; Shared libraries' run concluded at
  21:01:16Z and ci.yml's at 21:14:27Z (each record's `updated_at`); the stage's first check, at 21:07:13Z, while ci.yml's
  run was still in progress, finds the default branch at `30af0dad`, and so does every later check, among them those at
  21:13:33Z, before ci.yml's run concluded, and at 21:19:26Z and 21:19:45Z, after it had; a ref that is not
  force-pushed cannot come back to a commit it has left; and GitHub's activity list of the default branch, read at
  21:20:59Z ([`logs/activity-main-20261126T212059Z.tsv`](logs/activity-main-20261126T212059Z.tsv)), has as its newest
  entry the merge from `4e27f13d` to `30af0dad` at 20:54:50Z and nothing after it. The check at 21:19:45Z was logged
  before the CI import (the CI read at 21:19:45Z-21:19:52Z and the artifacts' downloads at 21:20:01Z-21:20:08Z,
  [`ci/commands.txt`](ci/commands.txt)). That both runs had concluded rests on each run record's `updated_at`, which the
  imported [`ci/run-45894440417.json`](ci/run-45894440417.json) and
  [`ci/run-45894440363.json`](ci/run-45894440363.json) hold.

## The method's notes

- [`scripts/`](scripts/pb.sh) holds the scripts as run: copies of the sixth model commit's
  [`../part-b-84d84cc3/scripts/`](../part-b-84d84cc3/scripts/pb.sh), the same method as the first five model commits'.
  `pb.sh`, `identity.py` and `completeness.py` differ from those only in the worktree, the build tree and the suite
  folder each names and in their header comments; `test_projects.py` also names this commit's runs, each with one
  attempt, as its log sources, and the header lines it prints are rewritten for this commit; its matching of result
  lines is unchanged (`logs/notes.log`).
- `identity/model-caller-scope.txt` answers the question this commit raises (stage 5c's caller's-records lines in the
  canonical dump, and the records the walk now computes), as `../part-b-84d84cc3/identity/model-annexb.txt` answered
  stage 5b's; it is no part of the comparison's method, which is unchanged. It was written by a scratch script that
  runs each command it shows and writes its output and exit; the script is not kept. It is a copy of the scratch script
  that wrote `model-annexb.txt`, with the commits, its header and notes, and its commands' patterns changed to stage 5c's
  line names and members (stage 5b's and stage 5a's members kept in the search of Broiler.JS and the bundle), two
  commands added for the bridge's `InheritedContext` and `CallerScopes`' declaration, and its command printer's quoting
  widened to an argument holding `!` or `|`.
- Changed from the sixth model commit's folder: `logs/main-checks.log` is written by the stage's check script, one
  labelled line per check, from the first check on, so there is no raw log to keep beside it. Its line at 21:19:26Z is
  labelled "Shared libraries concluded, CI in progress" from the last status read before it (21:10:49Z); ci.yml's run had
  concluded at 21:14:27Z, before that check, as the next read showed; the line is left as written (`logs/notes.log`).
  This folder keeps one status read, taken through the API during the base run, when ci.yml's run was still in
  progress; the first read of both runs, at 21:09Z after the stage's worktree, was by a GitHub tool whose answers are
  not kept, and found Shared libraries' run concluded `success` in its first attempt and ci.yml's in progress; ci.yml's
  run was then polled through the API, its status alone, until it read completed (`logs/notes.log`).
- Nothing was set aside at this commit: no run was stopped or re-taken, and no CI job was re-run.

[`../hashes.txt`](../hashes.txt) gives the SHA-256 of every file here.
