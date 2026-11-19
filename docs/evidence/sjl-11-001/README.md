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

*Dated note, 2026-11-17, after review, on branch `sjl-11-5`; the revision is a filed rule file and is not edited, and
the paragraph above is not rewritten.* What the revision says and the sources read otherwise:
- **The runs seen.** It says it is written "before any run this rule judges and before any run of this engine's
  regular expressions at SJL-11.5", and its list of the runs seen leaves one out. At 2026-11-17T08:28:06Z, before the
  inputs commit `24fe7cba` was made and before the revision `189d65ff`, the benchmark `Program.cs`'s `--regexp-answers`
  ran the workloads of [`inputs/workloads/`](inputs/workloads/README.md) through this engine, from a Release build of
  this branch's working tree: the inputs commit's files not yet committed, over the default branch's product code at
  `6fda6ca4`, the translator's. It was seen before the revision was written. It is taken at no commit the rule names,
  it is no base, after, control or measurement, and it judges nothing, so the revision's "It is not weaker evidence"
  still holds; this bundle does not retain its output.
- **No earlier revision.** It says "No earlier revision of it exists". No earlier revision file exists. The blob it
  quotes, `b1f7091d`, already holds the rule's own revision of 2026-10-25, "*Revised 2026-10-25, the day it was
  written, after review, before any run ...*", and its dated notes of 2026-11-01, which the revision says stand.
- **How it quotes.** From its heading "The rule it revises, quoted whole" on, each line of the blob is prefixed with
  "> ", and a blank line is a bare ">". The file does not say so itself.
- **Rows M2a and M15 at the base.** Since SJL-9's clause 8 (`0705e80b`, 2026-11-12) the translator has built every
  .NET `Regex` with `RegexOptions.CultureInvariant` (`Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.cs:1013-1016`
  at `6fda6ca4`), so both rows are corrected already at the base and cannot move between the base and after runs. The
  revision adds M15 to part A's list, where it can explain no row. Part D's "How it is read", "the base runs show it
  (row M2a, under `tr-TR` and under `lt-LT`), and the after runs must not", was written at `1f91b8f1`, before that
  change: at the base its first half no longer holds, and its second still does. The revision reads neither.
- **Part C and "The pins" through `0.1.0-preview.6`.** The revision says "Nothing else changes: parts B to E". Part C's
  reading of the N cases, which "end the process where Broiler.Regex `0.1.0-preview.4`'s parser recurses past the
  stack" at the base, and "The pins" above, "Broiler.Regex `0.1.0-preview.4`, which this engine restores today, until
  SJL-11.5 moves the pin", read through `0.1.0-preview.6` at the base, as the plan's note of 2026-11-16
  ([`SharedLibraries.md`](../../roadmap/SharedLibraries.md)) says: the base pins it, SJL-11.5 does not move the pin,
  and at the base the deepest nesting of row S3b compiles rather than ending the process.
- **The rows' words.** The revision says "Each of the correspondence's rows above says that no test262 file that run
  fails is explained by it". At Broiler.VM's `5e90bfe4` only row S12 says so: "No test262 file at `ccaac100` that
  Broiler.JS's `sjl-10-001` after run fails is explained by this row ..., so no Broiler.JS part A row is known to move
  for it". Rows S13 to S15 and M8 say "as row S12, no Broiler.JS test262 row is known to move for it", and rows M9 to
  M27 "no test262 row is known to move for it".

None of this changes what the revision decides. Every run this branch has taken is a local run of its own, at the
default branch's product code or at the branch's: none is a base or an after run of the rule.

*Dated note, 2026-11-17, on branch `sjl-11-5`, after the deletion of `RegexTiering`; the paragraphs above are not
rewritten.* Two findings of the branch's local runs, neither of them a run of the rule:
- **A pre-existing defect: `RuntimeTests.JSException_From_RegularException_CreatesNewJSException` depends on the
  order of its project's tests.** The review of the branch recorded it as an intermittent failure "no row explains"
  ("JSException.CreateJSError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run.").
  It is no row's and not this branch's. The test calls `JSException.From` on an `InvalidOperationException`, whose
  constructor reads `JSException.CreateJSError`, which only the BuiltIns assembly's module initializer sets, and the
  test touches no BuiltIns type itself: it passes only when a test that does (`RuntimeTests.JSNull_IsNull`, the tests
  that build a `JSString`, or `Phase5TaggedValuePrototypeTests`, which creates a `JSContext`) has run before it in the
  same process. Run alone it fails, at the default branch's `6fda6ca4` as at the branch's `1bee060a`. In a run of the
  whole project the order is xunit v3's, which follows its tests' unique IDs, and those change with the test
  assembly's path (the runner's assembly ID differs between the two build directories named next, and so does the
  order), so the order, and the outcome, follow the directory a build is made in, not the commit: built from a clean
  archive of `6fda6ca4` in `/dev/shm/js-s115b/base`, where `1bee060a`'s full runs had failed, a run of the project
  fails 3 times in 3, and `1bee060a` built in `/dev/shm/js-s115b/race-6fda6ca4`, where `6fda6ca4`'s ten full runs had
  passed, passes 3 times in 3; `7eeba1eb`, the commit that adds this note, does the same in each directory. The runs
  are not retained in this bundle; the change's description summarises them. The test is not skipped and is not changed here; making it independent
  of the order (for example by touching a BuiltIns type, or creating a `JSContext`, before it calls `JSException.From`)
  is a follow-up of its own.
- **A change no row names: the `y` flag on the members that do not go through `exec`.** Since `4e2e00ad` on this
  branch, `IJSRegExp.IsMatch` (which the `assert` module's `assert.match` calls) and the public `JSRegExp.Replace`
  honour the `y` flag as `RegExpBuiltinExec` from `lastIndex` and `RegExp.prototype[@@replace]` do in the
  specification's text, and the public `JSRegExp.Split`, which `String.prototype.split` calls when `@@split` is
  `undefined` or `null`, tries every position as `RegExp.prototype[@@split]`'s splitter, sticky whatever the flags,
  does. At the branch's first parent the three honoured `y` only for a pattern routed to Broiler.Regex. No row of
  Broiler.VM's `correspondence.md` names this (read at Broiler.VM's `450c785f`), so it is recorded here as **a finding
  for Broiler.VM's correspondence**, whose row is Broiler.VM's to add; `Broiler.JS/CHANGELOG.md` lists it as "A change
  no row names". The `split` fallback is still not the specification's, which converts the separator with ToString
  when no `@@split` applies, and is left for a later change.

*Dated note, 2026-11-18, on branch `sjl-11-5`, after the default branch at `5429c406` was merged into it; the note
above is not rewritten.* Its first item reads "A pre-existing defect:
`RuntimeTests.JSException_From_RegularException_CreatesNewJSException` depends on the order of its project's tests"
and ends "making it independent of the order (for example by touching a BuiltIns type, or creating a `JSContext`,
before it calls `JSException.From`) is a follow-up of its own". That follow-up was made on the default branch by
`a4a2f911c8984656569944692867144f4a1fdef6` ("Make two order-dependent tests independent of test order"), which this
branch now holds through that merge: the test "now runs that module initializer itself". At this branch's merge
commit `fff4172c8103cc75a71ab5b9bf5c445caae328c3`, built in Release from a clean archive on disk, the test passed run
alone (`dotnet test Broiler.JS/Broiler.JavaScript.Runtime.Tests -c Release --no-build --filter
"FullyQualifiedName~JSException_From_RegularException_CreatesNewJSException"`, one test, passed), and CI's test step
passed whole. These are local runs of the branch, not runs of the rule, and are not retained here.

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

*Dated note, 2026-11-18, on branch `sjl-11-5`; the paragraphs above are not rewritten.* The note of 2026-11-17 at the
inputs commit above ends "so this commit reaches the default branch before the base is taken, or the base does not
satisfy the rule. That is the owner's to order; nothing here merges it." The owner ordered it on 2026-11-18, by an
answer of the owner's own, not one taken under the owner's direction of 2026-10-18. Asked: "SJL-11.5's base in
Broiler.JS needs two things only you can order. First, its inputs commits (24fe7cba, 189d65ff) and a new dated rule
revision must land on main by a merge commit before the base is taken. Second, test262-evidence.yml moved from
ubuntu-latest to ubuntu-24.04 in your dependency update of 2026-10-29 (3b4b6860), and the rule fixes the workflow "as
this file's commit carries it". Only a dated rule revision filed before any run can admit that change. Do you allow
both?" Chosen: "Allow both" — "The inputs land on main by a merge commit with one dated revision. The revision lists the
y-flag correction row and admits the ubuntu-24.04 workflow. Then the hold starts and the base runs are taken at main's
head." Not taken: "Inputs only" — "The inputs land, but the workflow change is not admitted. Part A's base can then only
be taken with the workflow as the rule's commit carries it."; "Not yet" — "Nothing lands on main for SJL-11.5. Its base
and merge wait." So, by the rule's words and the owner's:
- **What lands first.** The inputs commit `24fe7cba` and the rule's revision `189d65ff`, with one new dated revision of
  the rule, land on the default branch by a merge commit before the base is taken. That revision is not filed here, and
  no rule file is added or edited here. By the owner's answer it "lists the y-flag correction row", the row of the
  finding above, "whose row is Broiler.VM's to add", "and admits the ubuntu-24.04 workflow": part A's configuration
  fixes the workflow "by .github/workflows/test262-evidence.yml as this file's commit carries it", and `3b4b6860` of
  2026-10-29 moved that file's jobs from `ubuntu-latest` to `ubuntu-24.04`.
- **Then the hold and the base.** The rule reads "**The base commit** is the default-branch commit that the merge
  landing SJL-11.5 has as its first parent. It descends from the inputs commit. Every base run is taken there, before
  the merge, while merges to `Broiler.JS/Broiler.JavaScript.*/` and `Broiler.ECMAScript/` are held (decision D16)." and
  "**The after commit** is that merge. If its first parent is not the commit the base runs were taken at, the base runs
  are void and are taken again before any after run is seen." The block above, "Every base run is taken at the base
  commit, the first parent of the merge that lands SJL-11.5, while merges to this engine's and the shared folder's code
  are held (decision D16)", says the same.
- **The pins.** By the owner's answer of the same day on D13, "Keep preview.6" — "No new Regex release. Both cut-overs
  use preview.6, whose library sources the reading judged.", this engine keeps its pin of Broiler.Regex
  `0.1.0-preview.6`, which the note of 2026-11-17 above on "The pins" reads the base as pinning.
- **Broiler.RegEx's reading.** Its candidate reading at `49d7be1f`, part A's second reading and the owner's ruling that
  "the block on both cut-overs is lifted as far as part A goes" are Broiler.RegEx's records, which
  [`SharedLibraries.md`](../../roadmap/SharedLibraries.md)'s SJL-11 section quotes in its notes of the same date, with
  the owner's other answers of that day and what the merge still waits on. This bundle reads none of their runs.

