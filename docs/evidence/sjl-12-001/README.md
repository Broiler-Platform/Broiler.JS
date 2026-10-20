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

**State, 2026-10-20, at SJL-12.4: the rule, and parts C and D's bases. Nothing here is a verdict.**
- Part A's base is not yet taken. Two dispatches of `test262 evidence` at the rule commit, with `include_negative`
  true, are void; they are retained and named, not judged (section 1). Part B's base is the rule commit's parser
  assembly, built when part B runs.
- Parts C and D's bases are taken at the corpus commit `1fe542ff`, with part C's control (section 1).
- The after runs follow SJL-12.5's last item, the compiler's for-loop pass.

*(Revised 2026-10-20, at SJL-12.4, when parts C and D's bases were committed. The state read "at SJL-12.5: rules only.
Nothing here is a verdict, and no base has been taken.", and its second item "Parts C and D's bases follow SJL-12.4,
which adds the parse corpus and the tool.")*

*(Revised 2026-10-20, at SJL-12.5, when two dispatches of part A's base had ended void. The state read "at SJL-12.1:
rules only. Nothing here is a verdict, and nothing has been taken.", and its first item "Part A's base is taken at the
rule's commit by `test262 evidence`, with `include_negative` true. Part B's base is the rule commit's parser
assembly, built when part B runs.")*

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

**It holds no figure until a run is taken**, and then only in the run's own files and this README's reading of them.

The owner's direction of 2026-10-18, "Please do all recommended decisions and skip waiting for review.", under which
JSD-0080 records its decisions, changes no rule: nothing is accepted without review, and SJL-12 may not meet its gate
before SJL-3 meets SJL-3's.

Broiler.VM judges its own half in its own bundle of the same name. This bundle reads none of its runs.

## 1. The bases (2026-10-20)

**Written 2026-10-20.** This section reads what this bundle retains of the bases. It judges nothing: the rule compares
each base with a run at the after commit, which does not exist yet.

| Part | Commit | Taken | Retained in |
|---|---|---|---|
| A | The rule commit, `4d813f2ac863895b9be5c4619b7886c9ddd71077` | Not yet taken. Two attempts, on 2026-10-19 and 2026-10-20, are void | [`base/part-a-void-attempt-1/`](base/part-a-void-attempt-1/imported.txt), [`base/part-a-void-attempt-2/`](base/part-a-void-attempt-2/imported.txt) |
| C and D | The corpus commit, `1fe542ff459935586cdf495e4a0e3d798248948a` | 2026-10-20, 12:38Z to 12:57Z, on the programme's local machine | [`base/`](base/) |
| C's control | Made from part C's base report alone | 2026-10-20 | [`controls/`](controls/) |

Part B's base, the rule commit's parser assembly, is built when part B runs, and is not in this bundle.

### Part A's base, at the rule commit

**Part A's base is not yet taken.** Two dispatches of `test262 evidence` at the rule commit are void. Each is
retained, unchanged, in its own folder named as void, and neither is the base.

**The rule commit** is `4d813f2ac863895b9be5c4619b7886c9ddd71077`, the default-branch merge of Broiler.JS#1059, which
carries [`decision-rule.md`](decision-rule.md) as it reads, with the corrections its section "Revisions" lists. The
parse corpus's README names it as the rule commit ([`conformance/parse/README.md`](../../../conformance/parse/README.md)).

| Attempt | Workflow run | Dispatched, completed | Artifact `test262-evidence`, and the SHA-256 of its zip | Retained in |
|---|---|---|---|---|
| 1, void | [39335653429](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/39335653429) | 2026-10-19 22:32Z, 23:40Z | id 12229603117, `22023f0310850eda5d7ddfff54dad41a1e3c1fb7b4aeb4ea24f30b28018a9241` | [`base/part-a-void-attempt-1/`](base/part-a-void-attempt-1/imported.txt) |
| 2, void | [39379425075](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/39379425075) | 2026-10-20 06:16Z, 07:23Z | id 12246874482, `25d837f27a47552b3e718481b794e52d05896289e204a14dcf7430fcdeb416fa` | [`base/part-a-void-attempt-2/`](base/part-a-void-attempt-2/imported.txt) |

- **What ran.** Each was dispatched from `main` when `main` was the rule commit itself, and tested it: `run.txt` reads
  `dispatched ref: refs/heads/main at 4d813f2ac863895b9be5c4619b7886c9ddd71077` and the same tested commit. So the
  workflow and `scripts/compliance/` that ran are the rule commit's. Each ran with suite `ccaac100`, four workers, a
  timeout of 30 s, timeouts run again, `time_zone` empty and negative-metadata tests included (`run.txt`'s inputs, and
  `includeNegative: True`).
- **Why each is void.** Each run's conclusion is failure. Its job `collect` failed at the workflow's step "Require a
  complete run", which wrote "The shards did not all run the same shell (shell-hashes.log)." and "The shards did not
  all run in the host time zone (host-zone.log)." and no other failure (GitHub's record of each run and that job's
  log, read 2026-10-20). The two records the step names are retained in each folder:
  - **Attempt 1.** [`shell-hashes.log`](base/part-a-void-attempt-1/shell-hashes.log) reads "shards: 8; distinct
    lists: 2": shard 4's list, `a6515ae3...`, against the other seven's, `433ab0c7...`.
    [`host-zone.log`](base/part-a-void-attempt-1/host-zone.log) reads "shards: 8; in the expected zone: 7": shard 4
    reads tzdata 2026e, against the merging job's 2026c.
  - **Attempt 2.** [`shell-hashes.log`](base/part-a-void-attempt-2/shell-hashes.log) reads "shards: 8; distinct
    lists: 2": shards 0, 4, 6 and 7, `433ab0c7...`, against shards 1, 2, 3 and 5, `a6515ae3...`; and "the re-run's
    shell does NOT hash as the shards' did". [`host-zone.log`](base/part-a-void-attempt-2/host-zone.log) reads
    "shards: 8; in the expected zone: 4": shards 0, 4, 6 and 7 read tzdata 2026c, against the merging job's 2026e.
  - In both attempts every shard reads `TZ` unset and UTC +0000 at both instants, and the shards fall into the same
    two groups by their shell's list as by their tzdata release. `run.txt` records the merging job's runner image, `ubuntu24`
    20261012.334.1 in attempt 1 and 20261018.339.1 in attempt 2; the artifact records no shard's image.
- **Named, not judged.** Neither attempt is part A's base, and nothing here reads their rows: this section quotes no
  total and compares nothing with them.
- **Retained unchanged.** Each folder holds every file of its artifact, 26 entries, each compared with its entry in
  the zip, none differing. Beside them are `imported.txt`, which records the run, the artifact's id and the digest of
  the zip as downloaded, which is the digest GitHub records, and why the attempt is void; and `hashes.txt`, the
  SHA-256 of every other file of the folder. `sha256sum -c hashes.txt`, run in each folder, passes.

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
  from the rule commit over the paths the condition lists names no commit. With `--full-history` it names
  `b1ac1b8a` alone, a merge that took the rule commit into the catalogue's branch and changes no listed path against
  the rule commit; it is itself an ancestor of the corpus commit. The condition is read again at the after commit.

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

**Retained and checked.** In each of `base/`, `base/part-a-void-attempt-1/`, `base/part-a-void-attempt-2/`,
`controls/` and `corpus/`, `sha256sum -c hashes.txt`, run in the folder, passes.
