# Evidence bundle SJL-11-001 (Broiler.JS) - regular expressions

**Milestone:** SJL-11 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-11-regexp.md`: its Broiler.JS clauses 4 to 6, SJL-11.6's
adversarial corpus in this engine, and this engine's measurement.

**Rule:** [`decision-rule.md`](decision-rule.md), committed at SJL-11.1 before any code of the area:
- part A is test262 per path in UTC, with negative-metadata tests (clauses 5 and 6);
- part B is this engine's measurement, over a benchmark class, `RegExpBenchmarks`, that the rule's inputs commit adds;
- part C is the adversarial corpus in this engine, the programs of [`inputs/adversarial/`](inputs/adversarial/);
- part D is the culture independence of the regular-expression vectors, under the invariant culture, `tr-TR` and
  `lt-LT`;
- part E is the deletion list, a search and a metadata check (clause 4).

Clause 7, every correction listed, is read from `Broiler.JS/CHANGELOG.md` against the parts' C1 rows and the rule's
list of this engine's corrections. The rule names it and does not decide it.

*Dated note, 2026-11-17, on branch `sjl-11-5`; the paragraphs above are not rewritten.* The rule is revised, before
any run it judges, by [`decision-rule-revised-2026-11-17.md`](decision-rule-revised-2026-11-17.md), which quotes it
whole: part A's list of this engine's corrections names rows S12 to S15 and M8 to M27 of the correspondence, the
corrections of Broiler.JS that Broiler.VM's JSD-0081 added by its decisions D-jsrows, D-jsexec and D-jsrest of
2026-11-16, and says how a row the list names "with Broiler.Regex 0.1.0-preview.4" reads now that the base routes
through `0.1.0-preview.6`. `decision-rule.md` is not edited; the two are read together.

**Area verdict:** [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts", "Regular expressions
(SJL-11.1)", with Broiler.VM's JSD-0081,
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0081-regular-expressions-move-to-broiler-regex-under-its-owner-conditions.md`,
and its correspondence from code, retained in Broiler.VM's bundle of the same name
(`Broiler.VM/docs/evidence/sjl-11-001/correspondence.md`, with every probe and each engine's answers).

**Inputs, committed with the rule:**
- [`inputs/adversarial/`](inputs/adversarial/): part C's programs, one per case, and `manifest.tsv`, written by
  Broiler.RegEx's `eng/adversarial-programs.py --engine broiler-js` from that repository's
  `tests/Adversarial/corpus.tsv`. The manifest's header names the corpus's SHA-256,
  `1a0f8c56bb970fa784fc6e573e9968e939d298d3e912320ed8c1a407fa997dd5`, and the Broiler.RegEx commit both were read at,
  `c2aa77167cb3ded40ef50b5c3eeae26f8f7c0b54`, on its branch for SJL-11.1, and each program's SHA-256. Two runs of the
  generator there wrote byte-identical files, and these are a byte-for-byte copy of them. Nothing in the folder is
  edited here. Broiler.VM's programs, in its bundle of the same name, come from the same corpus at the same commit.
- [`inputs/workloads/`](inputs/workloads/README.md): part B's six workloads and six controls, with their README.
  They are Broiler.VM's workloads, byte for byte, and the README gives each file's SHA-256.

**The pins.** test262 `ccaac100`. Broiler.Regex `0.1.0-preview.4`, which this engine restores today, until SJL-11.5
moves the pin to `0.2.0-preview.1` (JSD-0081 decision D12). From SJL-11.5 this engine passes Broiler.Regex no meter,
a frame ceiling of `1 << 20`, a trail ceiling of `1 << 21` and a nesting limit of 512 (decisions D7 and D10).

*Dated note, 2026-11-01; the paragraph above is not rewritten.* It reads "until SJL-11.5 moves the pin to
`0.2.0-preview.1` (JSD-0081 decision D12)", after decision D12 of Broiler.VM's JSD-0081 ("the SJL-11 surface ships as
`0.2.0-preview.1`"). The owner wrote on 2026-11-01 "Please keep using version 0.1.0-x scheme!", which revises D12: the
SJL-11 surface ships in the `0.1.0-preview.N` stream, not as `0.2.0-preview.1`, and since the owner published
Broiler.Regex `0.1.0-preview.5` on 2026-10-29 from Broiler.Regex `24344b3`, the next is `0.1.0-preview.6`. SJL-11.5
moves the pin to `0.1.0-preview.6`, or a later version of the same stream. This engine's `Directory.Packages.props`
has pinned Broiler.Regex `0.1.0-preview.5` since the owner's dependency update of 2026-10-29 (`a58d31d1`), where the
paragraph names the `0.1.0-preview.4` restored when it was written. The filed rule carries a dated note of its own
([`decision-rule.md`](decision-rule.md)).

**Owner:** the Broiler.JS maintainer. One person holds every role (the programme's EX-30, recorded in Broiler.VM).
**Reviewer:** none, and nothing here has been read by anyone but its author.

**State, 2026-10-25, at SJL-11.1: rules only. Nothing here is a verdict, and nothing has been taken.**
- The inputs commit adds `RegExpBenchmarks` and the benchmark `Program.cs`'s `--regexp-answers`, which read the
  workloads verbatim (decision D17).
- Every base run is taken at the base commit, the first parent of the merge that lands SJL-11.5, while merges to this
  engine's and the shared folder's code are held (decision D16).
- The after runs follow that merge.

*Dated note, 2026-11-17, at the inputs commit, on branch `sjl-11-5`; the block above is not rewritten.* It reads
"**State, 2026-10-25, at SJL-11.1: rules only. Nothing here is a verdict, and nothing has been taken.**" Since
2026-11-01, [`engine/`](engine/) holds both engines' first runs on the regular-expression vectors at corpus
revisions 31 and 32, with each engine's negative control and this engine's three cultures, as the note of that day
below says; they are retained and read, not judged, and none of them is taken at a commit the rule names. So
"nothing has been taken" no longer holds as written: **no run the rule judges has been taken** (no base, no after,
no control and no measurement), and nothing here is a verdict. This commit is the inputs commit the block names:
it adds `RegExpBenchmarks` (`Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks/RegExpBenchmarks.cs`) and
the benchmark `Program.cs`'s `--regexp-answers`, which read the files of [`inputs/workloads/`](inputs/workloads/)
verbatim, embedded in the benchmark assembly byte for byte and each checked against its SHA-256 there before it
runs. It changes only `Broiler.JS/benchmarks/` and `docs/`. It is made on the branch of SJL-11.5, `sjl-11-5`
(decision D35), where the rule asks for "the default-branch commit"; the rule's base commit "descends from the
inputs commit", so this commit reaches the default branch before the base is taken, or the base does not satisfy
the rule. That is the owner's to order; nothing here merges it.

**The commits named so far:**
- **The rule commit** carries [`decision-rule.md`](decision-rule.md) and the inputs. It is named here, by all forty
  digits, when the change that carries it has merged.

No other commit is named yet.

*Dated note, 2026-11-18, on the branch `sjl-11-5-inputs`; the list and the line above are not rewritten.* They read
"**The rule commit** carries [`decision-rule.md`](decision-rule.md) and the inputs. It is named here, by all forty
digits, when the change that carries it has merged." and "No other commit is named yet." The commits the rule names
before they exist, and its revisions, are named here by all forty digits, as this branch holds them:
- **The rule commit**, the commit that adds `decision-rule.md` and the inputs, is
  `0f21a84c4cc1bdfc001778833607aeb320669936`; the same change's `1cf69b998eff3638ef9840bdac9c0a0ebca92ddf` revised the
  rule that day, after review, as its note of 2026-10-25 says. The change merged on 2026-10-25 as
  Broiler-Platform/Broiler.JS#1069, `f918b040f4992b8a230b8b6d9a0f2c03000632be`.
- **The inputs commit** is `24fe7cba2dadbadb5917cf20e6c884d1ddabec85`, and **the rule's revision before any of its
  runs**, which files [`decision-rule-revised-2026-11-17.md`](decision-rule-revised-2026-11-17.md), is
  `189d65ffc84c3943338a5fab5be5e178fc4d3092`. Both were made on SJL-11.5's branch `sjl-11-5`, its first two commits
  after the default branch's `6fda6ca4`.
- **The rule's second revision**, [`decision-rule-revised-2026-11-18.md`](decision-rule-revised-2026-11-18.md), is
  `45a15487308eafb8c92d5325ffc22ee66e0a6475`, the one dated revision the owner's answer of 2026-11-18 orders ("Allow
  both", which the revision quotes whole, with the alternatives not taken). It names Broiler.VM's row M28 in part A's
  list, in the row's two halves, and has part A's runs taken by the evidence workflow as the default branch carries it
  at the base commit, admitting `3b4b6860`'s move of its jobs from `ubuntu-latest` to `ubuntu-24.04` and no other
  change. The note of 2026-11-17 above on the rule reads "`decision-rule.md` is not edited; the two are read
  together"; with this revision the three are, and none of them is edited.

The three commits are on the branch `sjl-11-5-inputs` with the default branch at `aecc5f89` merged into them
(`2d2ffed7ceb7c5380341df806b39545a4f65181c`), and are filed to land on the default branch by a merge commit before the
base is taken. Until that merge they are not default-branch commits, and the merge that lands them is named here
after it merges. No base commit is named, the hold of decision D16 has not started, and no run the rule judges has
been taken; nothing here is a verdict.

*2026-11-01:* the regular-expression vectors this bundle's part D runs were added at corpus revision 31, `conformance/vectors/regexp/` (milestone SJL-11.3), whose expected values are `scripts/conformance/regexpref.py`'s (`ref:regexpref@1`), as at `3ea693c727e7a5801d3e6681772f53dc98ccc10f` (first committed at `4420d902b55c29ab37792cb2cc1f74cbd5804c34`). Revision 31 classifies no difference; the revision that classifies both engines' first runs is the one part D names, and this README names it when it exists. **A disagreement for the owner:** part D's population is "every vector of conformance/vectors/regexp/" and its MET clause holds each to "the outcome its class expects (normative: out; ...)", but the 18 vectors of `regexp/budget.vectors` whose `out` is `status: exhausted` are library-level (JSD-0081's decision D25), and the corpus's SCHEMA.md section 4 has an engine-level runner skip them and give them no line, so this engine's run can never give their `out`. Read with SCHEMA.md, part D's population leaves them out; the rule is not revised here, and its reading is the owner's to state.

*2026-11-01, at corpus revision 32:* **part D names revision 32**, the revision that classifies both engines' first
runs on the regular-expression vectors (`conformance/vectors/REVISIONS.md` row 32). Its reasons are in
`scripts/conformance/regexp-divergences.txt`.
- **The disagreement above is decided.** By the owner's decision D-partd, taken as recommended under the owner's
  direction of 2026-10-18 and recorded in Broiler.VM's JSD-0081, "The owner's decisions of 2026-11-01", the status
  vectors are judged at library level only, by Broiler.Regex's part D. This part's population is the non-status vectors.
  The rule carries the decision as a dated note under part D and is not rewritten.
- **The runner.** This engine's runner, `CorpusRun`, skips each status vector since `d8cb8406`, and its summary line
  names each one.
- **[`engine/`](engine/) holds both engines' first runs on revision 31**, with each engine's negative control.
  Broiler.JS was run under the invariant culture in UTC and in New York, and under `tr-TR` and `lt-LT`. Broiler.VM was
  run from a scratch archive and scratch pin. It also holds both engines' runs on revision 32, and
  [`engine/commands.txt`](engine/commands.txt) says how each was taken.
- **Revision 31, the base-like reading of the cultures.** This engine's `regexp/` lines under `tr-TR` and `lt-LT`
  differ from the invariant culture's only in row M2a's vectors. That is the culture dependence part D's after runs
  must not show (row M2a, a correction SJL-11.5 makes). The reading is retained and read, not judged: it is taken at
  no commit the rule names.
- **What the first runs found, beside the rows the correspondence named.** Revision 32 and JSD-0081 record the owner's
  decisions of 2026-11-01:
  - row S5's class case is a correction of this engine (D-s5);
  - since `a58d31d1` this engine's `u` and `v` patterns go to Broiler.Regex `0.1.0-preview.5`, not to the
    `0.1.0-preview.4` "The pins" above names, and rows M4, S3b and B8 record its answers through it (D-p5);
  - rows M2, S7, S8 and F3 hold inputs the correspondence's probes did not reach (D-widen).

  The rule's list of this engine's corrections is read with them, by the same dated note. No test262 file at
  `ccaac100` tries any of their inputs, so part A's list of C1 rows needs none of them.

**What the bundle will hold:**
- `base/`: part A's base run, imported unchanged with the digest of its zip, and parts C and D's base runs;
- `after/`: the after runs of every part, each comparison's report, and the table of every C1, C3 and C5 row;
- `engine/`: this engine's runs of the regular-expression vectors SJL-11.3 adds, and part D's three cultures;
- `controls/`: each control's patch and its output, and its revert;
- `measurement/`: part B's sub-bundle, with its own README and manifest.

**It holds no figure until a run is taken**, and then only in the run's own files and this README's reading of them.
A test262 suite total is never quoted: part A is decided per path, and totals decide nothing.

The owner's direction of 2026-10-18, "Please do all recommended decisions and skip waiting for review.", under which
JSD-0081 records its decisions, changes no rule: nothing is accepted without review, and SJL-11 may not meet its gate
before SJL-7 meets SJL-7's, and so not before SJL-3 meets SJL-3's.

Broiler.VM and Broiler.RegEx judge their own halves in their own bundles of the same name. This bundle reads none of
their runs.