No commit is named here as the default branch's, and "The commits named so far" above is not changed by this note. No
run the rule judges has been taken, and nothing here is a verdict.

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
  both", which the revision quotes whole, with the alternatives not taken); the same branch's
  `909532bd2b2157eaf4364c4e8f6dfd0ff5e33f03` revised it that day, at review, before it merged and before any run, as
  its note of 2026-11-18 says, restating row M28's own limit on the `split` half. It names Broiler.VM's row M28 in
  part A's list, in the row's two halves, and has part A's runs taken by the evidence workflow as the default branch
  carries it at the base commit, admitting `3b4b6860`'s move of its jobs from `ubuntu-latest` to `ubuntu-24.04` and
  no other change. The note of 2026-11-17 above on the rule reads "`decision-rule.md` is not edited; the two are read
  together"; with this revision the three are, and none of them edits another.

These commits are on the branch `sjl-11-5-inputs` with the default branch at `aecc5f89` merged into them
(`2d2ffed7ceb7c5380341df806b39545a4f65181c`), and are filed to land on the default branch by a merge commit before the
base is taken. Until that merge they are not default-branch commits, and the merge that lands them is named here
after it merges. No base commit is named, the hold of decision D16 has not started, and no run the rule judges has
been taken; nothing here is a verdict.

*Dated note, 2026-11-18, on branch `sjl-11-5`, the hold's start; the list "The commits named so far", the line "No
other commit is named yet." and the two notes above are not rewritten.* The second note above ends "the merge that
lands them is named here after it merges. No base commit is named, the hold of decision D16 has not started, and no
run the rule judges has been taken". That change, Broiler-Platform/Broiler.JS#1125, merged on 2026-11-18 as
`5429c4065e8b273e5c7965598b8a2f5490a8ad98`, a merge commit whose first parent is the default branch's `aecc5f89` and
whose second is `20f1cbbb`, the head of `sjl-11-5-inputs`. So the inputs commit `24fe7cba` and the rule's revisions
`189d65ff`, `45a15487` and `909532bd` are default-branch commits from that merge on.
- **The hold starts.** By the owner's order of 2026-11-18, quoted in the first note above ("Allow both" — "... Then
  the hold starts and the base runs are taken at main's head."), the hold of decision D16 starts on 2026-11-18, at
  `5429c4065e8b273e5c7965598b8a2f5490a8ad98`, Broiler-Platform/Broiler.JS#1125's merge, which is the default branch's
  head when it starts. Nothing else merges to the default branch until the merge that lands SJL-11.5: the rule holds
  merges to `Broiler.JS/Broiler.JavaScript.*/` and `Broiler.ECMAScript/`, and any other merge would move the default
  branch's head too, so that the merge landing SJL-11.5 would not have the base runs' commit as its first parent.
- **The rule's words.** It reads "**The base commit** is the default-branch commit that the merge landing SJL-11.5
  has as its first parent. It descends from the inputs commit. Every base run is taken there, before the merge, while
  merges to `Broiler.JS/Broiler.JavaScript.*/` and `Broiler.ECMAScript/` are held (decision D16)." and "**The after
  commit** is that merge. If its first parent is not the commit the base runs were taken at, the base runs are void
  and are taken again before any after run is seen." Neither revision changes them: the second says it does not
  change "the definitions of the rule commit, the inputs commit, the base commit and the after commit, and the
  voiding of the base runs if the after commit's first parent is not the commit they were taken at".
- **What it means here.** Every base run is taken at `5429c4065e8b273e5c7965598b8a2f5490a8ad98`. That commit is the
  base commit only if the merge landing SJL-11.5 has it as its first parent; if the default branch moves before that
  merge, the runs taken there are void, as the rule says. This branch merged the default branch at that commit into
  itself on 2026-11-18; that merge is this branch's and moves no default-branch commit.

Nothing here names the after commit, and no run here is a verdict.

