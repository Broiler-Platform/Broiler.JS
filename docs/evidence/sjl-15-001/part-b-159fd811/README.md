# Part B's runs at the tenth model commit, `159fd811`

**Taken:** 2026-11-28, after the merge of SJL-15.2's stage 8 (Broiler-Platform/Broiler.JS#1151), for
[`../decision-rule.md`](../decision-rule.md)'s part B. These are the rule's runs at its tenth model commit, taken as
part B's runs at the first nine model commits were taken, in [`../part-b-be6c4a5c/`](../part-b-be6c4a5c/README.md),
[`../part-b-6c61879b/`](../part-b-6c61879b/README.md), [`../part-b-d87044c9/`](../part-b-d87044c9/README.md),
[`../part-b-56272150/`](../part-b-56272150/README.md), [`../part-b-4d8aa93a/`](../part-b-4d8aa93a/README.md),
[`../part-b-84d84cc3/`](../part-b-84d84cc3/README.md), [`../part-b-30af0dad/`](../part-b-30af0dad/README.md),
[`../part-b-2c48beb1/`](../part-b-2c48beb1/README.md) and [`../part-b-950bea8f/`](../part-b-950bea8f/README.md), and
laid out as those folders are. Their reading is in the bundle's [`README.md`](../README.md), the note of 2026-11-28 on
part B's runs at `159fd811`. Nothing here changes the rule, a tool or a product file.

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
| [`159fd811/`](159fd811/logs/commit.txt) | `159fd811f74d0f43e7ed7327081267a96f8f1196` | the tenth model commit after W2: the merge of Broiler-Platform/Broiler.JS#1151 (head `c8518398920616492243cc9ab34af9df5b0f0669`), SJL-15.2's stage 8 |
| [`b92ef028/`](b92ef028/logs/commit.txt) | `b92ef028327b7420d4f489cb805ef5270623e760` | its first parent, the merge of Broiler-Platform/Broiler.JS#1150 (stage 7's part B records), no model commit |

`git rev-parse 159fd811^1` is `b92ef028327b7420d4f489cb805ef5270623e760`. `b92ef028` is no model commit: nothing from
the ninth model commit `950bea8f` to it changes a path under `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*`
(`git diff --stat 950bea8f b92ef028 -- 'Broiler.ECMAScript/Broiler.ECMAScript.Frontend*'` is empty, and
`git diff --quiet 950bea8f 159fd811^1 -- 'Broiler.ECMAScript/Broiler.ECMAScript.Frontend*'` exits 0); and the loop over
the default branch's first-parent merges from the fifth model commit `4d8aa93a` that lists each merge changing such a
path lists exactly `84d84cc3`, `30af0dad`, `2c48beb1`, `950bea8f` and `159fd811` (`logs/notes.log`). Bundle
`sjl-12-001`'s tool, `docs/evidence/sjl-12-001/tools/parse-corpus`, is the same tree at both commits,
`5ede61145de01136368c49354acc1de1c469fcb6`, the tree it had at the first nine model commits and their first parents,
so it ran byte-unchanged; the manifest `conformance/parse/corpus.manifest` is the same blob at both,
`2419aa2aabda30a9d3ca9aa703d38c6455859441` (SHA-256
`1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc`), and so is
`scripts/compliance/compare_parse_corpus.py`, `c0685c65899e74e5e2b0a11a270ef3ee65f3c669`
([`identity/inputs.txt`](identity/inputs.txt)).

## What was run

- **The local runs**, per commit, by [`scripts/pb.sh`](scripts/pb.sh), called once per commit, one at a time, the first
  parent first, through the machine's run script, which holds the build lock behind its idle gate (the same `run.sh`,
  `gate.sh`, `machine.sh` and `timed.py` as bundle `sjl-14-001`'s
  [`bridge/scripts/`](../../sjl-14-001/bridge/scripts/run.sh), byte for byte, as at the first nine model commits): a
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
  a note beginning "# Notes, 2026-11-28" added at its end when it was retained; the commands above it are unchanged):
  - `scripts/compliance/compare_parse_corpus.py acceptance`, base `b92ef028`, after `159fd811`, the manifest at
    `159fd811`, no classification file: [`identity/acceptance.txt`](identity/acceptance.txt) and `acceptance.json`. The
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
  - [`identity/digest-model.txt`](identity/digest-model.txt): why stage 8's change, whose one change of behaviour is
    the probe's corpus digest mode requesting the semantic model, cannot reach this tool's `--dump` output or its P and
    K answers, read by read-only git with each command's output.
