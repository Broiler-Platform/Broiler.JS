# Part B's runs at the second model commit, `6c61879b`

**Taken:** 2026-11-25, after the merge of SJL-15.2's stage 2 (Broiler-Platform/Broiler.JS#1135), for
[`../decision-rule.md`](../decision-rule.md)'s part B. These are the rule's runs at its second model commit, taken as
part B's runs at the first model commit were taken, in [`../part-b-be6c4a5c/`](../part-b-be6c4a5c/README.md), and laid
out as that folder is. Their reading is in the bundle's [`README.md`](../README.md), the note of 2026-11-25 on part B's
runs at `6c61879b`. Nothing here changes the rule, a tool or a product file.

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
| [`6c61879b/`](6c61879b/logs/commit.txt) | `6c61879bce81b1a7badf987e16c7b0531030258d` | the second model commit after W2: the merge of Broiler-Platform/Broiler.JS#1135 (head `8b0c233c0a2d79ce2b9e81b525ce374b0d98e9dd`), SJL-15.2's stage 2 |
| [`2259fd4a/`](2259fd4a/logs/commit.txt) | `2259fd4a1860aefc68d49b8f6f9f0200fcb6cc4d` | its first parent, the merge of Broiler-Platform/Broiler.JS#1134 (ci.yml's Test step), no model commit |

`git rev-parse 6c61879b^1` is `2259fd4a1860aefc68d49b8f6f9f0200fcb6cc4d`. `2259fd4a` is no model commit: nothing from
the first model commit `be6c4a5c` to it changes a path under `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*`
(`git diff --stat be6c4a5c 2259fd4a -- 'Broiler.ECMAScript/Broiler.ECMAScript.Frontend*'` is empty; `logs/notes.log`).
Bundle `sjl-12-001`'s tool, `docs/evidence/sjl-12-001/tools/parse-corpus`, is the same tree at both commits,
`5ede61145de01136368c49354acc1de1c469fcb6`, the tree it had at the first model commit and its first parent, so it ran
byte-unchanged; the manifest `conformance/parse/corpus.manifest` is the same blob at both,
`2419aa2aabda30a9d3ca9aa703d38c6455859441` (SHA-256
`1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc`), and so is
`scripts/compliance/compare_parse_corpus.py`, `c0685c65899e74e5e2b0a11a270ef3ee65f3c669`
([`identity/inputs.txt`](identity/inputs.txt)).

## What was run

- **The local runs**, per commit, by [`scripts/pb.sh`](scripts/pb.sh), called once per commit, one at a time, the first
  parent first, through the machine's run script, which holds the build lock behind its idle gate (the same `run.sh`,
  `gate.sh`, `machine.sh` and `timed.py` as bundle `sjl-14-001`'s
  [`bridge/scripts/`](../../sjl-14-001/bridge/scripts/run.sh), byte for byte, as at the first model commit): a clean `git archive` of the commit on disk,
  outside every working tree; the tool built from it in Release with no build server, outside the tree; and run with
  `env -i`, so with `TZ=UTC` and no `LANG`, `LC_*` or `BROILER_*` variable, on the tool's own thread of 256 MB:
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
  - `scripts/compliance/compare_parse_corpus.py acceptance`, base `2259fd4a`, after `6c61879b`, the manifest at
    `6c61879b`, no classification file: [`identity/acceptance.txt`](identity/acceptance.txt) and `acceptance.json`. The
    script refuses runs that do not report every manifest input and variant at P and K, so it checks the default
    mode's completeness.
  - [`scripts/identity.py`](scripts/identity.py) over both modes' reports, every row line byte for byte and each
    report's end line: [`identity/rows-c.txt`](identity/rows-c.txt) (P's and K's outcome, exception type and message)
    and [`identity/rows-d1.txt`](identity/rows-d1.txt) (per input and variant, P's outcome and both dumps' hashes).
  - [`scripts/completeness.py`](scripts/completeness.py): each of the four reports holds exactly the manifest's inputs
    and variants, at P and K or once for the dumps, with its end line, and its outcomes counted, so that a run that read
    no input would show ([`identity/completeness.txt`](identity/completeness.txt)).