*Dated note, 2026-11-18, on branch `sjl-11-5`, the base runs; the list "What the bundle will hold" below is not
rewritten.* It reads "`base/`: part A's base run, imported unchanged with the digest of its zip, and parts C and D's
base runs". Every base run the rule asks of parts A, C and D was taken on 2026-11-18 at
`5429c4065e8b273e5c7965598b8a2f5490a8ad98`, during the hold, with the default branch's head at that commit before
and after each, and is retained in [`base/`](base/), with [`base/hashes.txt`](base/hashes.txt) (the SHA-256 of every
file there) and [`base/manifest.json`](base/manifest.json). They are base runs only, read and not judged: each part
is judged on its after runs, which do not exist yet, and nothing here is a verdict.
- **Part A**, by `.github/workflows/test262-evidence.yml` as the default branch carries it at that commit, blob
  `fc38229782ee1b2e93ecca9e990d182f5e44dbf2`, the blob the second revision admits: workflow run
  [44377139618](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/44377139618) (attempt 1), dispatched on
  `main` with commit `5429c4065e8b273e5c7965598b8a2f5490a8ad98`, suite_ref
  `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone
  empty and include_negative true, the inputs part A's configuration line fixes. Its head commit, its `run.txt`'s
  tested commit and its dispatched ref all read `5429c406`; its conclusion is success, and so is every job's. Its
  artifact `test262-evidence` (id 14465668792) was downloaded, and the zip's SHA-256,
  `94779ac9e0dba0fa11aa24a4d29443291ea9ad40c986eb7898374444dd02a9a0`, equals the digest GitHub records for it; the
  zip is named here by that digest, and every file in it is retained unchanged in [`base/part-a/`](base/part-a/),
  with [`imported.txt`](base/part-a/imported.txt), `runner-images.txt` and `part-a-artifacts.txt` added. Its own
  records read "shards: 8; distinct lists: 1" (one shell), "shards: 8; in the expected zone: 8" (UTC, tzdata 2026e)
  and "0 incomplete shard(s)". The shard jobs ran on three versions of the `ubuntu-24.04` image, as
  `runner-images.txt` lists; the rule fixes the shell and the zone, which the records show, and not the image version.
- **Part C**, locally, from a clean `git archive` of that commit on disk, the shell `BroilerJS` built in Release:
  every case of [`inputs/adversarial/manifest.tsv`](inputs/adversarial/manifest.tsv), one process each with
  `--script-host`, its program checked against the manifest's SHA-256 first (every one matched; the folder is the
  rule commit's, tree `3b1c1a60`), under the manifest's wall-clock limit, the invariant culture and `TZ=UTC`, by
  [`base/run-part-c.py`](base/run-part-c.py); exit 0. [`base/part-c/results.tsv`](base/part-c/results.tsv) gives each
  case's exit and outcome and `base/part-c/cases/` each process's whole output. At the base, cases M01 and M02
  complete where their declared outcome is a `RangeError`, N02, N04, N08, N10, N11, N14 and N16 complete where theirs is
  a `SyntaxError`, and N05 passes the wall-clock limit where its is a `SyntaxError`; every other case gives its
  declared outcome. Part C compares the after outcomes with the declared ones and with these.
- **Part D**, locally, from the same tree: the vector runner `Broiler.JavaScript.Vectors.Tests` built in Release, run
  three times, under the invariant culture, `tr-TR` and `lt-LT`, one evaluating process each, `TZ=UTC`; exits 0, 1 and
  1. Each report is the whole corpus at the tree's corpus revision, 43, kept as
  [`base/part-d/run-*.tsv.gz`](base/part-d/) with each run's negative control beside it. The `regexp/` vectors of
  revision 43 are revision 32's, the revision part D names, with the same ids, inputs and `out` lines; revision 39
  retired some of their `divergence` fields and reclassed them, which changes only their `class` and `divergence.*`
  lines (`git diff de8d7b54 5429c406 -- conformance/vectors/regexp`). [`base/part-d/reading.txt`](base/part-d/reading.txt)
  reads the three reports: their `regexp/` lines are byte-identical, and every line that differs between the cultures
  is an `intl/` vector, outside part D's population, which is why the `tr-TR` and `lt-LT` runs exit 1. Part D's "How
  it is read", "the base runs show it (row M2a, under `tr-TR` and under `lt-LT`), and the after runs must not", does not
  hold at this base for its first half, as the note of 2026-11-17 above on rows M2a and M15 says.
- **Part B** is not taken here. The rule judges it "an acceptance comparison when the lane is a controlled lane", and
  this machine is not one; its arms are builds of the base and after commits, measured in one session in a rotating
  order, so it needs the after commit. Whether it is taken on the owner's lane or recorded void is the owner's.
- **Part E** has no base run: its population is "the after commit's sources".

[`base/commands.txt`](base/commands.txt) gives every local command as it was run, and `base/logs/` each step's gate,
machine record, exit, output and time. The test262 checkout the local parts would need is none: neither part C nor
part D reads test262.

*Dated note, 2026-11-18, on branch `sjl-11-5`, after review of the base runs; the note above is not rewritten, and
no file in `base/` changes.*
- **The default branch's head during the runs.** The note above reads "with the default branch's head at that
  commit before and after each". The retained records show less. The local runs' script checked once, before its
  first step, that this clone's remote-tracking ref of the default branch read `5429c406`, with no fetch of its own,
  and no record checks it after any run. What shows the head at that commit throughout lies outside `base/logs/`:
  that remote-tracking ref last moved on 2026-11-18 at 18:26:39 UTC, to `5429c406`, before every run, and fetches
  after the runs left it there; `git ls-remote origin refs/heads/main` read
  `5429c4065e8b273e5c7965598b8a2f5490a8ad98` at 2026-11-18T22:06:48Z, after every run; and
  `.github/workflows/test262-evidence.yml` has one run at that commit, 44377139618.
- **Part A was dispatched first.** Run 44377139618 was dispatched at 2026-11-18T19:07:33Z, before this branch's local
  checks of its merge of the default branch (from 19:07:58Z) and before the note of the hold's start above was
  committed (19:24:43Z) and pushed (19:24:55Z). The hold began with Broiler-Platform/Broiler.JS#1125's merge, by the
  owner's order, and the run reads only `5429c406`, so the order changes nothing it retains.
- **Part A's jobs by id.** `base/part-a/runner-images.txt` names each job by its name only. GitHub's record of the
  run's jobs, read 2026-11-18, gives prepare 135059191047, shard (0) 135059373813, shard (1) 135059373739, shard (2)
  135059373728, shard (3) 135059373834, shard (4) 135059373876, shard (5) 135059373785, shard (6) 135059373952,
  shard (7) 135059373791 and collect 135096320193, each concluded success at `5429c406`.
- **The zone and the toolchain of parts C and D.** Both parts' base runs set `TZ=UTC` (`base/run-part-c.py`,
  `base/commands.txt`) beside the invariant culture, and ran under .NET SDK 10.0.404 and runtime
  Microsoft.NETCore.App 10.0.15, as `base/commands.txt` records. Neither part's rule line nor the manifest's run line
  ("the invariant culture") names a zone or a toolchain. Their after runs are taken with the same `TZ=UTC` and the
  same SDK and runtime, so that neither differs between base and after; this note revises no line of the rule.
- **For the owner: part D's corpus revision.** The note above reads "Each report is the whole corpus at the tree's
  corpus revision, 43". Part D's population is "every vector of conformance/vectors/regexp/ at the corpus revision
  bundle sjl-11-001 names" (`decision-rule.md:218`), and the rule's note of 2026-11-01 reads "The corpus revision
  this part names is 32" (`decision-rule.md:242`). Revision 43's `regexp/` vectors have revision 32's ids, inputs
  and `out` lines, but revision 39 reclassed 22 of them from `divergence` to `normative` and retired their
  `divergence` fields (`git diff de8d7b54 5429c406 -- conformance/vectors/regexp`: 22 `class` lines changed, 23
  `divergence.*` lines removed and one added). So each base report's verdict for those vectors, and its count
  "divergence no longer holds", is read against revision 43's classes, not revision 32's. That reading was the base
  stage's alone. Which one part D takes is the owner's: against revision 32's `class` lines, through a scratch
  corpus pin like the one [`engine/commands.txt`](engine/commands.txt) describes, with the base reports read or taken
  again that way; or against the tree's revision 43, with the equivalence above recorded. Part D's after runs take
  the same choice as its base, and the MET clause's "the outcome its class expects" reads that revision's `class`
  lines.
- **For the owner: part B's base.** The rule reads "Every base run is taken there, before the merge"
  (`decision-rule.md:107`) and "The after commit is that merge" (`:109`). Part B's arms are "B the base commit ... A
  the after commit ... A-again the A arm measured a second time" (`:146`), and "in every repetition, B, A and A-again
  each run once, in an order that rotates by repetition" (`:147`), so arm A, and with it every repetition, can run
  only once the merge exists. If arm B's runs are base runs in the sense of `:107`, that line and `:147` cannot both
  be met; whether they are is the owner's to read, and this note does not read it. The ways recorded so far are part
  B taken in full on a controlled lane after the merge, or part B recorded void, which "leaves the question open for
  this engine" (`:166`). Parts A, C and D's base runs have now been seen, so a dated revision that reconciled `:107`
  with `:147` would be one the rule's section "Revisions" describes: "A rule revised after any run it judges has been
  seen is a new dated file in this directory, quoting this one whole and saying it was written afterwards; its
  decision is then weaker evidence" (`:272-273`).

*Dated note, 2026-11-19, on branch `sjl-11-5`, the owner's answers; the note above is not rewritten.* Its two items
"For the owner" end "Which one part D takes is the owner's" and "whether they are is the owner's to read, and this note
does not read it". The owner answered both on 2026-11-19, with a third question on decision D35, by answers of the
owner's own, not taken under the owner's direction of 2026-10-18. They are quoted verbatim, numbered as the owner's
answers of 2026-11-18 (1 to 3) continue:
- **4.** Asked: "SJL-11.5's part B (the RegExpBenchmarks measurement) runs arm B (base commit) and arm A (the merge)
  interleaved in every repetition, so it can only run after SJL-11.5 merges, and the rule judges it only on a controlled
  lane. Its 'every base run is taken there, before the merge' cannot hold for arm B as written. What should happen to
  part B?" Chosen: "On your lane after merge" — "After the merge, a local session on win-x64-r7-5800x builds arm B from
  5429c406 and arm A from the merge and runs part B as written, as SJL-3's part B was run. A dated note reads arm B as
  built from the base commit, not as a run taken before the merge." Not taken: "Record it void" — "No part B run. It is
  recorded void, which by the rule leaves the measurement question open for this engine. It does not block anything."
- **5.** Asked: "Part D's base runs read the corpus at revision 43, but the rule names revision 32. The regexp vectors'
  ids, inputs and expected outputs are the same in both; only 22 class lines and the divergence lines changed, at
  revision 39. How should part D be read, at both the base and the after runs?" Chosen: "Read revision 32" — "As the
  rule says: the base reports are read again against revision 32's class lines through a scratch pin, with no new runs
  needed, and the after runs are read the same way." Not taken: "Read revision 43" — "Read the tree's revision 43 at
  both, with the equivalence of ids, inputs and outputs recorded. This departs from the rule's named revision."
- **6.** Asked: "Decision D35 orders both cut-overs "after SJL-12's after runs and after the candidate reading". The
  candidate reading is taken and merged (Broiler.Regex #28, #29, #30), but Regex task 4.2 stays open until the scratch
  branch sjl-11-2-control-b is deleted. Is D35 met for SJL-11.5's merge?" Chosen: "Yes, the reading is taken" — "D35
  asks only that the candidate reading come first, and it has. SJL-11.5 may merge once its PR's CI is green and the hold
  still holds." Not taken: "No, close task 4.2 first" — "SJL-11.5 waits until you delete the scratch branch and task
  4.2 is recorded closed. The hold stays on meanwhile."

What they mean here:
- **Part B, by answer 4.** Part B is taken after the merge that lands SJL-11.5, on the controlled lane
  [`win-x64-r7-5800x`](../lanes/win-x64-r7-5800x/README.md), by a local session on the owner's workstation, as bundle
  `sjl-3-001`'s part B was taken there on 2026-11-17
  ([`measurement-win-x64-r7-5800x/`](../sjl-3-001/measurement-win-x64-r7-5800x/README.md)): arm B built from the base
  commit `5429c4065e8b273e5c7965598b8a2f5490a8ad98`, and arm A, with A-again, from that merge. The rule's lines this
  note reads are, whole: "**The base commit** is the default-branch commit that the merge landing SJL-11.5 has as its
  first parent. It descends from the inputs commit. Every base run is taken there, before the merge, while merges to
  `Broiler.JS/Broiler.JavaScript.*/` and `Broiler.ECMAScript/` are held (decision D16)." (`decision-rule.md:106-108`);
  "**The after commit** is that merge. If its first parent is not the commit the base runs were taken at, the base runs
  are void and are taken again before any after run is seen." (`:109-110`); "arms: B the base commit, with
  RegExpBenchmarks.cs and the benchmark Program.cs as the inputs commit has them; A the after commit, with the same two
  files; A-again the A arm measured a second time under another lane name, the A/A lane; each arm's benchmark and
  engine assemblies named by hash in the manifest, which is written and made read-only before any lane runs" (`:146`);
  and "configuration: in every repetition, B, A and A-again each run once, in an order that rotates by repetition; ..."
  (`:147`). By the owner's answer this note reads `:107`'s "Every base run is taken there, before the merge", for part
  B's arm B, as **an arm built from the base commit**, not as a run taken before the merge: arm B's runs are taken after
  the merge, interleaved with arm A's in every repetition as `:147` asks, and are not base runs in `:107`'s sense, which
  parts A, C and D's base runs above keep. Arm B is the base commit only while the merge's first parent is `5429c406`,
  as `:109-110` say of every base run, and the local session checks that before it builds. The part is judged as
  written: "an acceptance comparison when the lane is a controlled lane" (`:144`), by its MET line's clauses 1 to 5
  (`:156-162`). This note revises no line of the rule and adds or edits no rule file; it records the owner's reading of
  `:107` and is not a revision under "Revisions" (`:272-273`). Part B's sub-bundle will be a new folder named for the
  lane, `measurement/win-x64-r7-5800x/`, inside the `measurement/` that the list "What the bundle will hold" below
  names for it and that [`inputs/workloads/README.md`](inputs/workloads/README.md) names as the only place a figure
  from the workloads is retained; neither is rewritten.
- **Part D, by answer 5.** Part D is read against corpus revision 32's `class` lines, the revision the rule names
  ("The corpus revision this part names is 32", `decision-rule.md:242`), at both the base and the after runs, through a
  scratch pin of revision 32's `conformance/vectors/regexp/`; the base reports are read again with no new run. The note
  after this one records that reading, and how the after runs are read the same way. The reading "against the tree's
  revision 43" that the note above names as the other way is not taken.
- **Decision D35, by answer 6.** Broiler.VM's JSD-0081 (at `79d72805`) reads: "**Decision D35 - the order of the
  cut-overs.** Both after SJL-12's after runs and after the candidate reading; either first; both in one release
  train; SJL-11.5's branch exists early for decision D14. The reason: risk D10's stop, and processes that host both
  engines." [`SharedLibraries.md`](../../roadmap/SharedLibraries.md)'s SJL-11 section quotes it in its note of
  2026-11-18 and reads "Whether D35 asks more of the candidate reading than that it has been taken, such as task 4.2's
  close, ..., no record read here says, and this note reads nothing into either." By answer 6, D35 is read as met for
  SJL-11.5's merge by the candidate reading's having been taken: SJL-12's after runs were taken (bundle
  [`sjl-12-001`](../sjl-12-001/README.md)), and the candidate reading was taken and merged in Broiler.RegEx. Broiler.RegEx's
  task 4.2 stays open there, its to record. Answer 6 does not name D35's "both in one release train", which the plan's
  note leaves unread, and this note reads nothing into it.

None of this names the after commit or takes a run; nothing here is a verdict.

*Dated note, 2026-11-19, on branch `sjl-11-5`, part D's base read at corpus revision 32, by answer 5 above; the notes
above are not rewritten, and no retained run output changes.* The note of the base runs reads "Each report is the whole
corpus at the tree's corpus revision, 43", and [`base/part-d/reading.txt`](base/part-d/reading.txt) reads the three
reports at that revision. They are now read again against revision 32's `class` lines, with no new run:
- **The scratch pin**, [`base/part-d/revision-32.pin`](base/part-d/revision-32.pin): `git archive` of
  `conformance/vectors/regexp/` at `b2c2e2243a086285d806866a64b83297791a8ec5`, Broiler-Platform/Broiler.JS#1089's
  merge, the first default-branch commit that holds revision 32 (its `regexp/` tree is that of `de8d7b54`, revision
  32's last change), written twice, byte for byte the same, the second extracted and each file hashed, as
  [`engine/commands.txt`](engine/commands.txt) describes its scratch pins. Nothing is run from it: its `class` and
  `divergence` lines are read against the reports.
- **The reading**, [`base/part-d/reading-revision-32.txt`](base/part-d/reading-revision-32.txt), written with the pin
  by [`base/part-d/read-revision-32.py`](base/part-d/read-revision-32.py) (`python3 -I
  docs/evidence/sjl-11-001/base/part-d/read-revision-32.py --repo . --scratch <an empty directory> --out
  docs/evidence/sjl-11-001/base/part-d`), which reads only git objects and the three retained reports; run twice into
  two directories, it wrote both files byte for byte the same. It checks that revision 32's and revision 43's
  `regexp/` hold the same 1,420 vectors, 18 of them status vectors, with every field but `class` and `divergence.*`
  the same, and the expected outcome the comparison uses for `broiler-js` (`per-engine` on an `impl-defined` vector,
  otherwise `out`) the same for every vector; that 22 `class` lines differ, `divergence` at revision 32 and
  `normative` at 43 (row M2a's 20 vectors and `regexp/literals/s9b-literal-grammar/0001` and `0002`), and that 22
  `divergence.*` fields of revision 32 are retired at 43 and one has other text there. Each report's `regexp/` line
  is checked against revision 43's vector, the corpus the run read, and its verdict is computed again under revision
  32's vector as the runner's comparison does (`VectorRun.Compare`, the corpus's SCHEMA.md section 8): the rendered
  outcome is the report's own, so whether it matches is the report's own, and a difference is classified or not by
  revision 32's `class` and `divergence.broiler-js` lines.
- **What it reads.** In each of the three reports, 1,402 `regexp/` lines, one for each of revision 32's non-status
  vectors and none for the 18 status vectors; at revision 32 the verdicts are the ones revision 43 gave, 169 classified
  divergences and 1,233 matches, and no difference. 22 lines read differently, the same in all three: row M2a's 20
  vectors read `divergence` and "match (divergence no longer holds)", where revision 43 reads them `normative` and
  "match", and the two `s9b-literal-grammar` vectors read `divergence` and "match", carrying no
  `divergence.broiler-js` field. The count "divergence no longer holds" over `regexp/` is 20 in each report at
  revision 32, where it is none at 43.
- **Part D's clauses at the base.** The rule takes the base runs "retained and read, not judged" (`decision-rule.md:219`),
  and reads them in "How it is read" (`:226-228`); only those are read here. **Read at the base**, at revision 32: the
  three reports' `regexp/` lines, read again, are byte-identical across the invariant culture, `tr-TR` and `lt-LT`; and,
  against revision 32's classes, every `normative` vector's result is its `out`, every `impl-defined` vector's is its
  `per-engine.broiler-js` outcome or a difference its `divergence.broiler-js` field classifies, and every `divergence`
  vector's is its `out` or a difference its field classifies, row M2a's 20 fields reporting that they no longer hold.
  **Read at the base** for "How it is read", "the base runs show it (row M2a, under `tr-TR` and under `lt-LT`), and the
  after runs must not": at revision 32 the base runs do not show it; row M2a's vectors match under all three cultures,
  as the note of 2026-11-17 on rows M2a and M15 says (`RegexOptions.CultureInvariant` since `0705e80b`). The MET line,
  "the three after reports are byte-identical and every vector's result after is the outcome its class expects", is
  the after runs', and the base alone decides nothing that needs them.
- **How the after runs will be read, the same way.** Part D's three after runs at the after commit are retained under
  `after/part-d/` with the same report names, and the same script reads them against the same pin's revision 32:
  `--runs after --tree-commit <the after commit> --out <that folder>`. It stops if the after commit's `regexp/`
  vectors differ from revision 32's in any field but `class` and `divergence.*`, or in the expected outcome, and
  otherwise writes the after reports' lines read again and the part's clauses on them. Its reading of each vector's
  result against "the outcome its class expects" takes revision 32's class: `normative`, its `out`; `impl-defined`, its
  `per-engine.broiler-js`; and a `divergence.broiler-js` field that the cut-over answers reports that it no longer holds.
  The MET line is then read on those lines; the reading at revision 43 in [`base/part-d/reading.txt`](base/part-d/reading.txt)
  is retained and is not part D's.

[`base/hashes.txt`](base/hashes.txt) and [`base/manifest.json`](base/manifest.json) list the three new files, which
are added beside the retained reports and change none of them. Nothing here is a verdict.

*Dated note, 2026-11-19, on branch `sjl-11-5`, after review of the two notes above; they are not rewritten, and no
retained run output changes.*
- **Part B's arm A, for the owner.** The note of the owner's answers reads "arm B built from the base commit
  `5429c4065e8b273e5c7965598b8a2f5490a8ad98`, and arm A, with A-again, from that merge" and "The part is judged as
  written". As the branch stands, arm A cannot be built as `decision-rule.md:146` words it, "A the after commit, with
  the same two files", the two being "RegExpBenchmarks.cs and the benchmark Program.cs as the inputs commit has them".
  `Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks/RegExpBenchmarks.cs` is blob
  `604dced1d4a4485e7fa01cac55630b21c0c72f59` at the inputs commit `24fe7cba` and on the branch, and
  `BenchmarkConfiguration.cs` beside it is unchanged too; but the benchmark `Program.cs` is blob
  `6074646340c64176f4c918b1c963dbd1347cdcff` at `24fe7cba` and at `5429c406`, and blob
  `095db42675307de05e23cd22ba5c2b8e24c3241a` on the branch, where `d892dd0b` removes its `--regex-tiering` block,
  which calls `RegexTieringMetrics.Write()`, with `RegexTieringMetrics.cs` and the `RegexTiering` type that file reads
  (decisions D20 and D21). The inputs commit's `Program.cs` therefore does not build at the after commit, and
  `:146`'s "with the same two files" cannot hold for arm A. How arm A is built is the owner's open question; it is not
  built until the owner's reading is recorded here by a dated note. "The part is judged as written" in the note above
  stands for part B's MET line, its clauses 1 to 5 (`:156-162`), only. This note revises no line of the rule and adds
  or edits no rule file.
- **Part D's clauses at the base.** The note above reads "and reads them in "How it is read" (`:226-228`); only
  those are read here." What its first "Read at the base" then reads is `:219`'s reading of the base reports laid
  against the terms of `:220`'s comparison, "the three after reports, byte for byte; and each vector's result after
  against its class's expectation for broiler-js". That comparison is the after runs', and the rule judges it on the
  after runs alone, as [`base/part-d/reading-revision-32.txt`](base/part-d/reading-revision-32.txt) says ("read at the
  base (revision 32's class; the rule judges it on the after runs alone)"); nothing in it is judged at the base.
- **The scratch pin.** The note above says [`base/part-d/revision-32.pin`](base/part-d/revision-32.pin) is written
  "as [`engine/commands.txt`](engine/commands.txt) describes its scratch pins". It follows that procedure, the archive
  written twice, byte for byte the same, the second extracted and hashed; its layout and its `content-sha256`
  convention are its own, stated in its `content-sha256` line, and are not those of `engine/broiler-vm-revision-32.pin`.

Nothing here is a verdict.

*Dated note, 2026-11-19, the merge that lands SJL-11.5, and the hold's end; the notes above and the list "The commits
named so far" are not rewritten.* The note of the hold's start reads "That commit is the base commit only if the merge
landing SJL-11.5 has it as its first parent; if the default branch moves before that merge, the runs taken there are
void, as the rule says" and ends "Nothing here names the after commit, and no run here is a verdict." SJL-11.5 merged
to the default branch on 2026-11-19 as Broiler-Platform/Broiler.JS#1126, `b2b226cd3158f0b7d990a3f4fecbe75b259b4fa1`,
a merge commit whose first parent is `5429c4065e8b273e5c7965598b8a2f5490a8ad98` and whose second is
`ce908940a3e6cb98dbf0246ca0993ff199412644`, the head of `sjl-11-5` (`git log -1 --format=%P b2b226cd`). So, by the
rule's words, "**The after commit** is that merge. If its first parent is not the commit the base runs were taken at,
the base runs are void and are taken again before any after run is seen":
- **The after commit** is `b2b226cd3158f0b7d990a3f4fecbe75b259b4fa1`.
- **The base commit** is `5429c4065e8b273e5c7965598b8a2f5490a8ad98`, the after commit's first parent, and the base
  runs in [`base/`](base/), all taken there, stand.
- **The hold of decision D16 ended** with that merge: it held merges "until the merge that lands SJL-11.5", as the
  note of its start says.

The after runs follow in the next note. Nothing here is a verdict.

*Dated note, 2026-11-19, the after runs of parts C, D and E, their controls, and each part's reading; the notes above
and the list "What the bundle will hold" below are not rewritten.* The list reads "`after/`: the after runs of every
part, each comparison's report, and the table of every C1, C3 and C5 row" and "`controls/`: each control's patch and
its output, and its revert". The after runs of parts C, D and E were taken on 2026-11-19 at the after commit
`b2b226cd3158f0b7d990a3f4fecbe75b259b4fa1` and are retained in [`after/`](after/), with
[`after/hashes.txt`](after/hashes.txt) (SHA-256 `ff541d5c633fd161146f493a06824d3f8086fec74dae2494d11e09e2262a1f46`)
and [`after/manifest.json`](after/manifest.json); the controls of parts C and E are in [`controls/`](controls/), with
[`controls/hashes.txt`](controls/hashes.txt) (`2d4079c15993899e086a480f00ac15ee5e6dfd12a9cbaa5c89497bd75c01de2e`) and
its manifest. Every file named below is named by its SHA-256 in its folder's `hashes.txt`. Each folder's
`.gitattributes` keeps its files' bytes as committed and has git's whitespace checks skip them, as run outputs keep the
whitespace they were written with.
- **How they were taken.** Locally, from a clean `git archive` of the after commit on disk, behind the same gate and
  wrapper as the base runs, with the base runs' commands and scripts: part C by
  [`base/run-part-c.py`](base/run-part-c.py), byte for byte, and part D by the same three `dotnet test` runs, under the
  invariant culture, `tr-TR` and `lt-LT`. Both parts ran with `TZ=UTC` and under .NET SDK 10.0.404 and runtime
  Microsoft.NETCore.App 10.0.15, as the note of 2026-11-18 on the zone and the toolchain says the after runs would.
  Both builds had no warning and no error. [`after/commands.txt`](after/commands.txt) gives every command,
  [`after/archive.txt`](after/archive.txt) the commit, its parents and tree, and `git ls-remote origin refs/heads/main`
  before and after the runs (the after commit both times), and `after/logs/` each step's records. Part C ran before
  part D here, after it at the base.
- **Part A's after run is not taken here.** It is a dispatch of `.github/workflows/test262-evidence.yml` (blob
  `fc38229782ee1b2e93ecca9e990d182f5e44dbf2` at the after commit, the blob the rule's revision of 2026-11-18 admits)
  on `main` with commit `b2b226cd3158f0b7d990a3f4fecbe75b259b4fa1` and the base run's other inputs, suite_ref
  `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone
  empty and include_negative true (base run 44377139618's own record, `base/manifest.json`). This change dispatches
  nothing, and part A has no reading here.
- **Part B** is not taken here; the next note records its arm A by the owner's answer 7.

**Part C: read NOT MET.** The rule: "MET if and only if, after, every case gives its declared outcome, no process ends
abnormally and none passes the wall-clock limit; every case whose outcome differs from the base's is in C1 or C3 and is
listed; and the control is reported as stated", with the classes C1 ("a case whose outcome at the base is a wrong
answer, a no match where the specification matches, a shorter match, a flipped look-around, or an abnormal end, and
whose outcome after is its declared one"), C3 ("a case that completed at the base and after throws the RangeError or
SyntaxError its declaration names, for a limit of decisions D7 and D10") and "no other class".
- **After, every case gives its declared outcome**, no process ends abnormally and none passes the wall-clock limit:
  [`after/part-c/results.tsv`](after/part-c/results.tsv) (`e87ca5b437a016c11c97dcabc01cc4ac1f9172ae0354c975e6b3b429237da466`),
  every program checked against the manifest's SHA-256 first, every exit 0, every row "matches-declared yes".
- **The control is reported as stated**: with the adapter answering no match where Broiler.Regex reports
  `LimitExceeded` ([`controls/part-c/control.patch`](controls/part-c/control.patch),
  `902626d4af0afd644be8dfbbe7ae05735056a7b2ed831f4e453080637a14ac23`), M01 and M02, the M cases, each give "completed
  null" where their declared outcome is "threw RangeError" ([`controls/part-c/control/results.tsv`](controls/part-c/control/results.tsv),
  `3fccd4c898996e2aea98a3c18a1baaaf7de469797286dd2c87e2c78a8527f096`). Reverted, the file is the after commit's blob and
  the copy matches a clean archive but for `bin/` and `obj/` (`controls/part-c/revert-check.txt`,
  `6d3051877e3e183f164f2dfeee7e0e47722327932c695f1cda853dc5a233487c`), and the reverted build gives both their
  declared outcome (`controls/part-c/reverted/results.tsv`,
  `bc01a3374630f855142bf04d7dfae3e704aab0e52e2014e5c3a83c0d5a392a72`).
