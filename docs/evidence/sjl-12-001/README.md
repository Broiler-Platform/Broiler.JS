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

**State, 2026-10-19, at SJL-12.1: rules only. Nothing here is a verdict, and nothing has been taken.**
- Part A's base is taken at the rule's commit by `test262 evidence`, with `include_negative` true. Part B's base is
  the rule commit's parser assembly, built when part B runs.
- Parts C and D's bases follow SJL-12.4, which adds the parse corpus and the tool.
- The after runs follow SJL-12.5's last item, the compiler's for-loop pass.

**What the bundle will hold:**
- `base/`: part A's base run, imported unchanged with the digest of its zip, and parts C and D's base runs;
- `corpus/`: the parse corpus's commit, its manifest's digest and the ancestry the rule's conditions ask for;
- `after/`: the after runs of every part, each comparison's report and the table of every C1, M, X and D row;
- `tools/`: the parse-corpus tool, added at SJL-12.4;
- `controls/`: each control's patch or copy, and its output;
- `catalogue/`: SJL-12.3's completeness check of the early-error catalogue (task 3.11), with the inventories it read
  and the checker that re-runs it (added 2026-10-20). It is a record of the catalogue, not a part of this rule.

**It holds no figure until a run is taken**, and then only in the run's own files and this README's reading of them.

The owner's direction of 2026-10-18, "Please do all recommended decisions and skip waiting for review.", under which
JSD-0080 records its decisions, changes no rule: nothing is accepted without review, and SJL-12 may not meet its gate
before SJL-3 meets SJL-3's.

Broiler.VM judges its own half in its own bundle of the same name. This bundle reads none of its runs.