- **The CI runs at `159fd811`**, in [`ci/`](ci/commands.txt): the runs GitHub Actions made on the push of `159fd811` to
  the default branch, read by read-only API calls once both had concluded and kept as the API returned them, as
  [`../part-b-950bea8f/ci/`](../part-b-950bea8f/ci/commands.txt) kept the ninth model commit's: each run's record, its
  jobs with their steps, its artifacts with their digests, and its job logs, with
  [`ci/test-projects.tsv`](ci/test-projects.tsv), every test project of `Broiler.JS.slnx` at `159fd811` with the result
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
during the stage up to the retention (`main-checks.log`), the gate's log, the outer commands and runs, the status read
of the push runs as the API answered it (`status-*.tsv`), GitHub's activity list of the default branch read before the
retention (`activity-main-*.tsv`), and the stage's notes (`notes.log`).

## What the runs say

- **Both runs are complete.** At each commit the default mode reports every input and variant of the manifest at P and
  K and the dump mode every input and variant once, each report ending with its end line; the outcomes are counted the
  same at both commits ([`identity/completeness.txt`](identity/completeness.txt); for P and K,
  [`identity/acceptance.txt`](identity/acceptance.txt)).
- **They are identical in every field.** `compare_parse_corpus.py acceptance` finds no difference, with nothing to
  admit; `identity.py` finds every row line of both modes the same bytes in the same order ([`identity/`](identity/)).
- **The dumps and the answers are unchanged by stage 8.** Stage 8 adds no line kind to the library's canonical dump
  (`Broiler.ECMAScript/runners/SyntaxDump.cs`, unchanged by it) and no member, type or row to the model. Under
  `Broiler.ECMAScript/Broiler.ECMAScript.Frontend` it changes only `///` documentation lines and the project file's
  comment and package description, so the rows table, which the Frontend tests now hold equal to the stage table's
  `semantics` rows but EA0006, is unchanged. Its one change of behaviour is in the probe,
  `Broiler.ECMAScript/Broiler.ECMAScript.Probe`: its corpus digest mode (`CorpusDigest.cs`) now parses each request
  through `CorpusDigestRequest.WithModel` (`CorpusDigestRequest.cs`), a copy of the corpus reader's request with
  `ParseRequest.BuildSemanticModel` on. This tool does not use the probe: nothing under `Broiler.JS/` or in bundle
  `sjl-12-001` names it or its corpus digest, the tool references only Broiler.JS's projects, Broiler.JS's own projects
  reference the shared Algorithms, Frontend and Intl projects, not the probe, and the Frontend and Intl projects
  reference only Algorithms, which references nothing, so the probe is reachable from no project the tool builds (an
  inference from these references, shown in [`identity/digest-model.txt`](identity/digest-model.txt)). The earlier
  stages' model lines are written only when a parse builds the model, that is when the request sets
  `ParseRequest.BuildSemanticModel`, `false` by default: stage 7's `model-captures`, `model-captured` and `model-named`
  lines ([`../part-b-950bea8f/identity/model-captures.txt`](../part-b-950bea8f/identity/model-captures.txt)), written by
  the same lines of `SyntaxDump.cs` at both commits, stage 6's `model-module`, `model-request`, `model-attribute`,
  `model-import` and `model-export-*` lines
  ([`../part-b-2c48beb1/identity/model-module.txt`](../part-b-2c48beb1/identity/model-module.txt)), stage 5c's
  `model-eval-call` and `model-caller-scope` lines
  ([`../part-b-30af0dad/identity/model-caller-scope.txt`](../part-b-30af0dad/identity/model-caller-scope.txt)), stage
  5b's `model-annexb` lines and the `alias:` form
  ([`../part-b-84d84cc3/identity/model-annexb.txt`](../part-b-84d84cc3/identity/model-annexb.txt)), stage 5a's
  `model-environment` and `model-resolution` lines
  ([`../part-b-4d8aa93a/identity/model-resolution.txt`](../part-b-4d8aa93a/identity/model-resolution.txt)), stage 4's
  private-name lines ([`../part-b-56272150/identity/model-private.txt`](../part-b-56272150/identity/model-private.txt))
  and stage 3's `model-target` lines
  ([`../part-b-d87044c9/identity/model-target.txt`](../part-b-d87044c9/identity/model-target.txt)). This tool writes its
  own canonical dump of Broiler.JS's tree and reaches the Frontend only through Broiler.JS's parser; nothing under
  `Broiler.JS/` or in bundle `sjl-12-001` changes between the two commits, and nothing there sets
  `BuildSemanticModel`, uses the library's `SyntaxDump`, or names a member the earlier stages added to the model (the
  words `FreeNames` and `Captured` in Broiler.JS are its engine's own): the two `ParseRequest` constructions in
  Broiler.JS, the bridge's, which P and K reach, and the portable compiler's, leave the member at its default
  ([`identity/digest-model.txt`](identity/digest-model.txt)). So the `--dump` output stays identical, as every row of
  [`identity/rows-d1.txt`](identity/rows-d1.txt) shows, and so do P's and K's answers, as every row of
  [`identity/rows-c.txt`](identity/rows-c.txt) shows.