- **Against the base** ([`base/part-c/results.tsv`](base/part-c/results.tsv),
  `710aa4f2296fcc8643adb6b0bb6125a9ed222ce149f81a4177cb73d2c6eb761c`), ten cases' outcomes differ, the ten the base-run
  note names; every other case gives the same outcome at both:

  | Case | At the base | After (its declared outcome) | Class |
  | --- | --- | --- | --- |
  | M01, M02 | completed, with an answer | threw RangeError | C3: completed at the base, after the RangeError its declaration names, a frame or trail ceiling (decisions D7 and D10) |
  | N02, N04, N08, N10, N11, N14, N16 | completed null | threw SyntaxError | C3: completed at the base, after the SyntaxError its declaration names, the nesting limit (decisions D7 and D10) |
  | N05 | past the wall-clock limit | threw SyntaxError | none: it did not complete at the base, so it is not C3; and its base outcome is none of C1's |

  No case is in C1. **N05 is in no class.** The rule's configuration names a process's outcomes as "completed with its
  answer, a thrown error with its constructor, an abnormal end, or past the wall-clock limit", four outcomes, and its
  MET line keeps the last two apart ("no process ends abnormally and none passes the wall-clock limit"), so a case past
  the wall-clock limit at the base is not C1's "an abnormal end"; and C3 asks that the case "completed at the base". The
  rule admits "no other class", so "every case whose outcome differs from the base's is in C1 or C3" does not hold for
  N05, and part C read NOT MET, on that case alone. N05 is a case of class N, the nesting the rule's "How it is read"
  places under rows S3 and S3b ("after, they throw the SyntaxError of the nesting limit, under C3 or under C1 with row
  S3b"). **For the owner:** whether a case past the wall-clock limit at the base is read under C1 is the owner's; this
  note does not read it, and a revision that admitted it now would be written after these runs were seen, which the
  rule's section "Revisions" calls weaker evidence.

**Part D: read MET on the retained runs.** The rule: "MET if and only if the three after reports are byte-identical
and every vector's result after is the outcome its class expects (normative: out; impl-defined: per-engine.broiler-js,
row M4's empty match among them by JSD-0081 decision D41; a divergence.broiler-js field that the cut-over answers
reports that it no longer holds)", over the population its note of 2026-11-01 gives ("the non-status vectors", "The
three after reports are compared byte for byte over them") at corpus revision 32, by the owner's answer 5.
- **The reports**: [`after/part-d/`](after/part-d/), `run-invariant.tsv.gz`
  (`7bc9bde2dae7fe9b3f9b4eab58448f8dbad4d6936bd7045c25eac24a39261398`), `run-tr-TR.tsv.gz`
  (`ff13bce63804984a3e6d297e5795618f28a3665c53d55bdb47416e1b45179cae`) and `run-lt-LT.tsv.gz`
  (`31cb3079873eb3d36c2c72a6fb4424d414d9d739b4868cb7229681e015dfd603`), each the whole corpus at the tree's revision
  43, with exits 0, 1 and 1, as at the base: every line that differs between the cultures is an `intl/` vector,
  outside part D's population ([`after/part-d/reading.txt`](after/part-d/reading.txt), which reads them at revision 43
  as [`base/part-d/reading.txt`](base/part-d/reading.txt) does, and is not part D's reading). Each run's negative
  control is byte for byte the base's.
