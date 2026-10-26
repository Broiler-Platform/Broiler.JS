# Evidence bundle SJL-12-001 (Broiler.JS) - the frontend contract and the parser's in-place preparation

**Milestone:** SJL-12 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-12-frontend-contract.md`: its Broiler.JS clauses 1 and
2, and this repository's half of shared clause 8.

**Rule:** [`decision-rule.md`](decision-rule.md), committed at SJL-12.1 before any change it judges:
- part A is test262 per path in UTC, with negative-metadata tests;
- part B is the parser's architecture test and its witness, over the parser assembly at the rule's commit and at
  the after commit;
- part C is the parse corpus's acceptance, at the parser alone and at the whole compile;
- part D is the determinism of the parser and of the compiler's for-loop pass, over two runs in two orders and two
  threads.

Clause 3, the contract record, is read from the plan. The catalogue of SJL-12.3 and the public-API records of
SJL-12.5 are read from the tree. The rule names them and does not decide them.

**SJL-12 is not an area verdict.** It applies no extraction gate; the frontend's verdict is SJL-13.1's (Broiler.VM's
JSD-0080, decision D1). **The contract record:** [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "The
frontend contract (SJL-12)", with Broiler.VM's JSD-0080,
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0080-the-shared-frontend-contract-and-the-in-place-preparation.md`.

**Owner:** the Broiler.JS maintainer. One person holds every role (the programme's EX-30, recorded in Broiler.VM).
**Reviewer:** none, and nothing here has been read by anyone but its author.

**State, 2026-10-26, at SJL-12's after evidence: every part of the rule is read at the after commit `c29d653f`, and
each is MET under the rule as written (sections 3 to 7). None of it is accepted, and SJL-12 is not an area
verdict.**
- Part A's base is taken at the rule commit by `test262 evidence`, with `include_negative` true: the fifth dispatch,
  on 2026-10-25. The four before it are void; they are retained and named, not judged (section 1). Part B's base is
  the rule commit's parser assembly, built when part B runs.
- Parts C and D's bases are taken at the corpus commit `1fe542ff`, with part C's control (section 1).
- Section 2 names, before any after run, how this README reads part C's class D *(added 2026-10-25, at SJL-12.5's
  records)*.
- The after runs are taken at the after commit, the merge that lands SJL-12.5's last item, the compiler's for-loop
  pass: part A's by `test262 evidence` on 2026-10-25, and parts B, C and D's on the programme's local machine on
  2026-10-26 (section 3). Parts A, B, C and D are each MET (sections 4 to 7). Section 8 reads this repository's
  clauses from them; SJL-12 may not meet its gate before SJL-3 meets SJL-3's.

*(Revised 2026-10-26, at SJL-12's after evidence, when the after runs were imported and read. The state read
"**State, 2026-10-25, at SJL-12.5: the rule, and parts A, C and D's bases. Nothing here is a verdict.**", and its
third item "The after runs follow SJL-12.5's last item, the compiler's for-loop pass."; the last item is new.)*

