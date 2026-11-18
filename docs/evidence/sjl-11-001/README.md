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