- **The reading at revision 32**: [`after/part-d/reading-revision-32.txt`](after/part-d/reading-revision-32.txt)
  (`1892820387a2e0191a79876bed628d459adf6f2c31809d26677a1bf1abf26bdc`), written by
  [`base/part-d/read-revision-32.py`](base/part-d/read-revision-32.py) with `--runs after --tree-commit
  b2b226cd3158f0b7d990a3f4fecbe75b259b4fa1`, as the note of 2026-11-19 on part D's base says the after runs would be
  read, run twice into two directories with both outputs byte for byte the same, through the scratch pin
  [`after/part-d/revision-32.pin`](after/part-d/revision-32.pin)
  (`35fad111993a5eafc170eeb125e71d6413686b93f1f24db2ba17f6c5109c534a`), byte for byte
  [`base/part-d/revision-32.pin`](base/part-d/revision-32.pin). The after commit's `regexp/` vectors are revision
  32's in every field but `class` and `divergence.*`, with the same expected outcome for `broiler-js`, so the script
  did not stop.
- **Byte-identical**: in each of the three reports, 1,402 `regexp/` lines, one for each of revision 32's non-status
  vectors and none for the 18 status vectors; read again at revision 32 the three are byte-identical (each set's
  SHA-256 `f8c5eb26fd8c9308b06989c015823951bc20353a196f1d5a68f4ecf5759f143c`), as they are as written.
