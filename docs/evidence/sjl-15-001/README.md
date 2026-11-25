# Bundle SJL-15-001: Broiler.JS on the shared semantic model

**Rules, records, probes and the `semantics` vectors' runs only. Nothing here is a verdict, and no run of the rule has been taken.**

*(Noted 2026-11-24, after part B's runs at the first model commit.)* The line above reads "Nothing here is a verdict, and
no run of the rule has been taken." The rule's first runs are now here: part B's at the first model commit `be6c4a5c` and
its first parent, in [`part-b-be6c4a5c/`](part-b-be6c4a5c/README.md), with a per-part reading limited to the model
commits taken so far (the note of 2026-11-24 on part B's runs, below); nothing here is a verdict of the milestone. The
line is left as written.

**Written:** 2026-11-17, at milestone SJL-15 (work packages SJL-15.0 and SJL-15.1, Broiler.JS's half) of the shared
JavaScript libraries programme, Broiler.VM's `docs/javascript-shared-libraries/milestones/sjl-15-static-semantics.md`.
This repository's records are `docs/roadmap/SharedLibraries.md`, sections "Static semantics (SJL-15.1)" in "Area
verdicts" and "Shared static semantics (SJL-15)", with the decisions that bind this repository by Broiler.VM's JSD-0088's
names and numbers and this repository's own decisions of 2026-11-17. Broiler.VM's half is its bundle `sjl-15-001` and
JSD-0088 (merged in Broiler-Platform/Broiler.VM#214 as `0ddc146f`, and the owner's answer to its decision D-census in
Broiler-Platform/Broiler.VM#215 as `626e4ed8`; observed). **Base:** this bundle was written from Broiler.JS `6fda6ca4`
(Broiler-Platform/Broiler.JS#1119) and Broiler.VM `5e90bfe4` (Broiler-Platform/Broiler.VM#216), each repository's
default branch on 2026-11-17.

## What it holds

| File | What |
|---|---|
| [`decision-rule.md`](decision-rule.md) | The rule: ten predeclared parts, A to J, filed before any change it judges (no model beyond the declarations walk, no model merge, no cut-over, no deletion); it holds no figure |
| [`decision-rule-revised-2026-11-17.md`](decision-rule-revised-2026-11-17.md) | The rule's first revision, written before any run it judges: a model commit's conditions on Broiler.VM's merges, what part J decides, the commit that carries the class tables and the column, and part D's input; it quotes the rule whole *(row added 2026-11-17, after review)* |
| [`inputs/closure-suites.txt`](inputs/closure-suites.txt) | Part D's population, the closure-sensitive suites, each test class with its file's digest at `6fda6ca4` and how it entered the list |
| [`dispositions.md`](dispositions.md) | SJL-15.5's deletion list member by member, and the disposition of every public type it empties (SJL-D-25), this engine's half of X6 |
| [`correspondence-broiler-js.md`](correspondence-broiler-js.md) | This engine's column of Broiler.VM's correspondence, read from code at `6fda6ca4`, each row with its class across both engines (decisions D-x2 and D-js-column), and its findings J1 to J7 |
| [`probes/`](probes/README.md) | The probes that settle the rows a reading cannot, run on both engines' default branches, every answer recorded beside the specification's |
| [`corrections.md`](corrections.md) and [`corrections/`](corrections/class-t.tsv) | The classes E and T and the corrections C1, each by row, before any run, written by [`tools/write_lists.py`](tools/write_lists.py) from the stage table and the probes |
| [`engine/`](engine/commands.txt) | The `semantics` vectors' runs: each engine's first run on corpus revision 42, with its negative control; both engines' runs on revision 43 as drafted, for the vectors it adds; and both engines' runs on revision 43, with their negative controls. Not runs of the rule *(noted 2026-11-24, at corpus revision 44: the folder also holds both engines' runs on revision 44 as drafted, for the vectors it adds, and both engines' runs on revision 44, with their negative controls; the cell is left as written)* |
| [`part-b-be6c4a5c/`](part-b-be6c4a5c/README.md) | Part B's runs at the first model commit `be6c4a5c` and at its first parent `8b1fa311`, with their comparison and the push runs at `be6c4a5c` of both workflows that run the test projects of `Broiler.JS.slnx`; runs of the rule *(row added 2026-11-24, after those runs)* |
| [`part-b-6c61879b/`](part-b-6c61879b/README.md) | Part B's runs at the second model commit `6c61879b` and at its first parent `2259fd4a`, with their comparison and the push runs at `6c61879b` of both workflows that run the test projects of `Broiler.JS.slnx`; runs of the rule *(row added 2026-11-25, after those runs)* |
| [`part-b-d87044c9/`](part-b-d87044c9/README.md) | Part B's runs at the third model commit `d87044c9` and at its first parent `77fe27aa`, with their comparison and the push runs at `d87044c9` of both workflows that run the test projects of `Broiler.JS.slnx`; runs of the rule *(row added 2026-11-25, after those runs)* |
| [`hashes.txt`](hashes.txt) | The SHA-256 of every other file of the bundle |

## What it will hold

Each named by the rule, retained when its run is taken, with the commit it ran at: part A's hosted runs at the base and
after commits; part B's identity runs at each model commit after W2; parts C, D, E, H and I's local runs; parts F and G's
runs at the library commit; part J's runs, void on a lane that is not controlled (decision D-measure); and `manifest.json`
(decision D56), written by the bundle's collector at the evidence step.

## The commits

- **The rule commit**: the last commit on this bundle's branch that changes `decision-rule.md`, and with it the merge
  that brings it into the default branch, named here when it exists. The rule is filed by that merge, before any model
  merge, and is never rewritten after it.
- **The model commits, the library commit, the base commit and the after commit**: named here when each exists, as the
  rule's section "The commits, named before they exist" defines them.

*(Noted 2026-11-17, after review, before any run of the rule.)* The rule commit was defined above as "the last commit on
this bundle's branch that changes `decision-rule.md`", which is `31ad8140`, a commit that carries none of
`corrections.md`, its tables, this engine's column or the probes, though the rule says they are "Committed in the same
change". They were added in `750630fb`, after the records and corpus revision 42, while Broiler.JS's first run on
revision 42 was being taken and before any result of any vector run had been seen. By the rule's revision of 2026-11-17
(matter 3), "the same change" is this branch's change, filed by its one merge, and **the rule commit is the last commit
on this branch that changes `decision-rule.md` or `decision-rule-revised-2026-11-17.md`**, with that merge: it carries
the rule, its revision, the class tables, the column and the probes together. The same revision says that a model commit
lands after Broiler-Platform/Broiler.VM#214 and after the Broiler.VM change of decision D-x2 have merged, each observed
and named here, rather than descending from them (matter 1).

*(Noted 2026-11-17, after review.)* Three files' prose was revised in place after the `semantics` vectors' first runs
(revision 42) had been seen: `corrections.md`, `correspondence-broiler-js.md` and `probes/README.md`, each in
`1f1f2bab` and again after review. Each holds a dated note quoting what was replaced. The class tables under
`corrections/` are as first committed in `750630fb` but for one row of `corrections.tsv`, C1's row of probe
`V046-EA0006`, moved from EA0006 to EA0007 after review, when the probe was found to test EA0007's step
(`corrections.md`'s note); no run of the rule had been taken. The `semantics` vectors do not yet hold the eval-code
inputs of probes V029, V030 and V046, which the probes show and revision 43 did not carry: they are next, by decision
D-js-revisions' note of 2026-11-17 in `docs/roadmap/SharedLibraries.md`.

**Owner:** the Broiler.JS maintainer. One person holds every role (the programme's EX-30, recorded in Broiler.VM), and
nothing here has been read by anyone but its author.

**What this repository's half leaves to Broiler.VM.** The column and the deletion list reach Broiler.VM's bundle
`sjl-15-001` and JSD-0088 by a dated note in a Broiler.VM change of its own (decision D-x2), which this change does not
make; until it merges, JSD-0088 records no row's class across both engines, and no SJL-15.2 code merges.

*(Noted 2026-11-18, before any run of the rule and before any SJL-15.2 code.)* The paragraph above, after its lead-in,
reads "The column and the deletion list reach Broiler.VM's bundle `sjl-15-001` and JSD-0088 by a dated note in a
Broiler.VM change of its own (decision D-x2), which this change does not make; until it merges, JSD-0088 records no
row's class across both engines, and no SJL-15.2 code merges." That change has merged:
**Broiler-Platform/Broiler.VM#217, merged as `450c785fd7773bf761c8bd857f9e4bc3dedd9869`** (first parent `5e90bfe4`) on
Broiler.VM's default branch, observed in Broiler.VM's history at `32b0fad0`. It fills the column "Broiler.JS" of
Broiler.VM's bundle `sjl-15-001`'s `correspondence.md` from this bundle's
[`correspondence-broiler-js.md`](correspondence-broiler-js.md), by the dated note of 2026-11-17 in Broiler.VM's
`correspondence.md`, and adds JSD-0088's dated note completing X2 and X6, which reads "No row's class is unknown" and
"Both engines' halves of X2 and X6 are then filed; this note meets no clause and moves no gate." This bundle's own
change had merged before it, in Broiler-Platform/Broiler.JS#1120 as `e4e45d38`.

**What that merge unblocks, by the rule's words.** The rule's revision of 2026-11-17, matter 1, says of a model commit
that "**it lands after** Broiler.VM's SJL-15.1 rule has merged (Broiler-Platform/Broiler.VM#214, `0ddc146f`,
observed) **and after** the Broiler.VM change that carries this engine's column and JSD-0088's dated note completing X2
and X6 has merged (decision D-x2), each merge observed and named, with its commit, in this bundle's README before any
run at the model commit is taken." The first merge is named in this README's opening paragraph; this note names the
second, with its commit. A merge of SJL-15.2 that lands from now on lands after both, and the paragraph above's "until
it merges" has passed.

**What it does not unblock.** It lets no SJL-15.2 code merge by itself. The eval-code inputs come first: this README's
note above reads "The `semantics` vectors do not yet hold the eval-code inputs of probes V029, V030 and V046, which the
probes show and revision 43 did not carry: they are next, by decision D-js-revisions' note of 2026-11-17", and that
note, in `docs/roadmap/SharedLibraries.md`, puts them in "a later corpus revision of the area, classified on both
engines' runs as revision 43's additions were, before any SJL-15.2 code merges". Broiler.VM's bundle `sjl-15-001`'s
README, as Broiler-Platform/Broiler.VM#217 left it, lists the same in "What waits, in order": "Broiler.JS's later
corpus revision with the eval-code inputs of probes V029, V030 and V046, classified on both engines' runs, before any
SJL-15.2 code". No such revision exists yet: `conformance/vectors/REVISIONS.md` ends at revision 43. SJL-11.5's hold
binds as well: the rule reads "None lands while SJL-11.5's bases are held (bundle `sjl-11-001`'s README, decision
D16)", and its revision leaves it: "The rest of the definition (W2, the dark windows of bundle `sjl-14-001`, SJL-11.5's
hold) is unchanged." No SJL-15.2 merge lands while those bases are held. This note names a merge and judges nothing;
the paragraph above is left as written.

*(Noted 2026-11-24, at corpus revision 44, before any SJL-15.2 code.)* The note above reads "No such revision exists
yet: `conformance/vectors/REVISIONS.md` ends at revision 43." This change adds it: corpus revision 44 (that file's row
44) adds the eval-code inputs of probes V029, V030 and V046 as `DirectEval` vectors, `since: 44`, and classifies both
engines' runs on them as revision 43's additions were, in `scripts/conformance/semantics-divergences.txt`: Broiler.JS's
at the default branch's engine `4f697339`, after SJL-14's W2, and Broiler.VM's at its origin/main `90e4cd24` from a
scratch pin. The runs and their commands are in [`engine/`](engine/commands.txt). Broiler.JS differs on V046's input
alone, accepting it (row DE3, a correction of Broiler.JS, C1's row EA0007); Broiler.VM differs on none of them, so the
vectors do not show the answer row AB1 of the correspondence records for Broiler.VM on probes V029 and V030, whose
sources ran as an indirect eval's code. The revision lands by this change's merge. It is no run of the rule, lets no
SJL-15.2 code merge by itself and judges nothing; the note above is left as written.

*(Noted 2026-11-24, at SJL-15.2 stage 1, written before any run of the rule.)* **The first model commit.** SJL-15.2's
stage 1, the model's foundation, changes `Broiler.ECMAScript/Broiler.ECMAScript.Frontend/` and
`Broiler.ECMAScript/Broiler.ECMAScript.Frontend.Tests/`, among others the canonical dump in
`Broiler.ECMAScript/runners/SyntaxDump.cs`: the merge that lands it will be the first model commit after W2
by the rule's "The commits, named before they exist", and is named here once it exists. It lands after
Broiler-Platform/Broiler.VM#214 and #217, each named above with its commit, and after corpus revision 44. **Part B is
taken after that merge, against its first parent** (decision D-js-part-b-method, recorded in
`docs/roadmap/SharedLibraries.md`'s section "Shared static semantics (SJL-15)" by this change): bundle `sjl-12-001`'s tool
built and run as the rule says at the merge and at its first parent, each from a clean `git archive`; compared per
input, variant and entry point by `scripts/compliance/compare_parse_corpus.py`, and per input by the dumps' hashes; with
the push runs at the merge of both workflows that run the test projects of `Broiler.JS.slnx`,
`.github/workflows/ci.yml` and Shared libraries (`.github/workflows/shared-libraries.yml`), imported here as bundle
`sjl-14-001`'s `bridge/w1-ci/` imported W1's; and nothing merged to the default branch after it until its ci.yml push run
has concluded, since a later push to the default branch cancels it. **Part B's reading at each model commit is limited
to the model commits taken so far**, each named, as bundle `sjl-14-001`'s reading of its own part B was limited to the
commits it had taken; the part's verdict waits for the last model commit before the base commit.

**How parts F and G will include the model, by stage 8** (decisions D-js-adapter, D-js-part-g, D-js-part-f-checklist and
D-js-library-runs, each recorded in the same section by this change): a library-level adapter for the `semantics`
vectors' `ScriptEvaluation` and `DirectEval` operations from stage 2, answering only the rows the model reports, and
growing stage by stage; the canonical dump's model section, which this change adds to
`Broiler.ECMAScript/runners/SyntaxDump.cs`, requested by the corpus runner's dump mode and by the Native AOT probe by
stage 8, with part G's control K2 planting a hash-ordered model side table; part F's checklist, a script in this bundle's
`tools/` that extends bundle `sjl-13-001`'s `tools/contract_check.py` to the milestone's facts, committed before any run
of part F, with part H's two controls; and parts F, G and E taken at stage 8's commit after its merge, each read as at
the library commit if no model commit lands before the base commit, and taken again at the actual library commit if one
does. No rule file is added or edited. This note names a merge to come and judges nothing.

*(Noted 2026-11-24, after SJL-15.2 stage 1's merge.)* **The first model commit is
`be6c4a5c3e389c7eb2aff71c34e31bdae5efe276`**, the merge of Broiler-Platform/Broiler.JS#1132 (head `f0418c36`), whose
first parent is `8b1fa311da6af71baac6378e8b4f8597a99c1312` (Broiler-Platform/Broiler.JS#1131, corpus revision 44). It
changes `Broiler.ECMAScript/Broiler.ECMAScript.Frontend/` and `Broiler.ECMAScript/Broiler.ECMAScript.Frontend.Tests/`,
among others the canonical dump in `Broiler.ECMAScript/runners/SyntaxDump.cs` and, moved to run alone,
`Broiler.JS/Broiler.JavaScript.Integration.Tests/Issue966InertEvalTests.cs`'s timing test, and descends from W2
(`83a9bd82`). The note above reads that the merge landing stage 1 "will be the first model commit
after W2 by the rule's "The commits, named before they exist", and is named here once it exists"; this names it. The
section's second item, which says of the model commits, the library commit, the base commit and the after commit
"named here when each exists", is left as written: the library commit, the base commit and the after commit do not
exist yet.

*(Noted 2026-11-24, after part B's runs at `be6c4a5c`.)* **Part B at the first model commit.** The note of 2026-11-24 at
SJL-15.2 stage 1 reads "Part B is taken after that merge, against its first parent". It is taken, in
[`part-b-be6c4a5c/`](part-b-be6c4a5c/README.md): bundle `sjl-12-001`'s tool, the same tree (`5ede6114`) at both commits,
built and run as the rule says from a clean `git archive` of `be6c4a5c` and of `8b1fa311`, entry points P and K and the
`--dump` mode over every input and variant of `conformance/parse/corpus.manifest`, the suite's archive checked by its
SHA-256 before each run; compared by `scripts/compliance/compare_parse_corpus.py acceptance` with no classification
file and by the dumps' hashes row for row; and both workflows' push runs at `be6c4a5c` imported as bundle `sjl-14-001`'s
`bridge/w1-ci/` imported W1's. Nothing merged to the default branch from before the first run until both push runs had
concluded. What the runs say:
- **Both runs are complete and identical in every field**: each report holds every input and variant of the manifest,
  at P and K and once for the dumps, with its end line; the comparison finds no difference and every row line of both
  modes is the same. The reports' headers differ only in each engine assembly's module version id, which is a build's
  name, not an answer: two builds of `8b1fa311` in two build trees differ the same way with every row the same, and two
  builds of each commit in one build tree gave byte-identical reports.
- **Shared libraries' push run concluded `success`** in its first attempt, every job green; its Build and SL rules
  jobs carry the six `Broiler.ECMAScript.*` test projects, each passed on both systems.
- **ci.yml's push run concluded `success` in its second attempt.** Its first attempt concluded `failure`: in
  windows-latest / Release's Test step, the test host of `Broiler.ECMAScript.Algorithms.Tests`, a project that step's
  filter selects no test of, died before any test ran ("testhost process failed to connect to datacollector process
  after 90 seconds", "Test Run Aborted."); every `Broiler.JavaScript.*` project in that job wrote "Passed!" with no
  test failed, two of them after the host's crash; the isolated CompilerScaling step was then skipped. The failed jobs were re-run once, under the session's rule that
  allows one re-run of a job that died before any test body ran; the re-run windows-latest / Release job and the CI
  result job concluded `success`, every other job carried from the first attempt green. ci.yml's jobs carry the
  fifteen `Broiler.JavaScript.*` test projects, each passed on Linux and, in the second attempt, on Windows, and the
  engine corpus in its two vectors jobs, passed. Both attempts are retained.

**The reading**, taken as recommended under the owner's direction ("Please do all recommended decisions and skip
waiting for review.") and flagged for the owner: **part B read MET on the retained runs at the model commits taken so
far (`be6c4a5c3e389c7eb2aff71c34e31bdae5efe276`), limited to them; the part's verdict waits for the last model commit
before the base.** Its basis, plainly: the identity half rests on the runs above; the rule's "the commit's CI run of
every test project passed" is read on ci.yml's push run at `be6c4a5c` as it concluded, in its second attempt, whose
first attempt failed in a test host before any test ran and is retained beside it, and on Shared libraries' push run.
**The alternatives, not taken, for the owner:** to read part B NOT MET at `be6c4a5c` because that run's first attempt
concluded `failure`, as bundle `sjl-13-001`'s third revision of 2026-11-03
([`decision-rule-revised-2026-11-03-third.md`](../sjl-13-001/decision-rule-revised-2026-11-03-third.md)) reads its own
part D on a push run's first attempt ("its first attempt decides, and every attempt is retained"), a precedent this
reading does not follow; or a dated revision of the clause saying which attempt of a run it reads, which would be the
owner's, since the runs have been seen. A NOT MET at any later model commit makes part B NOT MET.

**On ci.yml's Test step** (no part of the reading). The step runs `dotnet test` over `Broiler.JS.slnx` with a filter
that leaves out every `Broiler.ECMAScript.` test, since Shared libraries runs those projects, but it still starts a test
host for each of them. Both recent Windows failures of that step were in those hosts: at the first parent `8b1fa311`,
its push run's windows-latest / Release job failed when `Broiler.ECMAScript.Frontend.Tests`' test process "did not
respond within 60 seconds"; and at `be6c4a5c`, the first attempt above. **Next, before stage 2:** a separate change
leaves the `Broiler.ECMAScript` test projects out of that step; it is named here and not made by this change, which is
records only.

*(Noted 2026-11-25, in the change that makes it.)* **ci.yml's Test step leaves the `Broiler.ECMAScript` test projects
out.** The paragraph above reads "The step runs `dotnet test` over `Broiler.JS.slnx` with a filter that leaves out every
`Broiler.ECMAScript.` test, since Shared libraries runs those projects, but it still starts a test host for each of
them" and "**Next, before stage 2:** a separate change leaves the `Broiler.ECMAScript` test projects out of that step".
That change is the one carrying this note: the step now passes `--engine-only` to `eng/run-tests.sh`, which tests a
solution filter of every project of `Broiler.JS.slnx` but those of `Broiler.ECMAScript.slnf`, with the same filter, and
refuses to run unless those are exactly the solution's projects under `Broiler.ECMAScript/`; it starts no test host for
a `Broiler.ECMAScript` test project. Shared libraries' Windows job then failed the same way on
Broiler-Platform/Broiler.JS#1133's head (`Broiler.ECMAScript.Frontend.Tests`' test process "did not respond within 60
seconds"), so the same change also has both workflows' Windows test steps start two test projects at a time and every
test host wait longer for its data collector. Why a test host, its data collector or its test process failed to answer
in the three failures is not known: those two settings are mitigations whose effect is not shown, and only the hosted
Windows runs can show whether these steps now pass. None of it is part of part B's reading.

**The method's notes.** A first attempt at `8b1fa311` was stopped at the disk gate before its dump run, the shortfall
being its own build tree with the suite extracted on disk beside it; it is kept in
[`part-b-be6c4a5c/8b1fa311-attempt1-disk-gate/`](part-b-be6c4a5c/8b1fa311-attempt1-disk-gate/logs/commit.txt), set
aside and not compared. From the run that followed, the suite is extracted afresh from its checked archive to
`/dev/shm` before each run, as bundle `sjl-14-001`'s `bridge/scripts/commit.sh` kept its suite. No rule file is added
or edited, and no earlier output changes.

*(Noted 2026-11-25, at SJL-15.2 stage 2, written before part B's run at it.)* **The second model commit.** This branch's
merge will be the second model commit: SJL-15.2 stage 2, the early-error channel and the function-context rows, named
here once it exists. Part B is taken after it, against its own first parent (decision D-js-part-b-method), with both
workflows' push runs imported and decision D-js-part-b-attempt applying. The merge condition: both workflows are green
on the PR; after the merge, nothing merges to `main` until both push runs at the merge have concluded. For ci.yml's run
that includes the one re-run D-js-part-b-attempt allows (`docs/roadmap/SharedLibraries.md:3808`). A later push cancels
ci.yml's push run (`.github/workflows/ci.yml:25-27`, :36-39). A later push never cancels Shared libraries' push run,
because its push group carries the commit (`.github/workflows/shared-libraries.yml:34-39`). Its conclusion is imported
too, as at stage 1 ("Nothing merged to the default branch until both push runs at it had concluded",
`docs/roadmap/SharedLibraries.md:3789-3790`). The reading at it will be limited to the model commits taken so far.

From this change, part F's library-level run (the rule's part F, "Broiler.ECMAScript.Conformance.Tests' library-level
run, whose adapter answers a semantics vector's refusal or acceptance where the model decides it") is Conformance.Tests'
semantics adapter, `runners/AlgorithmsLibrary.Semantics.cs`, which answers the vectors of the rows the model reports,
by a row list of its own that a test holds equal to the Frontend's table (decisions D-js-adapter and D-js-adapter-form
in `docs/roadmap/SharedLibraries.md`'s section "Shared static semantics (SJL-15)"). Part F's "every vector the adapter
answers matches" is read at the parse, `error: SyntaxError` as a refusal and any other outcome as an acceptance, as
bundle `sjl-13-001`'s rule reads its syntax vectors: a reading of the session's, taken before any run of part F, which
the owner ruled on 2026-11-25 to keep ("Keep the reading at the parse (Recommended)"). EP0017 and EP0018 have no site of
their own in the library: no goal is ShadowRealm's, and EA0003, EA0004 or EE0162 refuses their sources first; whether
part F reads that as "a site in the library" is unknown, since the rule names only EA0006 to EA0008 as rows without one.
No rule file is added or edited. "What it will hold" above is left as written.

*(Noted 2026-11-25, after SJL-15.2 stage 2's merge.)* **The second model commit is
`6c61879bce81b1a7badf987e16c7b0531030258d`**, the merge of Broiler-Platform/Broiler.JS#1135 (head `8b0c233c`), whose
first parent is `2259fd4a1860aefc68d49b8f6f9f0200fcb6cc4d` (Broiler-Platform/Broiler.JS#1134, ci.yml's Test step). It
changes `Broiler.ECMAScript/Broiler.ECMAScript.Frontend/` and `Broiler.ECMAScript/Broiler.ECMAScript.Frontend.Tests/`,
among others the library-level adapter `Broiler.ECMAScript/runners/AlgorithmsLibrary.Semantics.cs` with its tests in
`Broiler.ECMAScript/Broiler.ECMAScript.Conformance.Tests/` and the Native AOT probe in
`Broiler.ECMAScript/Broiler.ECMAScript.Probe/`, and descends from W2 (`83a9bd82`) and from the first model commit
`be6c4a5c`. Its first parent is no model commit: nothing from `be6c4a5c` to `2259fd4a` changes a path under
`Broiler.ECMAScript/Broiler.ECMAScript.Frontend*`. The note above of 2026-11-25 at SJL-15.2 stage 2 reads "This branch's
merge will be the second model commit: SJL-15.2 stage 2, the early-error channel and the function-context rows, named
here once it exists"; this names it. The section "The commits"' second item, "named here when each exists", is left as
written: the library commit, the base commit and the after commit do not exist yet.

*(Noted 2026-11-25, after part B's runs at `6c61879b`.)* **Part B at the second model commit.** The note of 2026-11-25
at SJL-15.2 stage 2 reads "Part B is taken after it, against its own first parent (decision D-js-part-b-method), with both
workflows' push runs imported and decision D-js-part-b-attempt applying" and "The reading at it will be limited to the
model commits taken so far." It is taken, in [`part-b-6c61879b/`](part-b-6c61879b/README.md), as part B at the first
model commit was taken and in the same layout: bundle `sjl-12-001`'s tool, the same tree (`5ede6114`) at both commits,
built and run as the rule says from a clean `git archive` of `6c61879b` and of `2259fd4a`, entry points P and K and the
`--dump` mode over every input and variant of `conformance/parse/corpus.manifest`, the suite's archive checked by its
SHA-256 before each run; compared by `scripts/compliance/compare_parse_corpus.py acceptance` with no classification
file and by the dumps' hashes row for row; and both workflows' push runs at `6c61879b` imported as at the first model
commit. Nothing merged to the default branch from the merge until both push runs had concluded. What the runs say:
- **Both runs are complete and identical in every field**: each report holds every input and variant of the manifest,
  at P and K and once for the dumps, with its end line; the comparison finds no difference and every row line of both
  modes is the same. The reports' headers differ only in each engine assembly's module version id, which is a build's
  name, not an answer: two builds of `2259fd4a` in two build trees differ the same way with every row the same, as two
  builds of `8b1fa311` did at the first model commit.
- **Shared libraries' push run concluded `success`** in its first attempt, every job green; its Build and SL rules
  jobs test `Broiler.ECMAScript.slnf`, so its six `Broiler.ECMAScript.*` test projects, each passed on both systems.
- **ci.yml's push run concluded `success`** in its first attempt, every job green, with no re-run: its Test step, which
  since #1134 tests every project of `Broiler.JS.slnx` but those of `Broiler.ECMAScript.slnf`, passed the fifteen
  `Broiler.JavaScript.*` test projects on Linux and on Windows, the isolated CompilerScaling step ran on both, and the
  engine corpus passed in its two vectors jobs. So the re-run decision D-js-part-b-attempt allows was not needed.

**The reading**, taken as recommended under the owner's direction ("Please do all recommended decisions and skip
waiting for review."): **part B read MET on the retained runs at the model commits taken so far
(`be6c4a5c3e389c7eb2aff71c34e31bdae5efe276`, `6c61879bce81b1a7badf987e16c7b0531030258d`), limited to them; the part's
verdict waits for the last model commit before the base.** At `6c61879b` the rule's "the commit's CI run of every test
project passed" is read on both workflows' push runs as they concluded, each in its first attempt, so the question of
which attempt decides, flagged for the owner at the first model commit, does not arise here. The reading at
`be6c4a5c`, its basis and its flag, in the note of 2026-11-24 above, stay as written. A NOT MET at any later model
commit makes part B NOT MET. **Next:** SJL-15.2 stage 3, jump targets and labels, the third model commit. No rule file
is added or edited, and no earlier output changes.

*(Noted 2026-11-25, at SJL-15.2 stage 3, written before part B's run at it.)* **The third model commit.** This branch's
merge will be the third model commit: SJL-15.2 stage 3, jump targets and labels, named here once it exists. Part B is
taken after it, against its own first parent (decision D-js-part-b-method), with both workflows' push runs imported and
decision D-js-part-b-attempt applying. The merge condition: both workflows are green on the PR; after the merge, nothing
merges to `main` until both push runs at the merge have concluded. For ci.yml's run that includes the one re-run
D-js-part-b-attempt allows (`docs/roadmap/SharedLibraries.md:3808`). A later push cancels ci.yml's push run
(`.github/workflows/ci.yml:25-27`, :36-39). A later push never cancels Shared libraries' push run, because its push
group carries the commit (`.github/workflows/shared-libraries.yml:34-39`). Its conclusion is imported too, as at stage 1
("Nothing merged to the default branch until both push runs at it had concluded",
`docs/roadmap/SharedLibraries.md:3789-3790`). Part B is taken after the merge, against the merge's own first parent.
Stage 2's part B records say the same of stage 2: "Nothing merged to the default branch until both push runs at it had
concluded" (`docs/roadmap/SharedLibraries.md:3893-3894`). The reading at it will be limited to the model commits taken
so far.

From this change the library-level adapter, `runners/AlgorithmsLibrary.Semantics.cs`, also answers the vectors of
stage 3's jump rows, and, by decision D-js-adapter-groups in `docs/roadmap/SharedLibraries.md`'s section "Shared static
semantics (SJL-15)", those of correspondence group `lb1`, row LB1, which rest on EE0082 and EE0166: the acceptances that
show hand-off H20 read by the specification. Each is read at the parse, the reading the owner ruled on 2026-11-25 to
keep ("Keep the reading at the parse (Recommended)"), as the note above at SJL-15.2 stage 2 records it. The longer
test-host wait that Broiler-Platform/Broiler.JS#1134 set on both workflows' test steps, which the note of 2026-11-25 in
the change that made it names among "mitigations whose effect is not shown", was kept by the owner's own answer of
2026-11-25, "Keep the 300 s wait", as `docs/roadmap/SharedLibraries.md`'s note of 2026-11-25 at SJL-15.2 stage 2 records
it. No rule file is added or edited, and no earlier output changes. "What it will hold" above is left as written.

*(Noted 2026-11-25, after SJL-15.2 stage 3's merge.)* **The third model commit is
`d87044c9db5fe2a26841782308bd53e2f9294656`**, the merge of Broiler-Platform/Broiler.JS#1137 (head `cd92761b`), whose
first parent is `77fe27aa4bc6e0a8a0819b32a2b1b1158a9fe913` (Broiler-Platform/Broiler.JS#1136, stage 2's part B
records). It changes `Broiler.ECMAScript/Broiler.ECMAScript.Frontend/` and
`Broiler.ECMAScript/Broiler.ECMAScript.Frontend.Tests/`, among others the library-level adapter
`Broiler.ECMAScript/runners/AlgorithmsLibrary.Semantics.cs` with its tests in
`Broiler.ECMAScript/Broiler.ECMAScript.Conformance.Tests/`, the library's canonical dump
`Broiler.ECMAScript/runners/SyntaxDump.cs` and the Native AOT probe in `Broiler.ECMAScript/Broiler.ECMAScript.Probe/`,
and descends from W2 (`83a9bd82`) and from the first and second model commits `be6c4a5c` and `6c61879b`. Its first
parent is no model commit: nothing from `6c61879b` to `77fe27aa` changes a path under
`Broiler.ECMAScript/Broiler.ECMAScript.Frontend*`. The note above of 2026-11-25 at SJL-15.2 stage 3 reads "This branch's
merge will be the third model commit: SJL-15.2 stage 3, jump targets and labels, named here once it exists"; this names
it. The section "The commits"' second item, "named here when each exists", is left as written: the library commit, the
base commit and the after commit do not exist yet.

*(Noted 2026-11-25, after part B's runs at `d87044c9`.)* **Part B at the third model commit.** The note of 2026-11-25
at SJL-15.2 stage 3 reads "Part B is taken after it, against its own first parent (decision D-js-part-b-method), with both
workflows' push runs imported and decision D-js-part-b-attempt applying" and "The reading at it will be limited to the
model commits taken so far." It is taken, in [`part-b-d87044c9/`](part-b-d87044c9/README.md), as part B at the first and
second model commits was taken and in the same layout: bundle `sjl-12-001`'s tool, the same tree (`5ede6114`) at both
commits, built and run as the rule says from a clean `git archive` of `d87044c9` and of `77fe27aa`, entry points P and K
and the `--dump` mode over every input and variant of `conformance/parse/corpus.manifest`, the suite's archive checked
by its SHA-256 before each run; compared by `scripts/compliance/compare_parse_corpus.py acceptance` with no
classification file and by the dumps' hashes row for row; and both workflows' push runs at `d87044c9` imported as at the
second model commit. Nothing merged to the default branch from the merge until both push runs had concluded. What the
runs say:
- **Both runs are complete and identical in every field**: each report holds every input and variant of the manifest,
  at P and K and once for the dumps, with its end line; the comparison finds no difference and every row line of both
  modes is the same. The reports' headers differ only in each engine assembly's module version id, which is a build's
  name, not an answer: two builds of `77fe27aa` in two build trees differ the same way with every row the same, as two
  builds of `2259fd4a` did at the second model commit.
- **Stage 3's `model-target` lines do not reach the tool's dumps.** Stage 3 adds them to the model section of the
  library's canonical dump, which is written only when a parse builds the model; this tool writes its own dump of
  Broiler.JS's tree, reaches the Frontend only through Broiler.JS's parser, and nothing under `Broiler.JS/` or in bundle
  `sjl-12-001` changes between the two commits or sets `ParseRequest.BuildSemanticModel`
  ([`part-b-d87044c9/identity/model-target.txt`](part-b-d87044c9/identity/model-target.txt)). The `--dump` output is
  identical at both commits.
- **Shared libraries' push run concluded `success`** in its first attempt, every job green; its Build and SL rules
  jobs test `Broiler.ECMAScript.slnf`, so its six `Broiler.ECMAScript.*` test projects, each passed on both systems.
- **ci.yml's push run concluded `success`** in its first attempt, every job green, with no re-run: its Test step, which
  since #1134 tests every project of `Broiler.JS.slnx` but those of `Broiler.ECMAScript.slnf`, passed the fifteen
  `Broiler.JavaScript.*` test projects on Linux and on Windows, the isolated CompilerScaling step ran on both, and the
  engine corpus passed in its two vectors jobs. So the re-run decision D-js-part-b-attempt allows was not needed.

**The reading**, taken as recommended under the owner's direction ("Please do all recommended decisions and skip
waiting for review."): **part B read MET on the retained runs at the model commits taken so far
(`be6c4a5c3e389c7eb2aff71c34e31bdae5efe276`, `6c61879bce81b1a7badf987e16c7b0531030258d`,
`d87044c9db5fe2a26841782308bd53e2f9294656`), limited to them; the part's verdict waits for the last model commit before
the base.** At `d87044c9` the rule's "the commit's CI run of every test project passed" is read on both workflows' push
runs as they concluded, each in its first attempt, so the question of which attempt decides, flagged for the owner at
the first model commit, does not arise here. The readings at `be6c4a5c` and at `6c61879b`, and the first one's flag, in
the notes above of 2026-11-24 and 2026-11-25, stay as written. A NOT MET at any later model commit makes part B NOT MET.
**Next:** SJL-15.2 stage 4, private-name environments, the fourth model commit. No rule file is added or edited, and no
earlier output changes.

*(Noted 2026-11-25, at SJL-15.2 stage 4, written before part B's run at it.)* **The fourth model commit.** This branch's
merge will be the fourth model commit: SJL-15.2 stage 4, private-name environments, named here once it exists. Part B is
taken after it, against its own first parent (decision D-js-part-b-method), with both workflows' push runs imported and
decision D-js-part-b-attempt applying. The merge condition: both workflows are green on the PR; after the merge, nothing
merges to `main` until both push runs at the merge have concluded. For ci.yml's run that includes the one re-run
D-js-part-b-attempt allows (`docs/roadmap/SharedLibraries.md:3808`). A later push cancels ci.yml's push run
(`.github/workflows/ci.yml:25-27`, :36-39). A later push never cancels Shared libraries' push run, because its push
group carries the commit (`.github/workflows/shared-libraries.yml:34-39`). Its conclusion is imported too, as at stage 1
("Nothing merged to the default branch until both push runs at it had concluded",
`docs/roadmap/SharedLibraries.md:3789-3790`). Part B is taken after the merge, against the merge's own first parent.
Stage 3's part B records say the same of stage 3: "Nothing merged to the default branch until both push runs at it had
concluded" (`docs/roadmap/SharedLibraries.md:3972-3973`). The reading at it will be limited to the model commits taken
so far.

From this change the library-level adapter, `runners/AlgorithmsLibrary.Semantics.cs`, also answers the vectors of
stage 4's private-name rows, EE0167 and EA0009 (EE0177 has none), each read at the parse, the reading the owner ruled on
2026-11-25 to keep ("Keep the reading at the parse (Recommended)"), as the note above at SJL-15.2 stage 2 records it. By
decision D-js-adapter-visible-names in `docs/roadmap/SharedLibraries.md`'s section "Shared static semantics (SJL-15)", a
`DirectEval` vector's eval code now inherits the private names visible at the call, read from its Script's model, and
correspondence group `pn1` is not implemented: `pn1/0004` and `pn1/0005` expect a value the Script computes after
catching the eval code's refusal, which a reading at the parse cannot match. No rule file is added or edited, and no
earlier output changes. "What it will hold" above is left as written.