- **The CI runs at `6c61879b`**, in [`ci/`](ci/commands.txt): the runs GitHub Actions made on the push of `6c61879b` to
  the default branch, read by read-only API calls once both had concluded and kept as the API returned them, as
  [`../part-b-be6c4a5c/ci/`](../part-b-be6c4a5c/ci/commands.txt) kept the first model commit's: each run's record, its
  jobs with their steps, its artifacts with their digests, and its job logs, with
  [`ci/test-projects.tsv`](ci/test-projects.tsv), every test project of `Broiler.JS.slnx` at `6c61879b` with the result
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
during the stage up to the retention (`main-checks.log`, with its own note on the time before its first check; the later
checks are in the stage's scratch log only), the gate's log, the outer commands and runs, and the stage's notes
(`notes.log`).

## What the runs say

- **Both runs are complete.** At each commit the default mode reports every input and variant of the manifest at P and
  K and the dump mode every input and variant once, each report ending with its end line; the outcomes are counted the
  same at both commits ([`identity/completeness.txt`](identity/completeness.txt)).
- **They are identical in every field.** `compare_parse_corpus.py acceptance` finds no difference, with nothing to
  admit; `identity.py` finds every row line of both modes the same bytes in the same order ([`identity/`](identity/)).
- **The headers differ only in each engine assembly's module version id**, five lines per report, every other header
  line the same. The module version id is a build's name, not an answer: two builds of one commit, `2259fd4a`, SJL-15.2
  stage 2's part B rehearsal's in another build tree (a rehearsal, not a run of the rule, its reports in scratch) and
  part B's here, differ in the same five lines in the same way, with every row line identical
  ([`identity/two-builds-2259fd4a/`](identity/two-builds-2259fd4a/commands.txt)), as two builds of `8b1fa311` did at the
  first model commit ([`../part-b-be6c4a5c/identity/two-builds-8b1fa311/`](../part-b-be6c4a5c/identity/two-builds-8b1fa311/commands.txt)).
- **The CI runs.** Both workflows that run the test projects of `Broiler.JS.slnx` made one push run at `6c61879b`
  ([`ci/runs-at-6c61879b.json`](ci/runs-at-6c61879b.json)), and each concluded `success` in its first attempt, every
  job `success`:
  - **Shared libraries** (`.github/workflows/shared-libraries.yml`), run 45608012358, attempt 1, head
    `6c61879bce81b1a7badf987e16c7b0531030258d`: Build and SL rules (ubuntu-24.04) 139183848512 and (windows-latest)
    139183848089, which test `Broiler.ECMAScript.slnf`, so its six `Broiler.ECMAScript.*` test projects; Native AOT
    probe (linux-x64) 139183848831 and (win-x64) 139183848660; Assurance check (SL8) 139183848429; Pack, validate and
    restore the shared stream (dry run) 139183848487.
  - **CI** (`.github/workflows/ci.yml`), run 45608012312, attempt 1, head `6c61879bce81b1a7badf987e16c7b0531030258d`:
    Changed paths 139183848015; ubuntu-24.04 / Release 139184225787 and windows-latest / Release 139184225472, whose
    Test step tests every project of `Broiler.JS.slnx` but those of `Broiler.ECMAScript.slnf`
    (`eng/run-tests.sh --engine-only`), so the fifteen `Broiler.JavaScript.*` test projects, and whose step "Test
    compiler scaling in isolation" ran on both systems; engine vectors (ubuntu-24.04) 139184225543, (windows-latest)
    139184225589 and (Windows against Linux) 139193563588; CI result 139193563698. No job was re-run.
  - **The jobs that carry the test projects of `Broiler.JS.slnx`**: the six `Broiler.ECMAScript.*` projects of
    `Broiler.ECMAScript.slnf` in Shared libraries' two Build and SL rules jobs, each "Passed!" with no test failed on
    both systems; the fifteen `Broiler.JavaScript.*` projects in ci.yml's ubuntu-24.04 / Release and windows-latest /
    Release, each "Passed!" with no test failed in each, and CompilerScaling's isolated line on both systems; and the
    engine corpus (`Category=EngineCorpus` of `Broiler.JavaScript.Vectors.Tests`) in the two engine vectors jobs, each
    passed ([`ci/test-projects.tsv`](ci/test-projects.tsv)). No `Broiler.ECMAScript` test project is in ci.yml's jobs.
    The vectors jobs raised the warnings the workflow raises without failing (the vectors' timing, and the Windows
    report against the Linux one); the logs hold them.
  - **The artifacts**, every one both runs list, downloaded and checked against the API's digest and size, every one
    matching ([`ci/artifact-digests.tsv`](ci/artifact-digests.tsv)): CI's `engine-vectors-ubuntu-24.04` 15018899336,
    `engine-vectors-windows-latest` 15019303578 and `nuget-packages` 15019642137; Shared libraries' `shared-packages`
    15017997647, `native-aot-probe-linux-x64` 15018941752 and `native-aot-probe-win-x64` 15018192529. The zips are not
    kept here, as the first model commit's `ci/` kept none.
- **The default branch** stayed at `6c61879b` from the merge until after both CI runs had concluded
  ([`logs/main-checks.log`](logs/main-checks.log)): the merge was made at 11:49:35Z; the first check, at 12:00:34Z,
  finds the default branch at `6c61879b`, and a ref that is not force-pushed cannot come back to a commit it has left;
  Shared libraries' run concluded at 11:53:58Z and ci.yml's at 12:08:07Z (each record's `updated_at`); the check at
  12:15:01Z, after both, finds it at `6c61879b` still.

## The method's notes

- [`scripts/`](scripts/pb.sh) holds the scripts as run: copies of the first model commit's
  [`../part-b-be6c4a5c/scripts/`](../part-b-be6c4a5c/scripts/pb.sh), the same method. `pb.sh`, `identity.py` and
  `completeness.py` differ from those only in the worktree, the build tree and the suite folder each names and in their
  header comments (`identity.py`'s header there read "SJL-15.2 stage 1's part B rehearsal (not a run of the rule).", the
  stage-1 rehearsal's label); `test_projects.py` also names this commit's runs, each with one attempt, as its log
  sources, and its matching of result lines is unchanged (`logs/notes.log`).
- Nothing was set aside at this commit: no run was stopped or re-taken, and no CI job was re-run.

[`../hashes.txt`](../hashes.txt) gives the SHA-256 of every file here.