- **Every vector's outcome is its class's**: every line is a match, none a classified divergence, a difference or
  not implemented: every `normative` vector's result is its `out`, every `impl-defined` vector's its
  `per-engine.broiler-js`, and every `divergence` vector's its `out`; and every `divergence.broiler-js` field of
  revision 32 reports that it no longer holds, 189 in each report (the 169 that revision 43 still carries, and row
  M2a's 20, which revision 39 retired). The two `s9b-literal-grammar` vectors, `divergence` at revision 32 with no `divergence.broiler-js` field,
  match.
- **"How it is read"**, "the after runs must not" show row M2a's culture dependence: they do not; row M2a's vectors
  match under all three cultures, as at the base.

**Part E: read MET on the retained runs.** The rule: "MET if and only if E-1 to E-4 find nothing outside their
exceptions and the control is reported."
- **E-1**, [`after/part-e/e1.txt`](after/part-e/e1.txt) (`e97447ccbaa4e1719b13b0549cda0b55db0e6f54cbfcd006c313c139ebe54534`):
  `git grep -w -n` at the after commit over `Broiler.JS/` but `CHANGELOG.md`, one name at a time. No name but
  `HexValue` has a hit, and `JSRegExp.UnicodeSets.cs` is absent. `HexValue`'s hits are the rule's exception "a hit the
  bundle lists with the declaration that shows it is another type's member of the same name": the declaration
  `internal static int HexValue(this char ch)` of `CharExtensions`, `Broiler.JS/Broiler.JavaScript.Parser/CharExtensions.cs:17`,
  an extension the scanner calls for its string and identifier escapes (`FastScanner.cs:975` in `ScanHexEscape`,
  `:1020` in `ScanUnicodeCodePointEscape`, `:1669` and `:1690` in `ReadIdentifierEscape`), not the translator's
  `private static int HexValue(char c)` of `JSRegExp` (`JSRegExp.cs:4966` at the base commit), which is gone.
  `RegExpUnicodeValidator`, the forwarder the list keeps (decision D21, SJL-D-25 (a)), is no hit of a word search for
  any name above; its body, `Broiler.JS/Broiler.JavaScript.Ast/RegExpUnicodeValidator.cs:37-39`, is one call of
  `BroilerRegex.Validate`.
- **E-2**, [`after/part-e/e2.txt`](after/part-e/e2.txt) (`e073b0c6d9250750271d8fcdde6a49bfbed8e044d4b03e5fbfd20c3a80104349`):
  the metadata reader in [`after/part-e/e2check/`](after/part-e/e2check/) (System.Reflection.Metadata; it runs no code
  of what it reads) over the after commit's Release assemblies, by SHA-256 in
  [`after/part-e/assemblies.sha256`](after/part-e/assemblies.sha256). It reads every TypeRef and MemberRef, and every
  type's and member's signatures, IL operand tokens, local signatures and attributes, widely enough that it can only
  over-report. The Parser and Ast assemblies hold no reference into `System.Text.RegularExpressions`; the BuiltIns
  assembly holds none at all, so none from the types of `Broiler.JavaScript.BuiltIns.RegExp` or from JSStringPrototype's
  `search`, `match`, `matchAll`, `replace`, `replaceAll` and `split` members (in metadata the members `Search`, `Match`,
  `MatchAll`, `Replace`, `ReplaceAll` and `Split` of the partial class `Broiler.JavaScript.BuiltIns.String.JSString`,
  which `JSStringPrototype*.cs` declare); and in the Runtime assembly `LegacyRegExpState` holds none, the assembly's
  references being `DateParser`'s alone. A first run of the reader looked for JSStringPrototype's members under a type
  of that name, which metadata does not have; it was corrected and run again, and only the second run is retained
  (`after/commands.txt`). Both found no such reference in the BuiltIns assembly.
- **E-3**, [`after/part-e/sources.txt`](after/part-e/sources.txt) (`6c53a91e5723113b78328f2f808650d0910d8cdf6b56197d4d872f490bf7e0b7`),
  which gives every line read here from git at the after commit: `FastScanner.IsValidRegularExpressionLiteral`
  (`Broiler.JS/Broiler.JavaScript.Parser/FastScanner.cs:1048-1050`) is one call of `Broiler.Regex.BroilerRegex.Validate`,
  and the regular-expression scan calls it (`:1363`); `DecodeBracedUnicodeEscapes`, `TryDecodeBracedUnicodeEscapes`
  and the parenthesis-balance patch (its `sawBracedUnicodeEscape` mark and its depth count, `FastScanner.cs:1442` and
  `:1495-1496` at the base commit) are absent; and the division fallback stays: a scan that fails is read as `/` or
  `/=` (`:1218-1234`).
- **E-4**, the same file: JSStringPrototype's `Search` (`Broiler.JS/Broiler.JavaScript.BuiltIns/String/JSStringPrototype.Search.cs:185-218`)
  reaches a regular expression only through `new JSRegExp(search.StringValue, "")` (`:212`, step 4's RegExpCreate) and
  an Invoke of `@@search` (`:213` and `:217`), with no call of a `JSRegExp` member that matches; E-2's reader finds
  the constructor the only `Broiler.JavaScript.BuiltIns.RegExp` member `JSString::Search` names.
- **The control is reported**: with `JSRegExp`'s `BuildFlagsString`, as the base commit has it, re-added to a scratch
  copy ([`controls/part-e/control.patch`](controls/part-e/control.patch),
  `b0a7fd233c36278cdf4ee2aed18c56f8b084dafd4cca043f628678c1c35b423b`), E-1 reports it, at `JSRegExp.cs:453` of the
  copy ([`controls/part-e/e1-control.txt`](controls/part-e/e1-control.txt),
  `3c7206ea0cc3386c1576f917c1113d0d8311bb56b04cb528db4a992b42c80c45`). Reverted, the file is the after commit's blob,
  the copy matches a clean archive, and E-1 over it is byte for byte E-1 over the clean archive
  (`controls/part-e/revert-check.txt`, `191400d682d5e8c1625ad597cec401eaa12a4685a10b90c44299de3f1863661d`;
  `e1-reverted.txt` and `e1-clean-archive.txt`, both `3fc886cf5afb5a2727ba3f365dd60421d13f03a39fc83ddc157623002e9ede2f`),
  whose lines are those of `after/part-e/e1.txt`.
- **Findings for their owners**, as the rule's "How it is read" lists .NET `Regex` outside ECMAScript matching, outside
  E-2's population and not judged: at the after commit `DateParser.cs` (the Runtime assembly's references above) and
  `FastCompiler.DeferredBody.cs` (the Compiler assembly) still use it; `JSIntl.cs` keeps a
  `using System.Text.RegularExpressions;` that no reference in the BuiltIns assembly's metadata follows; and two files
  the rule does not name use it too, the shell's REPL (`Broiler.JS/Broiler.JavaScript/REPL/InteractivePrompt.cs`) and
  the source generator (`Broiler.JS/Broiler.JavaScript.JSClassGenerator/ITypeSymbolExtensions.cs`).

These are per-part readings only. No clause of SJL-11 (4 to 7), no exit reading and nothing of Broiler.VM's or
Broiler.RegEx's is read here; part A and part B have no reading; nothing is declared met beyond the words above.

*Dated note, 2026-11-19, part B's arm A by the owner's answer 7; the note above "Part B's arm A, for the owner" is not
rewritten.* It reads "How arm A is built is the owner's open question; it is not built until the owner's reading is
recorded here by a dated note." The owner answered on 2026-11-19, an answer of the owner's own, not taken under the
owner's direction of 2026-10-18, quoted verbatim and numbered as the answers above continue:
- **7.** Asked (2026-11-19): "SJL-11.5 part B's arm A is "the after commit, with the same two files"
  (RegExpBenchmarks.cs and the benchmark Program.cs "as the inputs commit has them"). RegExpBenchmarks.cs is unchanged
  at the merge. But the inputs commit's Program.cs calls the RegexTiering metrics that the cut-over deletes (D20/D21),
  so it cannot build at the merge. How should arm A be built?" Chosen: "Merge's own Program.cs" — "A dated revision,
  filed before any part B run, reads arm A's Program.cs as the merge's own. That file differs from the inputs commit's
  only by the removed --regex-tiering block, which part B never invokes. Its --regexp-answers path and the benchmark
  entry must be byte-identical; if they aren't, the run stops and comes back to you." Not taken: "Inputs file minus
  the block" — "Arm A uses the inputs commit's Program.cs with only the --regex-tiering block cut out, as a patched file
  named by hash in the manifest. It stays closer to the rule's words, but arm A then builds from a file no commit
  has."; "Record part B void" — "No part B run. The rule then leaves the measurement question open for this engine. It
  blocks nothing."

What it means here:
- **The revision.** [`decision-rule-revised-2026-11-19.md`](decision-rule-revised-2026-11-19.md), filed by
  `cb0246d63699030cdedf972524811fc2de2fc4e2` before any part B run, quotes `decision-rule.md` whole, as the earlier
  revisions do, and changes only part B's arms line: "A the after commit, with the same two files" reads "A the after
  commit, with RegExpBenchmarks.cs as the inputs commit has it and the benchmark Program.cs as the after commit has
  it", and the part B run refuses unless, before any lane runs, the after commit's `Program.cs` differs from the
  inputs commit's (`24fe7cba`, blob `6074646340c64176f4c918b1c963dbd1347cdcff`) only by the removal of the
  `--regex-tiering` block, with its `--regexp-answers` path and the benchmark entry byte-identical. Arm B, A-again,
  the configuration, the condition, the configuration check, the quantity, the noise floor, the comparison and the MET
  line are unchanged. It says it was written after parts A, C and D's base runs and part C's after run had been seen,
  with part D's after runs under way, none of them part B's, so its decision is weaker evidence in the sense of the
  rule's section "Revisions".
  `decision-rule.md` and the earlier revisions are not edited; the four are read together.
- **The condition holds at the after commit.** [`after/part-b-arm-a/check.txt`](after/part-b-arm-a/check.txt)
  (`4ed579dbe4ccd749c98b1cbf5787eae8b8ca0a45ffee516972a1f40061b23549`), written by
  [`after/part-b-arm-a/check-program-cs.py`](after/part-b-arm-a/check-program-cs.py)
  (`96456659d7c32b2cb51ee62cdd02f96060c3ab50a8d807c161bc5ada119fa7c7`) from git objects alone, reads: the inputs
  commit's `Program.cs` is blob `6074646340c64176f4c918b1c963dbd1347cdcff`; with its one `--regex-tiering` block
  removed (its lines 59 to 64, the `if` statement that calls `RegexTieringMetrics.Write()` and the blank line after
  it) it is, byte for byte, the after commit's `Program.cs`, blob `095db42675307de05e23cd22ba5c2b8e24c3241a`; the
  `--regexp-answers` block and the benchmark entry (`BenchmarkSwitcher` to `.Run(args,
  BenchmarkConfiguration.Create());`) are byte-identical in both; and `RegExpBenchmarks.cs` is blob
  `604dced1d4a4485e7fa01cac55630b21c0c72f59` at both, so arm A builds the after commit's own tree, with no file
  replaced. The local session runs the same check again before any lane runs, and keeps its output with the
  sub-bundle's manifest.
- **Part B is still to be taken**, on the controlled lane `win-x64-r7-5800x` by a local session on the owner's
  workstation (answer 4 above), into `measurement/win-x64-r7-5800x/`. No arm has been built and nothing has been
  measured; part B has no reading.

Nothing here is a verdict.

*Dated note, 2026-11-19, later the same day, what the readings above rest on where they did not say, and four records
they leave out; the notes above are not rewritten, no retained file changes and nothing is run again.*
- **Part C's class C3, the evidence for each entry.** The table above reads, for M01 and M02, "C3: completed at the
  base, after the RangeError its declaration names, a frame or trail ceiling (decisions D7 and D10)" and, for N02, N04,
  N08, N10, N11, N14 and N16, "C3: completed at the base, after the SyntaxError its declaration names, the nesting limit
  (decisions D7 and D10)", without naming the files that show which limit was reached. The retained outputs record only
  the error's constructor (`after/part-c/cases/M01.out.txt`: "adversarial-outcome M01 threw RangeError"; the programs
  print no message), so each entry rests on the following, all at the after commit, in
  `Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.Broiler.cs` (blob `6bee5b8217f31b9c332de2eb4e5e5af8537a470a`,
  SHA-256 `949c1c87f415febb6d7b554cb969642214537615c4a316f0674fcf355285c5e6`, the value
  `controls/part-c/revert-check.txt` gives):
  - **M01 and M02, on the control.** The adapter's match throws a RangeError only where Broiler.Regex reports
    `LimitExceeded`, naming the trail or the frames (`:147-150`, under the ceilings `:28-29` passes, the policy `:19-21`
    names as decisions D7 and D10). The control patch replaces exactly that throw with a no match
    ([`controls/part-c/control.patch`](controls/part-c/control.patch),
    `902626d4af0afd644be8dfbbe7ae05735056a7b2ed831f4e453080637a14ac23`), and with it both cases give "completed null"
    (`controls/part-c/control/results.tsv`, `3fccd4c898996e2aea98a3c18a1baaaf7de469797286dd2c87e2c78a8527f096`);
    reverted, both throw their RangeError again (`controls/part-c/reverted/results.tsv`,
    `bc01a3374630f855142bf04d7dfae3e704aab0e52e2014e5c3a83c0d5a392a72`). So each RangeError is a ceiling Broiler.Regex
    reported. Which of the two, frames or trail, nothing retained shows; the class names both.
  - **The seven N cases, on the source and the cases' depths.** The adapter compiles every pattern under `NestingLimit`
    (`:25`, `BroilerRegex.MaximumNestingLimit`; `:24` reads "511 nested groups compile, 512 are refused") and throws a
    SyntaxError for every compile status but success (`:55-58`), so the source alone does not tell the nesting limit
    from another refusal. The cases do. By their programs in [`inputs/adversarial/`](inputs/adversarial/), each run only
    after its SHA-256 matched the manifest's, N02, N08 and N14 nest 512 capturing groups, non-capturing groups and
    `v`-mode classes; N04, N10 and N16 nest 20,000 of the same; and N11 nests 1,000,000 non-capturing groups. Each
    pattern is otherwise that of N01, N07 or N13, which nest 511 of the same kind and give their declared answers after
    (`after/part-c/results.tsv`, `e87ca5b437a016c11c97dcabc01cc4ac1f9172ae0354c975e6b3b429237da466`). The flags differ
    only by `g`, which the grammar does not read. A pattern that differs from one that compiles only by deeper nesting
    of the same kind is refused for its depth, so each of the seven SyntaxErrors is the nesting limit. No retained file
    shows an error's message.

  This is the session's reading of those files. It changes no class and no reading: part C read NOT MET, on N05, stands.
  If the owner admits N05 under C1, part C's MET rests on these nine C3 entries as evidenced here.
- **Part B's check: the blank line.** The revision of 2026-11-19 defines the block as "the `if` statement whose
  condition is `args.Length == 1 && args[0] == "--regex-tiering"`, which calls `RegexTieringMetrics.Write()`, and
  nothing else". The difference the check accepts removes that statement and the blank line after it
  (`after/part-b-arm-a/check.txt`: "the block: inputs lines 59 to 64 (6 lines, the last blank)"). The check reads that
  blank line, which only separates the statement from the next, as part of the block, under answer 7's "only by the
  removed --regex-tiering block". Read the other way, the two files would differ by one blank line besides the block,
  and the part B run would refuse. The part B local session is told to apply the same reading, or the owner's if the
  owner rules otherwise first. The revision is not edited.
- **The host's kernel build.** `after/commands.txt` reads "Host: Ubuntu 24.04.5 LTS; Linux 6.18.44 x86_64; … .NET SDK
  10.0.404, runtime Microsoft.NETCore.App 10.0.15, as at the base runs". The machine records name the kernel's build:
  `Linux 6.18.44-fc-v595 x86_64` in every record in `base/logs/`, and `Linux 6.18.44-fc-v653 x86_64` in every record in
  `after/logs/` and `controls/logs/`. Apart from the time, the load, the free disk and the process list, the base and
  after records of each step differ in that line alone. The rule's parts C and D fix the zone, the cultures and the
  toolchain, not the kernel, so no clause turns on it. "As at the base runs" holds up to the kernel's build.
