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

**State, 2026-10-20, at SJL-12.5: rules only. Nothing here is a verdict, and no base has been taken.**
- Part A's base is not yet taken. Two dispatches of `test262 evidence` at the rule commit, with `include_negative`
  true, are void; they are retained and named, not judged (section 1). Part B's base is the rule commit's parser
  assembly, built when part B runs.
- Parts C and D's bases follow SJL-12.4, which adds the parse corpus and the tool.
- The after runs follow SJL-12.5's last item, the compiler's for-loop pass.

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