*(Revised 2026-10-25, at SJL-12.5, when part A's base was taken. The state read "State, 2026-10-20, at SJL-12.4: the
rule, and parts C and D's bases. Nothing here is a verdict.", and its first item "Part A's base is not yet taken. Two
dispatches of `test262 evidence` at the rule commit, with `include_negative` true, are void; they are retained and
named, not judged (section 1). Part B's base is the rule commit's parser assembly, built when part B runs.")*

*(Revised 2026-10-20, at SJL-12.4, when parts C and D's bases were committed. The state read "State, 2026-10-20, at
SJL-12.5: rules only. Nothing here is a verdict, and no base has been taken.", and its second item "Parts C and D's
bases follow SJL-12.4, which adds the parse corpus and the tool.")*

*(Revised 2026-10-20, at SJL-12.5, when two dispatches of part A's base had ended void. The state read "State,
2026-10-19, at SJL-12.1: rules only. Nothing here is a verdict, and nothing has been taken.", and its first item
"Part A's base is taken at the rule's commit by `test262 evidence`, with `include_negative` true. Part B's base is
the rule commit's parser assembly, built when part B runs.")*

**What the bundle will hold:**
- `base/`: part A's base run, imported unchanged with the digest of its zip, and parts C and D's base runs;
- `corpus/`: the parse corpus's commit, its manifest's digest and the ancestry the rule's conditions ask for;
- `after/`: the after runs of every part, each comparison's report and the table of every C1, M, X and D row;
- `tools/`: the parse-corpus tool, added at SJL-12.4;
- `controls/`: each control's patch or copy, and its output;
- `catalogue/`: SJL-12.3's completeness check of the early-error catalogue (task 3.11), with the inventories it read
  and the checker that re-runs it (added 2026-10-20). It is a record of the catalogue, not a part of this rule.

*(Added 2026-10-20, at SJL-12.5: `base/` also holds two void attempts at part A's base, each in its own folder named
as void, so that none of their files can be taken for the base's or overwrite another's (section 1).)*

*(Added 2026-10-25, at SJL-12.5: part A's base is in its own folder, `base/part-a/`, apart from parts C and D's
files, and `base/` holds four void attempts, each in its own folder named as void (section 1).)*

*(Added 2026-10-26, at SJL-12's after evidence: part A's after run is in its own folder, `after/part-a/`, with the
rule's comparison of it with the base in `after/part-a/comparison/`; parts B, C and D's after runs are in `after/`,
their records in `after/logs/`; controls K, K1 and K2 are in `controls/`, beside part C's, with their records in
`controls/logs/`. The table of every C1, M, X and D row is in sections 4 and 6, where each is empty: no verdict and
no acceptance changed (section 3).)*

**It holds no figure until a run is taken**, and then only in the run's own files and this README's reading of them.

The owner's direction of 2026-10-18, "Please do all recommended decisions and skip waiting for review.", under which
JSD-0080 records its decisions, changes no rule: nothing is accepted without review, and SJL-12 may not meet its gate
before SJL-3 meets SJL-3's.

Broiler.VM judges its own half in its own bundle of the same name. This bundle reads none of its runs.

## 1. The bases (2026-10-20)

**Written 2026-10-20; part A's base added 2026-10-25.** This section reads what this bundle retains of the bases. It
judges nothing: the rule compares each base with a run at the after commit, which does not exist yet.

| Part | Commit | Taken | Retained in |
|---|---|---|---|
| A | The rule commit, `4d813f2ac863895b9be5c4619b7886c9ddd71077` | 2026-10-25, 13:06Z to 14:11Z, by `test262 evidence`. Four attempts before it, from 2026-10-19 to 2026-10-21, are void | [`base/part-a/`](base/part-a/imported.txt); the void attempts in [`base/part-a-void-attempt-1/`](base/part-a-void-attempt-1/imported.txt) to [`base/part-a-void-attempt-4/`](base/part-a-void-attempt-4/imported.txt) |
| C and D | The corpus commit, `1fe542ff459935586cdf495e4a0e3d798248948a` | 2026-10-20, 12:38Z to 12:57Z, on the programme's local machine | [`base/`](base/) |
| C's control | Made from part C's base report alone | 2026-10-20 | [`controls/`](controls/) |

*(Revised 2026-10-25, at SJL-12.5, when part A's base was taken. Part A's row read "Not yet taken. Two attempts, on
2026-10-19 and 2026-10-20, are void" in its third column, and named the first two void attempts' folders alone in its
fourth; the section's first words read "**Written 2026-10-20.**")*

Part B's base, the rule commit's parser assembly, is built when part B runs, and is not in this bundle.

### Part A's base, at the rule commit

**Part A's base is taken: the fifth dispatch of `test262 evidence` at the rule commit, on 2026-10-25.** It is
retained, unchanged, in [`base/part-a/`](base/part-a/imported.txt). The four dispatches before it are void. Each is
retained, unchanged, in its own folder named as void, and none is the base.

*(Revised 2026-10-25, at SJL-12.5. The paragraph read "**Part A's base is not yet taken.** Two dispatches of `test262
evidence` at the rule commit are void. Each is retained, unchanged, in its own folder named as void, and neither is
the base.")*

**The rule commit** is `4d813f2ac863895b9be5c4619b7886c9ddd71077`, the default-branch merge of Broiler.JS#1059, which
carries [`decision-rule.md`](decision-rule.md) as it reads, with the corrections its section "Revisions" lists. The
parse corpus's README names it as the rule commit ([`conformance/parse/README.md`](../../../conformance/parse/README.md)).

| Attempt | Workflow run | Dispatched, completed | Artifact `test262-evidence`, and the SHA-256 of its zip | Retained in |
|---|---|---|---|---|
| 1, void | [39335653429](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/39335653429) | 2026-10-19 22:32Z, 23:40Z | id 12229603117, `22023f0310850eda5d7ddfff54dad41a1e3c1fb7b4aeb4ea24f30b28018a9241` | [`base/part-a-void-attempt-1/`](base/part-a-void-attempt-1/imported.txt) |
| 2, void | [39379425075](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/39379425075) | 2026-10-20 06:16Z, 07:23Z | id 12246874482, `25d837f27a47552b3e718481b794e52d05896289e204a14dcf7430fcdeb416fa` | [`base/part-a-void-attempt-2/`](base/part-a-void-attempt-2/imported.txt) |
| 3, void | [39452789227](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/39452789227) | 2026-10-20 15:55Z, 17:01Z | id 12282379197, `d078977e128ba0b4dcb362bc8b8acaa76ba4459ae40c3c521a87807f78b18e84` | [`base/part-a-void-attempt-3/`](base/part-a-void-attempt-3/imported.txt) |
| 4, void | [39566106261](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/39566106261) | 2026-10-21 08:00Z, 09:08Z | id 12333646224, `c761cbff4c3c92c30abb6f909d461b93e3d33c3b7a3ba5667c0a9570107f245a` | [`base/part-a-void-attempt-4/`](base/part-a-void-attempt-4/imported.txt) |
| 5, the base | [40196127730](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/40196127730) | 2026-10-25 13:06Z, 14:11Z | id 12601938718, `c6bef549d49f7ddad3f714518a88cdca0462ab027652b61393ffaa4bc0e1b80a` | [`base/part-a/`](base/part-a/imported.txt) |

*(Rows 3 to 5 added 2026-10-25, at SJL-12.5.)*

- **What ran.** Attempts 1 and 2 were dispatched from `main` when `main` was the rule commit itself, and tested it:
  `run.txt` reads `dispatched ref: refs/heads/main at 4d813f2ac863895b9be5c4619b7886c9ddd71077` and the same tested
  commit. So the workflow and `scripts/compliance/` that ran are the rule commit's. Attempts 3, 4 and 5 were
  dispatched from later commits of `main`, `17b91384`, `2345da11` and `792ff56e`, each of which descends from the rule
  commit and holds `.github/workflows/test262-evidence.yml` as the rule commit's own blob, `7158fb2a`; each named the
  rule commit as the commit to test, and `run.txt` reads it as the tested commit. Both of the workflow's checkouts,
  the shards' and the merging job's, are of the tested commit, so the shard action and `scripts/compliance/` that ran
  are the rule commit's in these three as well. Each attempt ran with suite `ccaac100`, four workers, a
  timeout of 30 s, timeouts run again and negative-metadata tests included (`run.txt`'s inputs, and
  `includeNegative: True`), and with `time_zone` empty: `run.txt`'s inputs name no `time_zone`, and its host time
  zone line and `host-zone.log` read `TZ` unset.
  *(Revised 2026-10-20, after review: the sentence read "Each ran with suite `ccaac100`, four workers, a timeout of
  30 s, timeouts run again, `time_zone` empty and negative-metadata tests included (`run.txt`'s inputs, and
  `includeNegative: True`)." `run.txt`'s inputs hold no `time_zone`.)*
  *(Revised 2026-10-25, at SJL-12.5, when attempts 3 to 5 were added. The item began "Each was dispatched from `main`
  when `main` was the rule commit itself, and tested it:", and its last sentence began "Each ran with suite".)*
- **Why attempts 1 to 4 are void.** Each run's conclusion is failure. Its job `collect` failed at the workflow's step
  "Require a complete run", which wrote "The shards did not all run the same shell (shell-hashes.log)." and "The
  shards did not all run in the host time zone (host-zone.log)." and no other failure (GitHub's record of each run
  and that job's log, read 2026-10-20 for attempts 1 and 2 and 2026-10-25 for attempts 3 and 4). The two records the
  step names are retained in each folder:
  - **Attempt 1.** [`shell-hashes.log`](base/part-a-void-attempt-1/shell-hashes.log) reads "shards: 8; distinct
    lists: 2": shard 4's list, `a6515ae3...`, against the other seven's, `433ab0c7...`.
    [`host-zone.log`](base/part-a-void-attempt-1/host-zone.log) reads "shards: 8; in the expected zone: 7": shard 4
    reads tzdata 2026e, against the merging job's 2026c.
  - **Attempt 2.** [`shell-hashes.log`](base/part-a-void-attempt-2/shell-hashes.log) reads "shards: 8; distinct
    lists: 2": shards 0, 4, 6 and 7, `433ab0c7...`, against shards 1, 2, 3 and 5, `a6515ae3...`; and "the re-run's
    shell does NOT hash as the shards' did". [`host-zone.log`](base/part-a-void-attempt-2/host-zone.log) reads
    "shards: 8; in the expected zone: 4": shards 0, 4, 6 and 7 read tzdata 2026c, against the merging job's 2026e.
  - **Attempt 3.** [`shell-hashes.log`](base/part-a-void-attempt-3/shell-hashes.log) reads "shards: 8; distinct
    lists: 2": shards 0, 1 and 4, `433ab0c7...`, against shards 2, 3, 5, 6 and 7, `a6515ae3...`.
    [`host-zone.log`](base/part-a-void-attempt-3/host-zone.log) reads "shards: 8; in the expected zone: 3": shards
    0, 1 and 4 read tzdata 2026c, the merging job's, and the other five 2026e.
  - **Attempt 4.** [`shell-hashes.log`](base/part-a-void-attempt-4/shell-hashes.log) reads "shards: 8; distinct
    lists: 2": shard 5's list, `433ab0c7...`, against the other seven's, `a6515ae3...`; and "the re-run's shell does
    NOT hash as the shards' did". [`host-zone.log`](base/part-a-void-attempt-4/host-zone.log) reads "shards: 8; in
    the expected zone: 1": shard 5 reads tzdata 2026c, the merging job's, and the other seven 2026e.
  - In all four attempts every shard reads `TZ` unset and UTC +0000 at both instants, and the shards fall into the
    same two groups by their shell's list as by their tzdata release. `run.txt` records the merging job's runner
    image, `ubuntu24` 20261012.334.1 in attempts 1, 3 and 4 and 20261018.339.1 in attempt 2; the artifact records no
    shard's image.
  *(Revised 2026-10-25, at SJL-12.5, when attempts 3 and 4 were added. The item's heading read "Why each is void.",
  its third sentence ended "(GitHub's record of each run and that job's log, read 2026-10-20).", and its last
  sub-item began "In both attempts" and read "`ubuntu24` 20261012.334.1 in attempt 1 and 20261018.339.1 in attempt
  2".)*
- **Why attempt 5 is the base.** Its conclusion is success, and so is every job's, the step "Require a complete run"
  of job `collect` among them (GitHub's record of the run and its jobs, read 2026-10-25). The records that step reads
  are retained in [`base/part-a/`](base/part-a/imported.txt):
  - [`shell-hashes.log`](base/part-a/shell-hashes.log) reads "shards: 8; distinct lists: 1", every shard's list
    `433ab0c7...`, and "the re-run's shell hashes as the shards' did";
  - [`host-zone.log`](base/part-a/host-zone.log) reads "shards: 8; in the expected zone: 8": every shard, and the
    merging job, reads `TZ` unset, UTC +0000 at both instants and tzdata 2026c;
  - [`merge.log`](base/part-a/merge.log) reads "0 incomplete shard(s)", and [`run.txt`](base/part-a/run.txt) names
    the suite commit `ccaac100` and `includeNegative: True`;
  - the one path that timed out was run again alone, as the rule says, and both the run and the re-run are retained
    ([`timed-out-paths.txt`](base/part-a/timed-out-paths.txt), [`rerun-timeouts.log`](base/part-a/rerun-timeouts.log),
    [`rerun-timeouts.json`](base/part-a/rerun-timeouts.json)).

  **Its runners.** Every shard's job and job `collect` ran on one image, `ubuntu-26.04` version 20261012.163.1, as
  the head of each job's own log reads it; those heads are copied into
  [`runner-images.txt`](base/part-a/runner-images.txt). `run.txt`'s runner line, which the workflow writes from the
  merging job's `ImageOS` and `ImageVersion`, reads `ubuntu26` 20261023.163.3. The two are recorded as each source
  gives them; the rule reads neither, and judges the runners by their shells' lists and their host-zone records.
  Attempts 1 to 4 ran on `ubuntu24` images.
- **What the after run must match.** The rule asks that "both runs' host-zone records read alike, line for line: TZ
  unset, UTC +0000 at both instants, and one tzdata release". The base's record reads tzdata 2026c. If the after
  run's reads another release, the rule's own words apply: "if the two runs' host-zone records differ, because the
  runners' tzdata moved between them, the part is not judged until the base is taken again at the same commit in the
  after run's zone record."
- **Named, not judged.** No void attempt is part A's base, and nothing here reads their rows: this section quotes no
  total and compares nothing with them. The base's rows are read at the after commit only, by the rule's comparison;
  this section quotes no total of the base either.
  *(Revised 2026-10-25, at SJL-12.5. The item read "Neither attempt is part A's base, and nothing here reads their
  rows: this section quotes no total and compares nothing with them.")*
- **Retained unchanged.** Each of the five folders holds every file of its artifact, 26 entries, each compared with
  its entry in the zip, none differing. Beside them are `imported.txt`, which records the run, the artifact's id and
  the digest of the zip as downloaded, which is the digest GitHub records, and why the attempt is void or complete;
  in `base/part-a/` only, `runner-images.txt`; and `hashes.txt`, the SHA-256 of every other file of the folder.
  `sha256sum -c hashes.txt`, run in each folder, passes.
  *(Revised 2026-10-25, at SJL-12.5. The item began "Each folder holds", and named no `runner-images.txt` and no
  complete attempt.)*

### Parts C and D's bases, at the corpus commit

| | |
|---|---|
| Commit | The corpus commit, `1fe542ff459935586cdf495e4a0e3d798248948a`, the default-branch merge of Broiler.JS#1062 ([`corpus/corpus-commit.txt`](corpus/corpus-commit.txt)) |
| Rule | [`decision-rule.md`](decision-rule.md), the same blob at the rule commit and at the corpus commit |
| Machine | The programme's local machine: four processors, .NET SDK 10.0.401 with runtime 10.0.12, Ubuntu 24.04.5 LTS, `linux-x64` ([`base/machine.txt`](base/machine.txt)) |
| Taken | 2026-10-20: the tool's build from 12:38Z, part C's runs 1 and 2 from 12:45Z, and part D's runs 1 to 3 from 12:53Z to 12:57Z ([`base/logs/runs.tsv`](base/logs/runs.tsv)). Broiler.VM's build and runs for its own bundle were taken on the same machine, the build between this tool's build and part C's run 1 and the runs after 12:57Z, none while one of these ran |
| Tool | [`tools/parse-corpus/`](tools/parse-corpus/README.md) as the corpus commit holds it, built in Release from a clean detached checkout of the corpus commit against that commit's own projects: no error, and 56 distinct warnings, none of them the tool's ([`base/logs/build.log.gz`](base/logs/build.log.gz)). The SHA-256 of the 25 assemblies the build wrote is in [`base/binaries.sha256`](base/binaries.sha256). Every command, in order, is in [`base/commands.txt`](base/commands.txt) |
| test262 | Read from a checkout at `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e` with no local change. The tool checks every test262 input against the manifest's SHA-256 before it parses anything, and refuses the run otherwise |

**The idle machine** ([`base/machine.txt`](base/machine.txt), [`base/logs/gate.log`](base/logs/gate.log)).
- Before the build and before each run, a gate required a 1-minute load average of at most 1.0 and no `dotnet` or
  `python` process, and recorded the load, the processor count, the operating system and the SDK
  ([`base/logs/pre-run.txt`](base/logs/pre-run.txt)). One gate waited: before part C's run 1 it read 1.20 at
  12:43:24Z, left by the Broiler.VM slice compiler's build, and went ahead at 12:45:24Z, reading 0.16. Every other
  gate went ahead at its first look. No other build, test or run was taken on the machine while these were taken.
- The gate read the 1-minute figure only. `machine.txt` says what the 5- and 15-minute figures carry: the 15-minute
  figure already read 1.00 before this work's first build.
- At 12:43:13Z another session fetched and fast-forwarded Broiler.VM's main checkout, after the slice compiler's build
  had ended and before part C's run 1's gate first looked at the machine, so during no run. It was a git operation,
  not a build, test or run.
- Both points were corrected in `machine.txt` before it was committed, and its dated note quotes the words it
  replaced.

**The environment** is the rule's, as every report's header records it: `TZ=UTC`; `LANG`, every `LC_*` and every
`BROILER_*` variable unset; the invariant culture; threads of 256 MB, one, or two for part D's run 3; and a compile
stack of 0, so that K compiles on the tool's own thread ([`tools/parse-corpus/README.md`](tools/parse-corpus/README.md),
"Environment, threads and stack").

**How the base is bound to the corpus commit.**
- **The reports name the commit.** Every report's header names each product assembly it loaded with its
  informational version, `0.1.0-preview.1+1fe542ff459935586cdf495e4a0e3d798248948a`, which carries the commit the
  assembly was built from, and its module version id, which identifies the build. The five reports name the same
  five assemblies, versions and ids, and so do the outputs of part C's comparison and control for both reports each
  compares. Each header
  also names the manifest's SHA-256, `1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc`, which is the
  corpus commit's ([`corpus/corpus-commit.txt`](corpus/corpus-commit.txt)).
- **Nothing written by a machine shows that the checkout was clean.** No record of its `git rev-parse HEAD` or
  `git status --porcelain` was retained, and the checkout was removed after the runs, so none can be made now. Three
  records written when the runs were taken add what the headers do not show:
  - [`commands.txt`](base/commands.txt) makes the checkout with `git worktree add --detach` at the corpus commit,
    named by all forty digits, and builds and runs from it;
  - [`machine.txt`](base/machine.txt) records that its `git status` was empty before and after the build;
  - [`binaries.sha256`](base/binaries.sha256) holds the SHA-256 of the 25 assemblies the build wrote.

  The first two are accounts written at the time. `binaries.sha256` is the one a reader can check: it is what a
  Release build of the corpus commit, at the checkout path `commands.txt` records and with the SDK `machine.txt`
  names, is compared with. No such build has been made again.
- **The corpus commit itself** is recorded from git in [`corpus/corpus-commit.txt`](corpus/corpus-commit.txt), as
  the layout above says `corpus/` holds it: its identity, its manifest's SHA-256 and the three conditions below.
- **The after runs are to retain**, in their own `logs/`, `git rev-parse HEAD` and `git status --porcelain` of their
  checkout before and after its build.

**The corpus commit's three conditions, read from git** ([`corpus/corpus-commit.txt`](corpus/corpus-commit.txt)).
- **It descends from the rule commit.** `git merge-base --is-ancestor` exits 0.
- **It names only the listed paths.** `git diff --name-only` between the two names 2,308 paths, each under a path the
  condition lists:
  - 2,274 under `conformance/parse/`;
  - 5 under `scripts/`: the corpus's builder, the comparison, the catalogue's extractor and two tests;
  - 2 under `Broiler.ECMAScript/docs/`: the early-error catalogue;
  - 27 under `docs/`: this bundle's README, catalogue and tool, the plan pair, and bundle `sjl-3-001`'s README and
    manifest.
- **It comes before every product change.** Read at the default branch, which is the corpus commit itself, `git log`
  from the rule commit over the paths the condition lists names no commit: no commit after the rule commit changes
  one of them. With `--full-history` it also names `b1ac1b8a`, the merge that took the rule commit into the
  catalogue's branch. On every path the condition lists, `b1ac1b8a` holds the rule commit's own files: against its
  second parent, the rule commit, it changes none of them. Against its first parent, `a6b29eaf`, which does not
  descend from the rule commit, it changes `Broiler.ECMAScript/CHANGELOG.md`, and only to the rule commit's own text
  of that file, which the merge brought in. A merge that holds, on those paths, exactly what the rule commit holds makes
  no change to them after the rule commit, so the condition holds. The condition is read again at the after commit.
  *(Revised 2026-10-20, after review: the item began "It comes before every product change." and its third sentence
  read "With `--full-history` it names `b1ac1b8a` alone, a merge that took the rule commit into the catalogue's
  branch and changes no listed path against the rule commit; it is itself an ancestor of the corpus commit." That
  `b1ac1b8a` is an ancestor of the corpus commit does not meet the condition, which asks the reverse.)*
  *(Revised again 2026-10-20, by the single writer: the item's heading ended ", as this section reads the
  condition.", and after "so on those paths it holds the rule commit's files." it read "This section reads a commit
  that "changes a path" as one that changes it against the rule commit, and so reads `b1ac1b8a` as no product change.
  That is a reading, not something git decides. On a reading that counts a change against either parent, `b1ac1b8a`
  is a product change after the rule commit, the corpus commit descends from it and does not come before it, and the
  condition fails; the rule makes a part whose corpus commit fails its conditions void." It was replaced because
  `b1ac1b8a` adds, on the listed paths, nothing the rule commit does not already hold: what it differs by from its
  first parent is the rule commit's own content, so counting that as a change after the rule commit would count the
  rule commit against itself.)*

**The runs** ([`base/logs/runs.tsv`](base/logs/runs.tsv); each run's standard error in [`base/logs/`](base/logs/),
its standard output empty).

| Run | What it ran | Exit | Retained |
|---|---|---|---|
| Part C, run 1 | Entry points P and K for every input and variant, in the manifest's order, on one thread | 0 | [`parse-corpus-c.tsv.gz`](base/parse-corpus-c.tsv.gz) |
| Part C, run 2 | The same | 0 | [`parse-corpus-c-run2.tsv.gz`](base/parse-corpus-c-run2.tsv.gz) |
| Part D, run 1 | `--dump`, in the manifest's order | 0 | [`parse-corpus-d1.tsv.gz`](base/parse-corpus-d1.tsv.gz) |
| Part D, run 2 | `--dump --order reverse` | 0 | [`parse-corpus-d2.tsv.gz`](base/parse-corpus-d2.tsv.gz) |
| Part D, run 3 | `--dump --threads 2`: each input parsed twice at once, once on each of two threads | 0 | [`parse-corpus-d3.tsv.gz`](base/parse-corpus-d3.tsv.gz) |

- Each report ends with its `#% end` line, so each completed. Part C's run 1 is part C's base report.
- Wall-clock and peak memory are in `runs.tsv` as records. SJL-12 measures nothing.

**Part C's base.**
- **Complete.** `scripts/compliance/compare_parse_corpus.py acceptance`, given the manifest, holds both reports to it:
  its SHA-256, the corpus commit's, and every input and variant it names, at both entry points. Each report holds
  213,926 rows: the manifest's 55,751 inputs give 106,963 input-variants, each answered at P and at K
  ([`base/parse-corpus-c-run1-run2.out`](base/parse-corpus-c-run1-run2.out), each report's header, and
  [`base/logs/parse-corpus-c.stderr.txt`](base/logs/parse-corpus-c.stderr.txt)).
- **Run 1 against run 2.** The comparison finds 0 differences and exits 0
  ([`base/parse-corpus-c-run1-run2.json`](base/parse-corpus-c-run1-run2.json)). The two reports are one file byte for
  byte, `0c007acd...` ([`base/hashes.txt`](base/hashes.txt)).
- **What run 1 answers**, counted from its rows. The counts are read, not judged: the rule decides on differences,
  and totals decide nothing.

  | Entry point | Accepted | Refused | Crashed |
  |---|---|---|---|
  | P, the parser alone | 101,562 | 5,401 | 0 |
  | K, the whole compile | 98,446 | 8,368 | 149 |

  K's 149 crashes are 141 `System.NotImplementedException` and 8 `JSException`s whose thrown value is a
  `ReferenceError`. The rule reads them at the after commit only, where an input that crashed at K at the base and is
  accepted, or refused with a `SyntaxError`, after, is class X. This section classifies nothing.

**Part D's base, read as the rule says.**
- **The pass is absent.** The corpus commit has no for-loop pass, and each of the three reports' headers says so:
  `for-loop-pass absent: Broiler.JavaScript.Compiler declares no type Broiler.JavaScript.Compiler.ForLoopPreparation`.
  Every accepted row's pass column reads `absent`, so each dump is the parser's tree alone.
- **Complete.** Runs 1 and 2 hold 106,963 rows each, and run 3 213,926, one per input and variant on each of its two
  threads. In each run, and on each thread of run 3, P accepts the same 101,562 input-variants that part C's run 1
  accepts at P, and no dump failed.
- **Runs 1 to 3.** `compare_parse_corpus.py determinism`, with part C's run 1 as the population report, takes its
  101,562 input-variants accepted at P as the population. It finds 6,398 whose four parser hashes are not one, and
  exits 1 ([`base/parse-corpus-d.out.gz`](base/parse-corpus-d.out.gz),
  [`base/parse-corpus-d.json.gz`](base/parse-corpus-d.json.gz)). Every difference is in the parser's hash. Read from
  the comparison's JSON: 6,339 of them are test262 input-variants and 59 Broiler.JS ones; 6,349 differ between run 1
  and run 2, and all 6,398 differ between run 3's two threads.
- **This is the difference the rule expects of the base, and it is recorded as it is.** The rule reads part D's base
  so: "The corpus commit has no for-loop pass, so the tool finds no entry point there and part D's base dumps the
  parser's tree alone; it shows the counter's dependence on parse order." At the corpus commit the parser's for-loop
  desugaring names its temporaries from a process-wide counter, so a loop's dump depends on how many loops the same
  process parsed before it ([`tools/parse-corpus/README.md`](tools/parse-corpus/README.md), "The canonical dump").
  Nothing here fixes it and nothing classifies it. Part D admits no difference, and decides at the after commit only,
  over the inputs part C's after run accepts at P.
- The comparison was run without `--require-pass`, because the corpus commit has no pass
  ([`base/commands.txt`](base/commands.txt)). No dump text was written: no run was given `--text`.

**Part C's control** ([`controls/`](controls/)). The rule's control is "a copy of the base report with one outcome
flipped and one message changed, which the comparison must report as two differences; retained with its output".
- `compare_parse_corpus.py control` copied part C's run 1, flipped `T000001 strict P` from accepted to refused, and
  changed the message of `T000741 strict K`, a refusal with a `SyntaxError`
  ([`controls/parse-corpus-c-control.json`](controls/parse-corpus-c-control.json)).
- It reports exactly those two differences, both unclassified, prints "control reported" and exits 0
  ([`controls/parse-corpus-c-control.out`](controls/parse-corpus-c-control.out)). That is the report the rule states.
- **Made twice.** The control was first made into the staged `base/` when the runs were taken. It was made again into
  `controls/` on 2026-10-20, from the retained run 1 alone, with the tool's README's control command run from the
  repository's root, so that its output names the paths the bundle commits ([`base/commands.txt`](base/commands.txt)).
  The copy is byte for byte the first one, `c8c56b4d...`, its standard output is byte for byte the first, and its JSON
  differs from the first only in the path it names for the copy.

**Retained and checked.** In each of `base/`, `base/part-a/`, `base/part-a-void-attempt-1/` to
`base/part-a-void-attempt-4/`, `controls/` and `corpus/`, `sha256sum -c hashes.txt`, run in the folder, passes.
*(Revised 2026-10-25, at SJL-12.5. The list read "`base/`, `base/part-a-void-attempt-1/`,
`base/part-a-void-attempt-2/`, `controls/` and `corpus/`".)*

## 2. Readings named before the after runs

**Written 2026-10-25, at SJL-12.5's records, before any run at the after commit is taken.** This section names how
this README will read the rule where a change SJL-12.5 merged raises a question the rule's words answer only when
read against it. It revises no rule: [`decision-rule.md`](decision-rule.md) is unchanged, and where a reading here
and the rule differ, the rule decides. It judges nothing, and it holds no figure.

**Part C's class D** *(named 2026-10-25)*. The rule admits as class D "an input refused at entry point P at the base
by the parser's for-loop desugaring itself (AssignTempNames in
Broiler.JS/Broiler.JavaScript.Parser/FastParser.ForStatement.cs at the rule commit, whose message is "Unknown
token"), accepted at P after, and refused at K after with the exception type and message it had at K at the base;
each listed with its outcomes at both entry points".
- **Its population is empty at the base.** Part C's base report,
  [`base/parse-corpus-c.tsv.gz`](base/parse-corpus-c.tsv.gz) (run 1, whose SHA-256, `0c007acd...`,
  [`base/hashes.txt`](base/hashes.txt) records), holds no row whose message is "Unknown token", at P or at K, and
  run 2 is the same file byte for byte. That message is the refusal of `AssignTempNames`, its two
  `throw new FastParseException(e.Start, $"Unknown token")` at the rule commit and at the corpus commit
  (`FastParser.ForStatement.cs:772` and `:819`), which item 5 moved into the compiler's pass
  (`Broiler.JS/Broiler.JavaScript.Compiler/ForLoopPreparation.cs:629` and `:676` at the after commit). So no input
  of the corpus is in class D's population, and no difference of the after run can be admitted as class D.
- **A K refusal that differs from the base's is outside D.** Item 5's changelog entry, at the after commit, says
  that the compile refuses such a binding "at the same token with the same message, when nothing later in the
  source fails first": the parser now parses the whole program before the pass runs, so a parse error later in the
  source, or an early error the parser raises after the loop, is the one reported. A row whose K refusal after
  differs from the base's because a later failure now comes first fails D's last condition, the exception type and
  message it had at K at the base, and so is not in D. Class M admits a refusal of the same type whose message
  differs only where `Broiler.JS/CHANGELOG.md` at the after commit lists that change by its old and new text, and
  item 5's entry there, the one that describes the reordering, lists no message by its old and new text. So this
  README will report such a row as a regression, not admit it, unless another entry of the after commit's changelog
  lists that row's own change by its old and new text, in which case the rule admits it as M and this README will
  name the entry.
- **Class M's changelog is the after commit's.** The rule reads class M's listing in `Broiler.JS/CHANGELOG.md` "at
  the after commit", so what SJL-12.5's records add to the changelog after that commit admits no row as class M.

## 3. The after runs (2026-10-26)

**Written 2026-10-26, at SJL-12's after evidence (work package 7.6), after every after run was taken.** This section
reads what the bundle retains of the after runs and how they were imported. Sections 4 to 7 judge each part against
the rule, and section 8 reads this repository's clauses from them.

| Part | Commit | Taken | Retained in |
|---|---|---|---|
| A | The after commit, `c29d653f5f93b66b5e0efdbe506205a5347fc18a` | 2026-10-25, 20:39Z to 21:43Z, by `test262 evidence`, run [40231802611](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/40231802611) | [`after/part-a/`](after/part-a/imported.txt); the comparison in [`after/part-a/comparison/`](after/part-a/comparison/comparison.log) |
| B | The after commit, and the rule commit's parser assembly | 2026-10-26, 00:56Z to 01:08Z, on the programme's local machine | [`after/part-b-architecture-test-after.log`](after/part-b-architecture-test-after.log), [`after/part-b-architecture-test-rule.log`](after/part-b-architecture-test-rule.log); control K in [`controls/`](controls/part-b-k.log) |
| C | The after commit | 2026-10-26, the tool's build from 01:09Z and the run from 01:14Z | [`after/parse-corpus-c.tsv.gz`](after/parse-corpus-c.tsv.gz), compared in [`after/parse-corpus-c.out`](after/parse-corpus-c.out) |
| D | The after commit | 2026-10-26, runs 1 to 3 from 01:18Z, controls K1 and K2 from 01:24Z to 01:57Z | [`after/parse-corpus-d1.tsv.gz`](after/parse-corpus-d1.tsv.gz) to `d3`, compared in [`after/parse-corpus-d.out`](after/parse-corpus-d.out); controls K1 and K2 in [`controls/`](controls/part-d-k2.out) |

**The after commit** is `c29d653f5f93b66b5e0efdbe506205a5347fc18a`, the default-branch merge of
Broiler-Platform/Broiler.JS#1068. It lands SJL-12.5's item 5, the compiler's for-loop pass, the last of SJL-12.5's
items in JSD-0080's order (decision D26), as the rule's "after commit" says. Two merges followed it on the default
branch before the after runs were read: SJL-11.1's records, Broiler-Platform/Broiler.JS#1069 (`f918b040`), and
SJL-12.5's records, #1070 (`a6c68dd4`). Against the after commit they change files under `docs/`,
`Broiler.ECMAScript/docs/` and `Broiler.JS/CHANGELOG.md` alone, and so no path the corpus commit's third condition
lists ([`after/logs/corpus-commit-at-after.txt`](after/logs/corpus-commit-at-after.txt)).

**The corpus commit's three conditions, read again at the after commit**, as section 1 says they would be
([`after/logs/corpus-commit-at-after.txt`](after/logs/corpus-commit-at-after.txt), from git alone). All three hold.
- **It descends from the rule commit**, and the after commit descends from it.
- **It names only the listed paths.** The same paths, grouped under the same listed paths, as
  [`corpus/corpus-commit.txt`](corpus/corpus-commit.txt) records.
- **It comes before every product change.** Every commit between the rule commit and the after commit that changes a
  listed path descends from the corpus commit. The one exception is `b1ac1b8a`, the merge section 1 reads, which is
  an ancestor of the corpus commit and changes no listed path against the rule commit. This holds without
  `--full-history` and with it.
- **A correction to `corpus/corpus-commit.txt`'s reading, which the bundle keeps as it was committed.** The pathspec
  that record used for the first listed path, `Broiler.JS/Broiler.JavaScript.*/`, matches no file, because of its
  trailing slash. Git matches a wildcard pathspec against each file's path, and a file's path never ends in a slash.
  So that record's reading of condition 3 looked at `Broiler.ECMAScript/` alone. The reading at the after commit
  shows it: that pathspec names no commit between the rule commit and the after commit, though twelve change the
  parser. `after/logs/corpus-commit-at-after.txt` reads the condition with `Broiler.JS/Broiler.JavaScript.*`, which
  matches every file under each `Broiler.JS/Broiler.JavaScript.<name>/`. Between the rule commit and the corpus commit
  it finds what that record found, `b1ac1b8a` alone, so the record's conclusion holds.

**Part A's after run.** It was taken by `.github/workflows/test262-evidence.yml`, run
[40231802611](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/40231802611), attempt 1. It was dispatched
on 2026-10-25 at 20:39Z from `main` at `a6c68dd4`, with the after commit named by all forty digits and the base's
other inputs, and it completed at 21:43Z. Its conclusion is success, and so is every one of its ten jobs, `prepare`,
the eight shards and `collect`, the step "Require a complete run" among them (GitHub's record of the run and its jobs,
read 2026-10-26).
- **Imported unchanged** into [`after/part-a/`](after/part-a/imported.txt): every file of the artifact
  `test262-evidence` (id 12614713183), 26 entries, each compared with its entry in the zip, none differing. The zip's
  SHA-256 is `c9801960…`, the digest GitHub records. [`imported.txt`](after/part-a/imported.txt) records the run, the
  artifact and the digest. [`runner-images.txt`](after/part-a/runner-images.txt) copies the head of each job's own
  log. [`hashes.txt`](after/part-a/hashes.txt) holds the SHA-256 of every other file of the folder, with
  `comparison/`. As at the base, the bundle keeps `test262-evidence` alone of the run's 18 artifacts.
- **What ran.** The dispatch ref `a6c68dd4` holds `.github/workflows/test262-evidence.yml` as the rule commit's own
  blob, `7158fb2a`, and changes nothing under `.github/` or `scripts/` since `792ff56e`, the base's dispatch ref. Both
  of the workflow's checkouts are of the tested commit. At the after commit, `.github/` is the rule commit's, and
  `scripts/compliance/run_test262.py`, `merge_test262_shards.py` and `compare_test262_paths.py` are the rule commit's
  blobs. So the workflow, the shard action and the scripts that ran are the rule commit's, as at the base.
- **Its runners.** Every shard's job and job `collect` ran on the image `ubuntu-26.04`, version 20261012.163.1, as the
  head of each job's own log reads it. The base's ran on the same image. `run.txt`'s runner line reads `ubuntu26`
  20261023.163.3, as the base's does. The .NET SDK is 10.0.403, where the base's was 10.0.402. The rule names neither
  an image nor an SDK. It judges the runners by their shells' lists and their host-zone records, and the comparison
  script records the SDK as a field it does not compare
  ([`comparison.json`](after/part-a/comparison/comparison.json), `uncomparedFields`).
- **The host-zone record is the base's, line for line and byte for byte**
  ([`after/part-a/host-zone.log`](after/part-a/host-zone.log),
  [`comparison/zone-records.log`](after/part-a/comparison/zone-records.log)). `TZ` is unset, UTC +0000 at both
  instants and tzdata 2026c, for every shard and for the merging job. So the runners' tzdata did not move between the
  base and the after run, and the rule's clause for a moved record does not apply. Part A is judged (section 4).

**The local after runs of parts B, C and D** were taken by WP7.5 on the programme's local machine, in order: part B,
then part C, then part D with its controls. Each was taken once, and none was taken again. Their records are
[`after/commands.txt`](after/commands.txt), [`after/machine.txt`](after/machine.txt) and
[`after/logs/`](after/logs/runs.tsv).
- **The machine is section 1's**: four processors, .NET SDK 10.0.401 with runtime 10.0.12, Ubuntu 24.04.5 LTS,
  `linux-x64`. Its kernel string has moved since the base, and `machine.txt` records it.
- **The idle machine.** Before each build and each run, WP4.7's gate required a 1-minute load average of at most 1.0
  and no `dotnet` or `python` process, and it recorded what it read ([`logs/gate.log`](after/logs/gate.log),
  [`logs/pre-run.txt`](after/logs/pre-run.txt)). Four gates waited one poll, each for the load alone, each after one
  of this work's own builds. Every other gate went ahead at its first look. Broiler.VM's after runs had ended before
  these began, and no other build, test or run was taken on the machine while they ran.
- **The checkouts.** Three clean detached worktrees were used: the after commit, the rule commit, and a scratch copy of
  the after commit for the controls. Each records `git rev-parse HEAD`, `git status --porcelain` and
  `--porcelain --ignored` as made, before and after each build, and after the runs, as section 1 asked of the after
  runs ([`logs/checkout-*.txt`](after/logs/checkout-after-commit-made.txt),
  [`controls/logs/checkout-k-*.txt`](controls/logs/checkout-k-made.txt)). Each is empty, except that the scratch
  copy's records taken with a control applied show that control's one changed file.
- **The tool** is the corpus commit's, the same blob at the after commit. It was built in Release from clean at the
  after commit, against that commit's own projects, with no error and the base's 56 distinct warnings, none of them
  the tool's ([`logs/build.log`](after/logs/build.log)). Every after report of parts C and D names the after commit's
  five product assemblies, with their informational version `0.1.0-preview.1+c29d653f…` and their module version
  ids, and the manifest's SHA-256, `1c534210…`, the corpus commit's. Every header reads the rule's environment:
  `TZ=UTC`; `LANG`, every `LC_*` and every `BROILER_*` variable unset; the invariant culture; threads of 256 MB.
- **Where WP7.5 followed the rule past its instructions.** Its instructions and the rule differed in three places,
  and it followed the rule each time:
  - **After control K's checked revert,** the scratch copy was built and tested once more. The rule says K "passes
    after the revert". The instructions listed three test runs.
  - **After K1's and K2's reverts,** the copy's tool was built again and runs 1 to 3 taken again. The rule's MET
    clause asks that "both controls differ as stated and agree again after their revert". The instructions asked only
    for each revert's check.
  - **Part B's second run** names the rule commit's `Broiler.JavaScript.Parser.dll` in the Release output of that
    commit's `Broiler.JavaScript.Parser.Tests`. The test refuses a parser folder without the `Broiler.*` assemblies the
    parser loads, and neither the rule nor the instructions name a build output.

**What the import changed** ([`after/logs/import.txt`](after/logs/import.txt), which lists every change with each
file's SHA-256 before and after it). A read-only check of WP7.5's outputs found four things to fix in the files and one
record to add. Each was done here, and nothing was run again but one read-only comparison.
- **Part C's comparison's standard error was lost.** WP7.5 wrote it to `logs/parse-corpus-c.stderr.txt`, and then
  wrote the tool's run's standard error over it under the same name. The comparison was run again at the import, the
  same script on the same retained files. Its standard output and JSON are WP7.5's byte for byte, and its standard
  error, which is empty, is now
  [`logs/parse-corpus-c-comparison.stderr.txt`](after/logs/parse-corpus-c-comparison.stderr.txt)
  ([`logs/parse-corpus-c-comparison-again.txt`](after/logs/parse-corpus-c-comparison-again.txt)).
- **The control comparisons' standard error** is now in [`controls/logs/`](controls/logs/), where the controls are.
- **The three `git worktree add` records** keep, of git's progress line, the last segment alone, as a terminal shows
  it. Bundle `sjl-12-001`'s `after/ordering/` in Broiler.VM trimmed its log the same way. `import.txt` records each
  untrimmed file's SHA-256.
- **The scratch folder's path** is written `<scratch>` in the retained logs and commands, as bundles `sjl-8-001` and
  `sjl-10-001` write it. `import.txt` records each file's SHA-256 before and after the change. No report and no gzip
  file named the path.
- **The corpus commit's third condition** is read again at the after commit, which WP7.5's
  [`logs/after-commit.txt`](after/logs/after-commit.txt) did not do (above).
- **What is not retained.** WP7.5's runner report is not retained, because its facts are in `commands.txt`,
  `machine.txt` and `logs/`, and this section reads it. `commands.txt` ends with a dated note on each of these changes.

**Retained and checked.** In each of `after/`, `after/part-a/` and `controls/`, `sha256sum -c hashes.txt`, run in the
folder, passes. `after/hashes.txt` covers `after/` and `after/logs/`; `after/part-a/hashes.txt` covers
`after/part-a/`, with `shards/` and `comparison/`; and `controls/hashes.txt` covers `controls/` and `controls/logs/`,
part C's control included, whose three lines are unchanged. No file the bundle held before is changed but this
README and `controls/hashes.txt`.

## 4. Part A - test262 per path, in UTC, with negative tests: MET

**MET under part A of the rule as written: no row changed.** The after run is compared with part A's base of
section 1 through `scripts/compliance/compare_test262_paths.py` as SJL-0.9 committed it (`1410079e`, the only commit
that touches it; the blob `e951dd47` at the rule commit, at the after commit and on `main`). Each run's timed-out rows
are replaced by their re-runs, and the classification file the rule asks for is retained beside the report,
[`classifications.json`](after/part-a/comparison/classifications.json). **Every row, each path in both variants,
gives its base's verdict: 0 changed, 0 regressions, 0 unclassified, exit 0.** The classification file is empty,
`{}`, because no verdict changed, and the script reports no classification unused
([`comparison.log`](after/part-a/comparison/comparison.log),
[`comparison.json`](after/part-a/comparison/comparison.json)).

| Part A's condition | This comparison |
|---|---|
| Both runs complete: every expected shard reported | Eight of eight expected shards reported in each run, none incomplete and none retried. Every shard's status file reads phase `full` and exit code 1, for failing tests, in both runs ([`completeness.log`](after/part-a/comparison/completeness.log), read from each run's own files; [`after/part-a/merge.log`](after/part-a/merge.log)) |
| No configuration failure | None in either run |
| One suite revision | `ccaac100` in each run, the revision the rule pins |
| One shell | In each run, every shard and the re-run hash the shell's binaries alike: one distinct list, `25fd801b…` after and `433ab0c7…` at the base ([`after/part-a/shell-hashes.log`](after/part-a/shell-hashes.log)). The two runs' shells differ, as they must, since the after run's is built from the after commit |
| Every shard in the zone | In each run, all eight shards in the runner's zone, UTC ([`after/part-a/host-zone.log`](after/part-a/host-zone.log)) |
| `includeNegative` true in both | In `run.txt`, in the merged results and in the re-run, in each run |
| The two runs' host-zone records read alike, line for line | Yes, and byte for byte ([`zone-records.log`](after/part-a/comparison/zone-records.log)): `TZ` unset, UTC +0000 at both instants and tzdata 2026c in both |
| The script finds them comparable | Yes, exit 0. Its checks ([`compare-checks.log`](after/part-a/comparison/compare-checks.log)) show two things. The after run compared with itself changes nothing. A copy with one passing row turned to failed is reported as one regression, with exit 1. The fields the script records and does not compare are the same in both runs, but the .NET SDK (section 3) |
| No row passing at the base fails, times out or is skipped after | None |
| Every changed verdict in C1, and listed | **Met**: no verdict changed, so C1 lists no row (the table below) |

**Timeouts and their re-runs.** As at the base, one path timed out in both variants,
`test/staging/sm/regress/regress-1507322-deep-weakmap.js`
([`after/part-a/timed-out-paths.txt`](after/part-a/timed-out-paths.txt), the base's byte for byte). The workflow ran
it again alone, with one worker, at the same commit and with the same shell, and it timed out again in both variants
([`after/part-a/rerun-timeouts.log`](after/part-a/rerun-timeouts.log),
[`rerun-timeouts.json`](after/part-a/rerun-timeouts.json)). The script took each run's re-run, through `--base-rerun`
and `--after-rerun`, and replaced the two rows on each side. So they compare timed out against timed out (the report's
`replacedByRerun`). Both the runs and the re-runs are retained.

**The C1 table.** Each row that changed under class C1, with its path, its variant, its kind and its correction:

| Path | Variant | Base | After | Kind | Correction |
|---|---|---|---|---|---|

It has no row, because no verdict changed. So no row is a finding against JSD-0080.

**What it says.** Corrections J1 and J2, and the compile crashes the rule admits as C1's kind (b), move no verdict of
the suite at this revision. Either no test262 case reaches them, or the cases that do already gave the same verdict
at the base. No test262 suite total is read here: totals decide nothing.

The retained logs and report name this repository's checkout as `<repo>` and the scratch folder as `<scratch>`. The
comparison and its checks were run twice at the import, at 05:31Z and at 05:34Z on 2026-10-26, the second time only
so that the retained logs name the scratch folder as the other records do. Both printed the same lines, and the
second run's files are the ones retained.

## 5. Part B - the parser's architecture test and its witness: MET

**MET under part B of the rule as written.** Each condition of the rule's MET clause, with the files it reads:

| Part B's condition | This run |
|---|---|
| The test passes over the after commit's assembly | Run 1, `dotnet test` of `Broiler.JavaScript.Architecture.Tests` in Release at the after commit, over that commit's `Broiler.JavaScript.Parser` assembly: "Total tests: 15, Passed: 15", exit 0 ([`after/part-b-architecture-test-after.log`](after/part-b-architecture-test-after.log)). The fixtures print two parses of one source with equal dumps, and 80,000 concurrent parses on four threads, none ending on another parse's token. Non-vacuity accepts four `InitOnly` static fields it reads, among them `FastScanner.NoBigIntWidth` |
| It fails over the rule commit's, naming each violation the rule lists | Run 2, the same test with `BROILER_ARCH_PARSER_ASSEMBLY` naming the rule commit's Release `Broiler.JavaScript.Parser.dll`: "Passed: 14, Failed: 1", exit 1 ([`after/part-b-architecture-test-rule.log`](after/part-b-architecture-test-rule.log)). The failure names the static field `FastParser.TempVarID`; the `InitOnly` static fields `FastScanner.EOF` and `FastScanner.EmptyToken`, of the mutable type `FastToken`, whose fields `Next` and `Previous` are mutable; the reference to the assembly `Broiler.JavaScript.Runtime`; and each of the three walkers by its TypeRef, `AstIdentifierReplacer`, `IdentifierNameCollector` and `ClosureDetector`. These are every violation the rule lists. It also names the three fixtures, each failing at the rule commit |
| It refuses every witness shape | In both runs, the witness compiled in memory is refused for each of the rule's ten shapes. That is twelve cases, since the walker shape has one per walker |
| Control K fails, and then passes as stated | Run 3, with control K applied to a scratch copy of the after commit ([`controls/part-b-k.patch`](controls/part-b-k.patch): a static counter, `FastParser.ControlKParsers`, incremented by each parser made): "Passed: 14, Failed: 1", exit 1, naming that field alone as a mutable static field ([`controls/part-b-k.log`](controls/part-b-k.log)). The revert is checked: `git diff --exit-code` exits 0 against the copy's HEAD and against the after commit, and `git status` is empty ([`controls/part-b-k-revert-check.txt`](controls/part-b-k-revert-check.txt)). The reverted copy, built and tested again, passes 15 of 15 ([`controls/part-b-k-reverted.log`](controls/part-b-k-reverted.log)) |

The builds are in [`after/logs/`](after/logs/build-architecture-tests.log) and
[`controls/logs/`](controls/logs/build-part-b-k.log), and the assemblies they wrote are hashed in
[`after/binaries.sha256`](after/binaries.sha256) and [`controls/binaries.sha256`](controls/binaries.sha256).

**What it says.** Read over the Parser assembly's own declarations and references (JSD-0080, decision D24), at the
after commit the parser holds no mutable static and no static of a mutable type the clauses forbid, names neither
`AsyncLocal` nor `ThreadLocal` and has no `[ThreadStatic]`, references no `Runtime` assembly, and names none of the
three desugaring walkers. Its for-loop parses to the declaration as written. The test that holds this refuses each
shape of its witness and the control. "References no `Runtime` type" is read by assembly, as part B's clause reads
it. `Expressions`, which the parser references, still looks up `Runtime`'s `JSValue` by name, unchanged since before
SJL-12 ([`SharedLibraries.md`](../../roadmap/SharedLibraries.md), SJL-12.5's item 4).

## 6. Part C - the parse corpus's acceptance: MET

**MET under part C of the rule as written: no difference.** `scripts/compliance/compare_parse_corpus.py acceptance`,
as the corpus commit holds it (blob `c0685c65`, the same at the after commit), compares the base report of section 1,
[`base/parse-corpus-c.tsv.gz`](base/parse-corpus-c.tsv.gz), with the after report,
[`after/parse-corpus-c.tsv.gz`](after/parse-corpus-c.tsv.gz). It compares each input, variant and entry point by
outcome, exception type and message. It was given the manifest and the after commit's changelog, and no
classification file. It prints "differences 0; admitted 0; not admitted 0" and exits 0
([`after/parse-corpus-c.out`](after/parse-corpus-c.out), [`after/parse-corpus-c.json`](after/parse-corpus-c.json);
its standard error, empty, in
[`after/logs/parse-corpus-c-comparison.stderr.txt`](after/logs/parse-corpus-c-comparison.stderr.txt)).

| Part C's condition | This run |
|---|---|
| Both runs complete | The after report holds a row for every input and variant at P and at K, in the manifest's order, with no duplicate, and ends with its `#% end` line ([`after/logs/complete-parse-corpus-c.txt`](after/logs/complete-parse-corpus-c.txt)). The base's is complete (section 1). The script holds both reports to the manifest |
| The manifest's digest equal to the corpus commit's | `1c534210…` in both reports' headers and in the comparison's output |
| The control is reported | Part C's control, made from the base report at the base and retained in [`controls/`](controls/parse-corpus-c-control.out), reports exactly its two differences (section 1). It was not made again |
| Every difference in M, X or D, and listed | **Met**: there is no difference, so the table below has no row |

**The M, X and D table.** Each difference in class M, X or D, with its input, variant and entry point, and its
outcomes at both commits:

| Input | Variant | Entry point | Base | After | Class | Changelog entry |
|---|---|---|---|---|---|---|

It has no row.
- **Class D's population is empty**, as section 2 read it before the after run: the base report holds no "Unknown
  token" refusal. No row of the after run needed the reading.
- **No row is a K refusal whose message moved** because a later failure now comes first, as section 2 described. So
  no row is reported as a regression on that reading.
- **Class X admits no row.** The inputs that crashed at K at the base crash at K after, with the same exception
  types: no difference, not a resolved crash. Section 1's count of them is the after report's too
  ([`after/logs/complete-parse-corpus-c.txt`](after/logs/complete-parse-corpus-c.txt)).
- **Class M admits no row**, so no changelog entry is read for one.

## 7. Part D - determinism of the parser and its compiler pass: MET

**MET under part D of the rule as written.** `compare_parse_corpus.py determinism --require-pass` takes part C's after
report as the population report, so its population is the 101,562 input-variants part C's after run accepts at P.
It compares, for each, the four hashes of each dump: run 1 in the manifest's order, run 2 in the reverse order, and
run 3's two threads. It prints "population 101562; differences 0" and exits 0
([`after/parse-corpus-d.out`](after/parse-corpus-d.out), [`after/parse-corpus-d.json`](after/parse-corpus-d.json)).

| Part D's condition | This run |
|---|---|
| The tool finds the pass's entry point in each of the three runs | Each report's header and the comparison read "for-loop-pass found: `Broiler.JavaScript.Compiler.ForLoopPreparation.Prepare(Broiler.JavaScript.Ast.AstProgram, System.Int32) : Broiler.JavaScript.Ast.AstProgram`", the entry point the rule names. Every accepted row's pass column reads `prepared`, and no pass threw ([`after/logs/complete-parse-corpus-d1.txt`](after/logs/complete-parse-corpus-d1.txt) to `d3`) |
| Every input gives one hash per dump in all four | **Yes**: 0 of the population differ, in the parser's dump or in the pass's |
| Control K1 differs as stated | [`controls/part-d-k1.patch`](controls/part-d-k1.patch) numbers the pass's temporaries from a process-wide counter. Run 2 differs from run 1: the script, with `--expect-differ 1:2`, finds 6,349 input-variants that differ between them and exits 0 ([`controls/part-d-k1.out.gz`](controls/part-d-k1.out.gz), [`.json.gz`](controls/part-d-k1.json.gz)). Every difference is in the pass's hash |
| Control K2 differs as stated | [`controls/part-d-k2.patch`](controls/part-d-k2.patch) orders the pass's per-iteration re-bindings and copy-backs, a list in the dumped tree, by each name's `string.GetHashCode()`. Run 1 differs from run 2: 56 input-variants, exit 0 ([`controls/part-d-k2.out`](controls/part-d-k2.out), [`.json`](controls/part-d-k2.json)). Every difference is in the pass's hash |
| Both agree again after their revert | Each revert is checked: `git diff --exit-code` exits 0 against the copy's HEAD and against the after commit, and `git status` is empty ([`controls/part-d-k1-revert-check.txt`](controls/part-d-k1-revert-check.txt), [`part-d-k2-revert-check.txt`](controls/part-d-k2-revert-check.txt)). The reverted copy's tool, built again, gives runs 1 to 3 with 0 differences under `--require-pass` ([`controls/part-d-k-reverted.out`](controls/part-d-k-reverted.out)). Each run's rows, without the `#%` header lines, have the SHA-256 of the after commit's run of the same number ([`controls/part-d-k-reverted-rows-against-after.txt`](controls/part-d-k-reverted-rows-against-after.txt)) |

**What it says.** At the after commit the parser's dump no longer depends on how many loops the same process parsed
before it, which section 1 recorded of the base. The pass's temporaries are numbered per pass (JSD-0080, decision
D22). A process-wide counter in them, and a hash order in the tree, are each detected, and each revert restores the
after commit's dumps exactly.

## 8. This repository's clauses, read from the records

**Written 2026-10-26, at SJL-12's after evidence.** The milestone's Broiler.JS clauses, and this repository's half of
its shared clauses, as the records now hold them. None of it is accepted: one person holds every role (EX-30), and no
second holder has read it. **SJL-12 may not meet its gate before SJL-3 meets SJL-3's.** Broiler.VM's clauses 4 to 6,
and its half of shared clauses 7 and 8, are judged in its bundle of the same name and read by the programme's ledger.

| Clause | Where | State on the records |
|---|---|---|
| 1. The parser reads no ambient state, holds no mutable static, references no `Runtime` type, and does not desugar. A Broiler.JS architecture test enforces this, with a witness | Part B (section 5), with part D (section 7) for the parse's determinism | **Met on the records**: part B is MET. The clause is read as the milestone's dated notes read it: "does not desugar" is SJL-12.5's for-loop desugaring (JSD-0080, decision D23), and "reads no ambient state" is read over the Parser assembly's own declarations and references (decision D24) |
| 2. test262 per path meets the predeclared rule | Part A (section 4) | **Met on the records**: part A is MET |
| 3. The contract record is in Broiler.JS's plan | [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "The frontend contract (SJL-12)" | **Met**, as recorded at SJL-12.1; the rule does not decide it |
| 7, this repository's half: the early-error catalogue is retained | [`Broiler.ECMAScript/docs/frontend/early-errors.tsv`](../../../Broiler.ECMAScript/docs/frontend/early-errors.tsv), with [`catalogue/`](catalogue/completeness.md) | **Retained**, since SJL-12.3; the rule does not decide it |
| 8, this repository's half: the parse corpus and its baselines are retained | [`conformance/parse/`](../../../conformance/parse/README.md); the base reports in [`base/`](base/) and the after reports in [`after/`](after/) | **Retained**; parts C and D (sections 6 and 7) read them |

## 9. Where the records disagree

**Written 2026-10-26, at SJL-12's after evidence.** None of these changes a part's reading.
- **`corpus/corpus-commit.txt`'s pathspec** for `Broiler.JS/Broiler.JavaScript.*/` matches no file, so its reading
  of condition 3 looked at `Broiler.ECMAScript/` alone. Read with a pathspec that matches, the condition holds at the
  corpus commit and at the after commit (section 3). That record is kept as committed.
- **The runner image.** The instructions for this reading asked that a differing runner image be treated under part
  A's NOT MET clause. The rule names no image: its clause is on the host-zone records. Both runs ran on the same
  image, so the instruction has no consequence here. The .NET SDK differs, 10.0.402 at the base and 10.0.403 after,
  and the rule names no SDK.
- **WP7.5's instructions and the rule** differed on controls K, K1 and K2, and neither named the build output for the
  rule commit's parser. WP7.5 followed the rule (section 3).
- **Class X's changelog.** The rule says which commit's changelog class M reads, the after commit's, and does not say
  it for class X. No row needed either.
- **JSD-0080's decision D21 lists corrections J1 and J2**, and the rule admits them in part A as class C1, with
  resolved compile crashes; part A shows no changed verdict. That is not a disagreement: the rule admits such rows,
  and does not require that there be any.