- **The headers differ only in each engine assembly's module version id**, five lines per report, every other header
  line the same. The module version id is a build's name, not an answer: two builds of one commit, `b92ef028`, SJL-15.2
  stage 8's rehearsal's in another build tree (a rehearsal, not a run of the rule, its reports in scratch) and part B's
  here, differ in the same five lines in the same way, with every row line identical
  ([`identity/two-builds-b92ef028/`](identity/two-builds-b92ef028/commands.txt)), as two builds of `26c6d6ec` did at the
  ninth model commit
  ([`../part-b-950bea8f/identity/two-builds-26c6d6ec/`](../part-b-950bea8f/identity/two-builds-26c6d6ec/commands.txt)),
  two builds of `59ccb11c` at the eighth, two builds of `4e27f13d` at the seventh, two builds of `d3e64b4d` at the
  sixth, two builds of `746591d2` at the fifth, two builds of `9ae6aee0` at the fourth, two builds of `77fe27aa` at the
  third, two builds of `2259fd4a` at the second and two builds of `8b1fa311` at the first.
- **The CI runs.** Both workflows that run the test projects of `Broiler.JS.slnx` made one push run at `159fd811`
  ([`ci/runs-at-159fd811.json`](ci/runs-at-159fd811.json)), and each concluded `success` in its first attempt, every
  job `success`:
  - **Shared libraries** (`.github/workflows/shared-libraries.yml`), run 46082299097, attempt 1, head
    `159fd811f74d0f43e7ed7327081267a96f8f1196`: Build and SL rules (ubuntu-24.04) 140783796366 and (windows-latest)
    140783796377, which test `Broiler.ECMAScript.slnf`, so its six `Broiler.ECMAScript.*` test projects; Native AOT
    probe (linux-x64) 140783796376 and (win-x64) 140783796520; Assurance check (SL8) 140783796159; Pack, validate and
    restore the shared stream (dry run) 140783796413.
  - **CI** (`.github/workflows/ci.yml`), run 46082299057, attempt 1, head `159fd811f74d0f43e7ed7327081267a96f8f1196`:
    Changed paths 140783795683; ubuntu-24.04 / Release 140783995804 and windows-latest / Release 140783995729, whose
    Test step tests every project of `Broiler.JS.slnx` but those of `Broiler.ECMAScript.slnf`
    (`eng/run-tests.sh --engine-only`), so the fifteen `Broiler.JavaScript.*` test projects, and whose step "Test
    compiler scaling in isolation" ran on both systems; engine vectors (ubuntu-24.04) 140783995790, (windows-latest)
    140783995806 and (Windows against Linux) 140788555118; CI result 140788555143. No job was re-run.
  - **The jobs that carry the test projects of `Broiler.JS.slnx`**: the six `Broiler.ECMAScript.*` projects of
    `Broiler.ECMAScript.slnf` in Shared libraries' two Build and SL rules jobs, each "Passed!" with no test failed on
    both systems; the fifteen `Broiler.JavaScript.*` projects in ci.yml's ubuntu-24.04 / Release and windows-latest /
    Release, each "Passed!" with no test failed in each, and CompilerScaling's isolated line on both systems; and the
    engine corpus (`Category=EngineCorpus` of `Broiler.JavaScript.Vectors.Tests`) in the two engine vectors jobs, each
    passed ([`ci/test-projects.tsv`](ci/test-projects.tsv)). No `Broiler.ECMAScript` test project is in ci.yml's jobs.
    Two vectors jobs raised the warnings the workflow raises without failing: engine vectors (ubuntu-24.04) the
    vectors' timing, which also says that the slowest vector, `semantics/super/ee0131/0003`, took 5.000 s of the 5 s
    limit and was the one vector run again alone after finding no completion in the parallel pass (the ninth model
    commit's run 46029175113 warned the same of the same vector), and engine vectors (Windows against Linux) the
    Windows report against the Linux one; engine vectors (windows-latest) raised none. The logs hold them.
  - **The artifacts**, every one both runs list, downloaded and checked against the API's digest and size, every one
    matching ([`ci/artifact-digests.tsv`](ci/artifact-digests.tsv)): CI's `engine-vectors-ubuntu-24.04` 15238206491,
    `engine-vectors-windows-latest` 15237902322 and `nuget-packages` 15238570043; Shared libraries' `shared-packages`
    15237473927, `native-aot-probe-linux-x64` 15237755717 and `native-aot-probe-win-x64` 15237538881. The zips are not
    kept here, as the first nine model commits' `ci/` kept none.
- **The default branch** stayed at `159fd811` from the merge until after both CI runs had concluded: the merge was made
  at 2026-11-27T23:24:10Z; Shared libraries' run concluded at 23:33:05Z and ci.yml's at 23:40:30Z (each record's
  `updated_at`, which the imported [`ci/run-46082299057.json`](ci/run-46082299057.json) and
  [`ci/run-46082299097.json`](ci/run-46082299097.json) hold). This stage began after both had concluded: its first
  check of the default branch, at 2026-11-28T00:20:19Z, and every later one up to the retention find it at `159fd811`
  ([`logs/main-checks.log`](logs/main-checks.log)); a ref that is not force-pushed cannot come back to a commit it has
  left; and GitHub's activity list of the default branch, read at 01:07:21Z
  ([`logs/activity-main-20261128T010721Z.tsv`](logs/activity-main-20261128T010721Z.tsv)), has as its newest entry the
  merge from `b92ef028` to `159fd811` at 2026-11-27T23:24:10Z and nothing after it. So the default branch did not move
  between the merge and the runs' conclusions. The CI import was read after both runs had concluded (the CI read at
  00:25:11Z-00:25:17Z and the artifacts' downloads at 00:25:26Z-00:25:33Z, [`ci/commands.txt`](ci/commands.txt)).

## The method's notes

- [`scripts/`](scripts/pb.sh) holds the scripts as run: copies of the ninth model commit's
  [`../part-b-950bea8f/scripts/`](../part-b-950bea8f/scripts/pb.sh), the same method as the first eight model commits'.
  `pb.sh`, `identity.py` and `completeness.py` differ from those only in the worktree, the build tree and the suite
  folder each names and in their header comments; `test_projects.py` also names this commit's runs, each with one
  attempt, as its log sources, and the header lines it prints are rewritten for this commit; its matching of result
  lines is unchanged (`logs/notes.log`).
- `identity/digest-model.txt` answers the question this commit raises (stage 8's probe requesting the model), as
  `../part-b-950bea8f/identity/model-captures.txt` answered stage 7's; it is no part of the comparison's method, which
  is unchanged. It was written by a scratch script that runs each command it shows and writes its output and exit; the
  script is not kept. It is a copy of the scratch script that wrote `model-captures.txt`, with the commits, its header
  and notes changed. Stage 8 adds no dump line kind, so that script's first thirteen commands are kept with only the
  commits changed; its fourteenth, fifteenth and sixteenth (stage 7's walk maps, their writes and its new members) are
  replaced by this script's fourteenth to twentieth: the files stage 8 changes under the runners, the Frontend and the
  probe; every line it changes under the Frontend that is not a `///` line, with the project files and without them;
  where the probe turns `BuildSemanticModel` on; that Broiler.JS and the bundle name neither the probe nor its corpus
  digest; and the shared projects that Broiler.JS's projects and the Frontend and Intl projects reference. Its runner is
  unchanged; its command printer also quotes an argument holding `*`, `?` or `[`, so that each printed command can be
  run as shown. The file was first written before part B's runs and written again after their comparison, with only its
  notes' last sentence changed to cite `rows-d1.txt` and `rows-c.txt`; every command and its output is unchanged.
- Changed from the ninth model commit's folder: both push runs had concluded before this stage began, and this stage's
  status read through the API, at 00:20:30Z (`logs/status-20261128T002030Z.tsv`), and its CI import and artifacts'
  downloads were read before either of part B's runs, not between them; that both runs had concluded rests not on the
  status read but on each run record's `updated_at` ([`ci/commands.txt`](ci/commands.txt)). The stage's first pass
  stopped before any build, because the free disk did not meet the 8 GiB its checks ask before each build and each run;
  the stage resumed once disk had been freed elsewhere on the machine, and every disk check of both runs passed at its
  first check (`b92ef028/logs/disk.log`, `159fd811/logs/disk.log`; `logs/notes.log`). Stage 8's rehearsal, whose build
  of `b92ef028` is the second of the two builds above, named its suite folder `/dev/shm/js-s158-pb-suite`, the folder
  this stage's `pb.sh` names; each script removes it at its end, and each of this stage's runs extracted it afresh
  after checking the archive's SHA-256 (`b92ef028/logs/suite.log`, `159fd811/logs/suite.log`; `logs/notes.log`). That
  rehearsal's script differs from this stage's in its disk checks only
  ([`identity/two-builds-b92ef028/commands.txt`](identity/two-builds-b92ef028/commands.txt)).
- Nothing was set aside at this commit: no run was stopped or re-taken, and no CI job was re-run.

[`../hashes.txt`](../hashes.txt) gives the SHA-256 of every file here.