- **Part C's reverted control build's binaries.** `controls/part-c/reverted-binaries.sha256` gives
  `Broiler.JavaScript.BuiltIns.dll` `deef16298400d2ef57aa100ffabf319ceb76bd759c170aaa00cdf4857f9b851f` and
  `BroilerJS.dll` `b7c0b58f229b621c0669e927ffa7d479f1d0047d3f34605d5537145c6a51bef5`, and `after/shell-binaries.sha256`
  gives `d88190432fed92d9cccfda10eb69528a4119a775c0c9e295a47902c438a2d6da` and
  `abfeec98243586254a6a8df13e660b00afd14d14955507a5a11426369808e388`. The two are not expected to be equal, and the
  control does not rest on them. It rests on the revert's check at source level (the file's SHA-256 as above, and the
  whole copy equal to a clean archive but for `bin/` and `obj/`) and on the M cases' outcomes. The after build ran in
  its own scratch directory and the control's builds in another (`after/part-c/cases/M01.out.txt` and
  `controls/logs/ctl-c-build.out.txt` name both). Builds made off CI keep their build directory's paths in the
  assemblies: the repository sets `ContinuousIntegrationBuild` only on CI (`eng/Broiler.Packaging.props:80`) and sets no
  `PathMap`. That is the most likely cause. It was not confirmed by building twice in one directory. `BroilerJS.dll` is
  the same in the control's build and the reverted build, both made in the control's directory, the second
  incrementally.
- **Part E's first E-2 run.** The run that `after/commands.txt` says was discarded is not retained in the bundle. Its
  output, the wrapper's standard output of the first `e2-run` in `controls/logs/runs.tsv`, has SHA-256
  `0020699221e06148e15c9ea6fd90113efe1e70ed326368c7addb4a34708e8cd5` and is kept only in the session's scratch. It
  differs from `after/part-e/e2.txt` in two places only. The first is the BuiltIns assembly's E-2 scope line, whose
  `found:` list was empty, under the type name `Broiler.JavaScript.BuiltIns.String.JSStringPrototype`. The second is the
  E-4 support lines. In both runs every assembly's TypeRefs and MemberRefs into `System.Text.RegularExpressions` are the
  same, none in the BuiltIns assembly. The first version of the tool was corrected in place and is not kept; only the
  version in [`after/part-e/e2check/`](after/part-e/e2check/) is.

Part A's after run is still not taken and part A has no reading, as the note above says. Nothing here is a verdict.

*Dated note, 2026-11-19, part A's after run and its reading; the notes above are not rewritten.* The note of the
after runs reads "**Part A's after run is not taken here.** It is a dispatch of `.github/workflows/test262-evidence.yml`
... This change dispatches nothing, and part A has no reading here.", and the note above ends "Part A's after run is
still not taken and part A has no reading, as the note above says." The run has been taken, and is read here.
- **The run.** `.github/workflows/test262-evidence.yml` run
  [44496119000](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/44496119000) (attempt 1), dispatched on
  2026-11-19 at 09:24:27Z on `main` with commit `b2b226cd3158f0b7d990a3f4fecbe75b259b4fa1`, suite_ref
  `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone
  empty and include_negative true, base run 44377139618's inputs but for the commit (its own `run.txt`'s inputs line;
  time_zone empty is not echoed there, as at the base). This change dispatched nothing: the run was dispatched
  outside it. Its head commit, its `run.txt`'s dispatched ref and tested commit all read `b2b226cd`; it completed at
  10:30:50Z, and its conclusion is success, and so is every job's: prepare 135463615714, shard (0) 135463766058,
  shard (1) 135463766082, shard (2) 135463766213, shard (3) 135463766128, shard (4) 135463766081, shard (5)
  135463766184, shard (6) 135463766194, shard (7) 135463766222 and collect 135497106809, the step "Require a complete
  run" among collect's; the steps skipped are those the base run skipped (GitHub's record of the run and its jobs,
  read 2026-11-19T10:34Z). The workflow is blob `fc38229782ee1b2e93ecca9e990d182f5e44dbf2` at the after commit, the
  blob the rule's revision of 2026-11-18 admits and the base run ran, and nothing under `.github/` or `scripts/`
  differs between the base commit and the after commit (`git diff 5429c406 b2b226cd -- .github/ scripts/` is empty).
- **Retained** in [`after/part-a/`](after/part-a/imported.txt) as `base/part-a/` retains the base run: every file of
  the artifact `test262-evidence` (id 14520196133), 26 entries, each compared with its entry in the zip, unchanged;
  the zip's SHA-256, `2b641877918f2e3dd8e1020df5d01659fb2ea67d0d51ce42ef21411218c04185`, equals the digest GitHub
  records for it, and the zip is named by that digest and not committed. [`imported.txt`](after/part-a/imported.txt)
  (`9075235084696661520ddaa12d3b2f48a2f1cb418a46c610fe8bf5932f391e5c`), `runner-images.txt` (copied from each job's
  own log by the procedure that wrote the base's, which it reproduces byte for byte from the base's logs) and
  `part-a-artifacts.txt` (`1fa09419d85457c0445bb5393c95680f7cb310f52860de9aaeac21bd013d2fab`; the run's 18
  artifacts, each downloaded and equal to GitHub's digest) are added, with
  [`after/part-a/hashes.txt`](after/part-a/hashes.txt) (`c7bdadebb9f7dcbdbbe061e6f504656e2aeb16f31c9192e4e68508a07f441cd7`)
  over the folder and `comparison/`, the rule's comparison. [`after/hashes.txt`](after/hashes.txt), now
  `8aa93fc70d1a711a370cf48508756c5d947edd47748b1a05a70e00b80184ed9c`, and `after/manifest.json` list them; no file they
  listed changes. The manifest's `runs.partA` read "not taken here: its after run is a dispatch of
  .github/workflows/test262-evidence.yml at the after commit with the base run's inputs, which this change does not
  make"; it now records the run, and keeps that value under a key that says it is replaced.
- **Its runners.** Every job ran on `ubuntu-24.04`, on three image versions, as at the base: 20261116.133.1 for shards
  0, 2 and 7, 20261111.127.1 for shards 1 and 6, and 20261111.370.1 for shards 3, 4 and 5 and collect
  (`runner-images.txt`); `run.txt` reads "ubuntu24 20261111.370.1; Linux X64; logical processors 4" and dotnetVersion
  11.0.100, as the base's does. The rule fixes the shell and the zone, which the records show, and not the image version.

**Part A: read MET on the retained runs.** The rule, with its revisions of 2026-11-17 and 2026-11-18 (the workflow
admitted as `fc382297`, and part A's list read with rows S12 to S15, M8 to M27 and row M28 in its two halves): "MET if
and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision, one
shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, line for
line, the script finds them comparable, every duplicate-named-groups row passes at both commits, no row passing at the
base fails, times out or is skipped after outside C3, of either kind, and every changed verdict is in C1, C3 or C5 and
is listed." The comparison is `scripts/compliance/compare_test262_paths.py`, blob
`e951dd478a858ac06c4178c28164325b06506d00` at the rule commit, the base commit and the after commit (committed at
SJL-0.9), over `base/part-a/test262-results.json.gz` (`f298d8b098c3ed2568254e51c894fa4df31b5057ca7a6e8e6c0f3efb99be5c64`)
and `after/part-a/test262-results.json.gz` (`805c8609a5cfc43d1cb6c6eeb9340742714e881d481791519b890ba10a20980f`),
decompressed, each run's timed-out rows replaced by its re-run (`--base-rerun base/part-a/rerun-timeouts.json`,
`cd39ed5336b188e1e46a1abefa63d14f9ce714289cc6c01f7e798d9904648450`; `--after-rerun after/part-a/rerun-timeouts.json`,
`1a8e6ff9d77b1ca9368b10f5ecc3c96579174ea1938ea2ec6f7dcc686f108a49`), with the classification file beside its report,
[`comparison/classifications.json`](after/part-a/comparison/classifications.json)
(`26e886506c4abde1469df7819cf46f95978fced53cfa36450b2b50b592b06d67`). The script decides no class; the classification
file is this note's, one entry per changed row, written only after a first look at the comparison without it, which
reported the same rows, every one unclassified, and is not retained.

| Part A's condition | The retained runs |
| --- | --- |
| Both runs complete: every expected shard reported, no configuration failure, one suite revision | Eight of eight expected shards reported in each run, none incomplete and none retried, every status file phase `full`, exit code 1, for failing tests; no configuration failure; `ccaac100` alone in each ([`comparison/completeness.log`](after/part-a/comparison/completeness.log), `db53c9e3883f1464c9a54abb2e22e777fc65ab6b1c2beed43bad824c29b54a9a`, read from each run's own files; `after/part-a/merge.log`, `b929864c7416568c160e44d35763a6cdf4e1055c4cd1a2c73158c7ec4924bcdb`) |
| One shell | In each run every shard and the re-run hash the shell's binaries alike: one distinct list, `4ff7c231…` after and `4d744985…` at the base (`after/part-a/shell-hashes.log`, `725446de8b7230688f26b43d3155bab67e0ae39a6ff1d7ffb46d4f541d29cdae`); the two differ, as they must, each built from its run's commit |
| Every shard in the zone; `includeNegative` true in both | All eight in the runner's zone, UTC, in each run; `includeNegative` true in `run.txt`, the merged results and the re-run, in each (`completeness.log`) |
| The two runs' host-zone records read alike, line for line | Yes, and byte for byte: `after/part-a/host-zone.log` and `base/part-a/host-zone.log` are both `b4cebfcad5e8d2108521538f2033a51275353b258f445f7cdd0240af148d736b`, `TZ` unset, UTC +0000 at both instants and tzdata 2026e ([`comparison/zone-records.log`](after/part-a/comparison/zone-records.log), `8892a8cde0d20d8f29dadafd80016f32671c174bf1b4512f17c0af052ed6110b`); so the rule's clause for a moved record does not apply |
| The script finds them comparable | Yes, exit 0 ([`comparison/comparison.log`](after/part-a/comparison/comparison.log), `e1277dfe176393c0f5e8b92eab3d7429b9e73d9db91801407630944bd39f1303`; [`comparison/comparison.json`](after/part-a/comparison/comparison.json), `d481c1d733be59a2a877edcbdac0edda65b8344c670262193d835f22a13fa3a6`). Its checks ([`comparison/compare-checks.log`](after/part-a/comparison/compare-checks.log), `f631ac1bb56a069cc831186deeffb54d73c31f82f94aa3e6d3755e3164094611`): the after run against itself changes nothing, and a copy with one passing row turned to failed is one regression, exit 1. The fields it records and does not compare are the same in both runs |
| Every duplicate-named-groups row passes at both commits | Yes: every row whose features, the runner's reading of the file's front matter, list `regexp-duplicate-named-groups` passed at both, in both variants ([`comparison/duplicate-named-groups.log`](after/part-a/comparison/duplicate-named-groups.log), `9927506a56f0e4bf983d392e6afceaf81ecdb4644f906b7ac5f9235b2c505593`) |
| No row passing at the base fails, times out or is skipped after outside C3 | None: no row passing at the base changed; the script reports no regression |
| Every changed verdict in C1, C3 or C5, and listed | Yes: every changed row is a C1 row the rule's list names with its file, listed below and in the classification file; the script reports every one admitted and none unclassified, and no classification unused |

**Timeouts and their re-runs.** As at the base, one path timed out in both variants,
`test/staging/sm/regress/regress-1507322-deep-weakmap.js` (`after/part-a/timed-out-paths.txt`, the base's byte for
byte, `2eed8d54df5970a25649d4edd4705ff69fae80b8c2356465f8822c304c289a68`); the workflow ran it again alone, with one
worker, at the same commit and with the same shell, and it timed out again in both variants, as at the base. The
script took each run's re-run and replaced the two rows on each side, so they compare timed out against timed out
(the report's `replacedByRerun`). Both the runs and the re-runs are retained.

**The C1 table.** Every changed row moves from failed at the base to passed after, in the variant `original`; each is
a negative-metadata test whose literal the base's scanner accepted, so the statement the test forbids ran ("negative
test expected SyntaxError but got different error"), and after is the early `SyntaxError` the test expects, the
scanner checking every literal with Broiler.Regex's validate entry (decision D22). The `terser` variant of each is
skipped in both runs, "parse, early, and resolution negative tests cannot be minified before execution", and does not
change. Each is listed in `Broiler.JS/CHANGELOG.md` by its row, under "The literal check".

| Row | Paths (each `original`, failed at the base, passed after) | Correction |
| --- | --- | --- |
| S7 | `test/language/literals/regexp/invalid-optional-lookbehind.js`, `invalid-optional-negative-lookbehind.js`, `invalid-range-lookbehind.js` and `invalid-range-negative-lookbehind.js` | a quantified look-behind accepted without `u`; Annex B's grammar admits a quantifier after a look-ahead only |
| S8 | `test/built-ins/RegExp/prototype/unicodeSets/breaking-change-from-u-to-v-01.js` to `breaking-change-from-u-to-v-28.js` but `-03.js` | the `v` flag's reserved syntax accepted |
| S11 | `test/language/literals/regexp/early-err-modifiers-code-point-repeat-i-1.js`, `-code-point-repeat-i-2.js`, `-other-code-point-uppercase-I.js`, `-should-not-case-fold-i.js`, `-should-not-case-fold-m.js` and `-should-not-case-fold-s.js`, and every `early-err-arithmetic-modifiers-` file the rule's list names for the row, from `add-remove-i.js` to `should-not-case-fold-s.js` | a literal's errors reported only when it is evaluated, not early |

The rows and the files are exactly those of the rule's list for rows S7, S8 and S11, every file it names for them and
no other (`decision-rule.md`'s list of this engine's corrections, under "The literal check"). So:
- **The files the list names moved**, each from failing at the base to passing after, as the rule's "How it is read"
  expects; none still fails after, so no correction is recorded as one SJL-11.5 did not make for want of a move.
  `breaking-change-from-u-to-v-03.js`, which the list leaves out, passes at both.
- **No row is in C3 or C5**, and no C1 row is explained by a row other than S7, S8 or S11, so no row is a finding
  against JSD-0081.
- **Rows S12 to S15, M8 to M27 and row M28** (the revisions of 2026-11-17 and 2026-11-18) explain no row: no row other
  than the table's changed. No row moves for M28's half for `IsMatch` and the public `Replace`, and **no row moves
  through the `split` fallback**, as row M28 records it or in any other way, so the revision of 2026-11-18's reading
  of that half is not reached.
- **No row of row S9**, an invalid literal read as division, changed.
- **The rows that can show no move.** The list's rows that already follow the specification at the base, through
  Broiler.Regex `0.1.0-preview.6`, move no row, as the revision of 2026-11-17 says of them ("a correction the runs
  cannot show, not a regression"); this reading names none of them as made or not made, which clause 7 reads.

These are part A's per-part reading only. No clause of SJL-11 (4 to 7), no exit reading and nothing of Broiler.VM's or
Broiler.RegEx's is read here. Part B still has no reading. Nothing is declared met beyond the words above.

*Dated note, 2026-11-19, the owner's answer 8, the rule's revision for case N05, and part C read a second time; the
notes above are not rewritten, no retained file changes and nothing is run again.* The note of the after runs reads
"**Part C: read NOT MET.**", "**N05 is in no class.**" and "**For the owner:** whether a case past the wall-clock
limit at the base is read under C1 is the owner's; this note does not read it, and a revision that admitted it now
would be written after these runs were seen, which the rule's section "Revisions" calls weaker evidence." That first
reading stays on record as it stands. The owner answered on 2026-11-19, an answer of the owner's own, not taken under
the owner's direction of 2026-10-18, quoted verbatim and numbered as the answers above continue:
- **8.** Asked (2026-11-19): "SJL-11.5 part C (the adversarial corpus) reads NOT MET on one case alone, N05. At the
  base, the old .NET-translated engine ran N05 past the 300 s wall-clock limit and it was killed. After the cut-over,
  N05 throws its declared SyntaxError, which is the outcome the corpus asks for. The rule has no class for that change:
  C1's base outcomes don't include "past the wall-clock limit", and C3 needs the case to have completed at the base.
  The other nine changed cases are C3. What should happen?" Chosen: "Admit N05 by revision" — "A dated revision admits
  exactly this change for N05: past the wall-clock limit at the base, the declared outcome after. It is recorded as
  weaker evidence, because it was written after the runs were seen. Part C is then read again on the same retained
  runs, with nothing rerun." Not taken: "Keep part C NOT MET" — "The reading stands as taken. SJL-11's part C clause
  stays unmet for Broiler.JS until a later dated choice."

What it means here:
- **The revision.** [`decision-rule-revised-2026-11-19b.md`](decision-rule-revised-2026-11-19b.md), filed by
  `a77bdb7966452266d39024ab1f3a9a6f9800c6d4`, quotes
  `decision-rule.md` whole, as the earlier revisions do, and changes only part C: it adds, after class C3, "**the
  exception for case N05** - case N05 of the manifest alone, whose outcome at the base is past the wall-clock limit
  and whose outcome after is its declared one, threw SyntaxError; listed with this revision", which is not a class
  and admits nothing else, and part C's MET line's "every case whose outcome differs from the base's is in C1 or C3 and
  is listed" reads "every case whose outcome differs from the base's is in C1 or C3, or is case N05 under its
  exception, and is listed". The classes C1 and C3, "no other class" for every other case, the wall-clock limit and
  every other part stand. It says it was written after part C's runs, its control and its first reading had been
  seen, so its decision is weaker evidence in the sense of the rule's section "Revisions". `decision-rule.md` and the
  three earlier revisions are not edited; the five are read together. Its name adds `b` to the date, the bundle's
  second revision of one day. *(Added the same day, before the revision merged:)* `d5c3e1896b6d848e7d2effe0f2064cbe526a7005`
  changed the revision's own text, before it merged and before any run, so that it holds no figure, as the rule says of
  itself; its dated note says what each place read. Its decision and the two lines it changes are as above.

**Part C, read a second time, under the revision of 2026-11-19 (b): read MET on the retained runs.** It is read on
the runs the first reading read, nothing rerun: [`base/part-c/results.tsv`](base/part-c/results.tsv)
(`710aa4f2296fcc8643adb6b0bb6125a9ed222ce149f81a4177cb73d2c6eb761c`), [`after/part-c/results.tsv`](after/part-c/results.tsv)
(`e87ca5b437a016c11c97dcabc01cc4ac1f9172ae0354c975e6b3b429237da466`) and the control in
[`controls/part-c/`](controls/part-c/). The MET line, as the revision reads it: "MET if and only if, after, every case
gives its declared outcome, no process ends abnormally and none passes the wall-clock limit; every case whose outcome
differs from the base's is in C1 or C3, or is case N05 under its exception, and is listed; and the control is reported
as stated."
- **After, every case gives its declared outcome**, no process ends abnormally and none passes the wall-clock limit:
  every program was checked against the manifest's SHA-256 first, every exit is 0 and every row reads
  "matches-declared yes", as the first reading found (`after/part-c/results.tsv`).
- **Every case whose outcome differs from the base's is admitted and listed.** The same ten cases differ, and no
  other; every other case gives the same outcome at both:

  | Case | At the base | After (its declared outcome) | Admitted by |
  | --- | --- | --- | --- |
  | M01, M02 | completed, with an answer | threw RangeError | C3, a frame or trail ceiling (decisions D7 and D10), as the first reading lists them, on the evidence the note "what the readings above rest on" gives |
  | N02, N04, N08, N10, N11, N14, N16 | completed null | threw SyntaxError | C3, the nesting limit (decisions D7 and D10), as the first reading lists them, on the same note's evidence |
  | N05 | past the wall-clock limit (`base/part-c/cases/N05.out.txt`, `0cc162d1c9d44749493ed8fd76cba1fb1ac419bcb84bc5bf43c1effde877b701`: exit -9, "timed-out: True") | threw SyntaxError (`after/part-c/cases/N05.out.txt`, `209fda347ee7fee16bd551c4052d59f30365c1b78717c49b812c8ff269c4eddd`) | the exception for case N05, `decision-rule-revised-2026-11-19b.md`; not C1 and not C3 |

  No case is in C1, as at the first reading. N05's change is exactly the one the exception names: past the
  wall-clock limit at the base, "threw SyntaxError" after. No other case was past the wall-clock limit at the base.
  N05's program nests 1,000,000 capturing groups, the kind of N01, N02 and N04, whose 511 compile after and whose 512
  and 20,000 are refused; the exception does not ask which refusal N05's SyntaxError is, and this reading does not
  class it.
- **The control is reported as stated**, as the first reading found: with the patch, M01 and M02 each give "completed
  null" where their declared outcome is "threw RangeError" ([`controls/part-c/control/results.tsv`](controls/part-c/control/results.tsv),
  `3fccd4c898996e2aea98a3c18a1baaaf7de469797286dd2c87e2c78a8527f096`); reverted and the revert checked, both give
  their declared outcome again (`controls/part-c/reverted/results.tsv`,
  `bc01a3374630f855142bf04d7dfae3e704aab0e52e2014e5c3a83c0d5a392a72`; `controls/part-c/revert-check.txt`,
  `6d3051877e3e183f164f2dfeee7e0e47722327932c695f1cda853dc5a233487c`).

This second reading rests on a revision written after the runs were seen, and is weaker evidence for that, as the
revision says. The first reading, "read NOT MET", is not withdrawn; both stand on record, the second under the
revision. These are per-part readings only. No clause of SJL-11 (4 to 7), no exit reading and nothing of Broiler.VM's
or Broiler.RegEx's is read here, and nothing is declared met beyond the words above.

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
