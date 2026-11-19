# Bundle SJL-14-001: Broiler.JS on the shared syntax library

**Rules only. Nothing here is a verdict.** *(Noted 2026-11-15, at SJL-14.2's tail: the line before this note reads "Rules only. Nothing here is a verdict."; it no longer holds for part B, which is judged in "Part B, judged on 2026-11-15" below. The side-by-side run at W1 is retained and read, not judged. Every other part has no run, and no clause is read here.)*

**Written:** 2026-11-03, at milestone SJL-14 (work packages SJL-14.0 and SJL-14.1) of the shared JavaScript libraries
programme, Broiler.VM's `docs/javascript-shared-libraries/milestones/sjl-14-broiler-js-syntax.md`. This repository's
record is `docs/roadmap/SharedLibraries.md`, section "Broiler.JS on the shared syntax library (SJL-14)", with the
owner's decisions D40 to D56 and D64 (of 2026-11-03, but D41 and D44 to D47, of 2026-10-26 as Broiler.VM's JSD-0083
revised them; D45 revised again on 2026-11-03, finding S14-F1). SJL-14 files no verdict of its own: Broiler.VM's JSD-0083, SJL-13.1's area
verdict, covers this engine's front end too (decision D40). **Base:** this bundle's records were written from
Broiler.JS `33d3093b` and Broiler.VM `c5bde2b`, each repository's default branch on 2026-11-03.

## What it holds now

| File | What |
|---|---|
| [`decision-rule.md`](decision-rule.md) | The rule: eleven predeclared parts, A to K, filed before any change it judges (no bridge, no switch, no adapter package, no deletion); it holds no figure |
| [`corrections.md`](corrections.md) and [`corrections/`](corrections/k-rows.tsv) | The corrections K1 to K10, each with its rows and test262 paths; the classes E and T with their rows and sites; finding S14-F1; the probes' rows |
| [`dispositions.md`](dispositions.md), [`dispositions.tsv`](dispositions.tsv) and [`api/`](api/) | Every public type and member of `Ast` and `Parser` with its disposition (decision D50), from a PublicApiGenerator listing at `33d3093b`; the non-tree types' new home; SJL-14.5's deletion list member by member |
| [`inherited-context/`](inherited-context/README.md) | The inherited-context probes of decision D44, run on both engines with every answer recorded |
| [`inputs/`](inputs/README.md) | Part I's workloads (the real corpora, named by revision and SHA-256) and part J's seeds |
| [`tools/records/`](tools/records/write_records.py) | The script that writes `corrections/`'s generated tables and `dispositions.tsv`, with a `check` mode |
| [`tools/side-by-side/`](tools/side-by-side/README.md) | Part C's tool, with entry points P, B, K and K-on and sjl-12-001's canonical dump; `compare.py` (outcome pairs, part B's identity, class M's message table) and `spancheck.py` (class S's span check); landed with W1 (SJL-14.2) |
| [`bridge/`](bridge/README.md) | Part B's runs: W1's CI run of every test project, imported from GitHub Actions with its run ids and digests; the identity runs with the switch off at W1 and its first parent and at each library merge since W1 and its first parent; the switch-on outputs at the library merges and their first parents, read; the comparisons; a read at the default branch's head, outside part B *(added 2026-11-15, at SJL-14.2's tail)* |
| [`side-by-side/w1/`](side-by-side/w1/README.md) | Part C's tool run at W1, every entry point with the dumps, and its readings (outcome pairs, the generated message tables, the outcome moves against the corrections' listing, the span check), read and not judged *(added 2026-11-15, at SJL-14.2's tail)* |
| [`collect-part-i.py`](collect-part-i.py) | Part I's collector for a controlled lane, drafted before any part I run, modelled on bundle `sjl-3-001`'s `collect-lane.py` *(added 2026-11-19, on W2's branch)* |
| [`tools/obsolete-reader/`](tools/obsolete-reader/README.md) | Part F's metadata reader, drafted before any part F run *(added 2026-11-19, on W2's branch)* |
| [`tools/fuzz/`](tools/fuzz/README.md) | Part J's seeded mutation session, committed with W2's change *(added 2026-11-19, on W2's branch)* |
| [`tools/deletion-search/`](tools/deletion-search/README.md) | Part K's deletion search, with its control, committed with W2's change *(added 2026-11-19, on W2's branch)* |

## W2's code, written on 2026-11-19 (on branch `sjl-14-w2`, not merged)

**Written:** 2026-11-19, with W2's code, on a branch from the default branch's `fefb4f9c`, before any base run and before
the base commit exists. Nothing here is a run or a reading of a part; the rule is not edited. The commits are the
branch's, each named "SJL-14: W2 stage N, ..." for the plan's stage order (`docs/roadmap/SharedLibraries.md`, "The work,
in order"), and W2 is the one merge that lands them.

**The session's decisions under the owner's direction of 2026-10-18**, "Please do all recommended decisions and skip
waiting for review.", each taken as recommended, with its alternative not taken; none revises a rule line:
- **B5, the shared stream's version.** `Broiler.ECMAScript/Directory.Build.props`' `VersionSuffix` moves from
  `preview.8` to `preview.12` in W2's package stage (stage 4), so that the Parser package's dependency on
  `Broiler.ECMAScript.Frontend` names the version published from tag `ecmascript-v0.1.0-preview.12`, whose
  `Broiler.ECMAScript/` equals this branch's (the section "The shared stream's published version" of the records of
  2026-11-19 reads it). Moving it publishes nothing. *Not taken:* having `eng/pack.ps1` resolve the shared dependency from
  the `ecmascript-v*` tags.
- **B6, part F's control.** The pristine consumer packs both sets, the engine closure, which packs the shared libraries
  it references, and the shared set's other packable projects, and maps this repository's ids to the local feed alone by
  package source mapping (`scripts/packaging/test_pristine_consumer.py`, stage 6), so that removing
  `Broiler.ECMAScript.Frontend` from the feed fails the restore with the missing-package error rather than restoring
  the published version from nuget.org; `--control-remove` takes the control. The decision reads "map `Broiler.*`";
  the mapping names the prefixes `Broiler.JavaScript.` and `Broiler.ECMAScript.`, since `Broiler.Regex` and
  `Broiler.Unicode.*`, which other repositories publish, are not packed here and come from nuget.org. `scripts/` is no
  product path. *Not taken:* a dated rule revision of the control, which would be weaker evidence by the rule's
  "Revisions" and goes to the owner by precedent.
- **B7, the facade and part I's quantity.** `FastParser.ParseProgram`'s facade parses through `SharedSyntaxBridge.Parse`,
  which takes the hop to the library's parse thread (decision D47 (a)), so the harness's `parse` column
  (`CompilePhaseMetrics`, which times `FastParser.ParseProgram` on the calling thread) measures the rule's after
  quantity, "the hop to the parse thread, the frontend's parse, the bridge and the return", with each arm running its
  own commit's harness. This is how the after commit's harness reaches part I's quantity, which reading B10 of the
  records of 2026-11-19 left to W2 to record. *Not taken:* a change of the harness under `benchmarks/`.
- **B8, a bridge defect found at the base.** One part C finds unclassified at the base commit is fixed inside W2, on this
  branch, and part C reads NOT MET for that input unless a class admits it. None is known: no base run has been taken.
  *Not taken:* reading the rule's "fixed as a dark merge" as allowing a bridge-only merge before W2, which conflicts
  with the rule's "W1 is the only first-parent commit that carries any part of the bridge or the switch".
- **B10, two code points.** `FastScanner.IsValidRegularExpressionLiteral`, Broiler.Regex's validate entry, stays where it
  is, and the bridge's regular-expression provider calls it (`dispositions.md` lists it on no deletion row). *Not
  taken:* moving it into `BridgeRegularExpressions`. And row R24 of K10 is corrected by an overload of the public
  `DirectEvalSupport.Execute` whose new parameter carries the class's heritage, with its row in
  [`docs/public-api.md`](../../public-api.md) ("Public API changes, 2026-11-19") (stage 3). *Not taken:* leaving R24
  uncorrected at SJL-14.

**Where W2's code departs from the records, and why**, each recorded here before any base run:
- **`FastParseException` stays in `Ast`**, not moved or forwarded: its public field `Token` is a `FastToken`, which stays
  in `Ast`. `dispositions.md` records it by a dated note of the same day.
- **The stages' contents.** The plan's stage order puts "the switch on and removed with the old parser's
  implementations" first. The implementations cannot leave before their facades exist, and the portable compiler, the
  module analysis and the two re-scans still read them until stages 2 and 3, so stage 1 removes the switch and the
  implementations leave with the facades: `FastParser`'s partial files and the parser's half of `FastScope` in stage 4,
  the adapter packages, and the old scanner, with `FastTokenStream`'s scanning code, `FastKeywordMap`'s table and
  `CharExtensions`' scanner members, in stage 5, whose division fallback was a branch of the scanner's
  regular-expression reader. The brief's map named stage 5 "the division fallback only"; the plan names it "the
  regular-expression code and the division fallback". The Unicode tables, internal, moved to
  `Broiler.JavaScript.Expressions` in stage 2, since the portable compiler, which may reference neither `Ast` nor the
  parser (decision D53), reads them; D50 moves them "with their readers". Two commits continue a stage: stage 1's
  deep-nesting fixtures (rows R12 and R13, "revised at SJL-14"), and stage 4's marks on `Parser`'s nested public types.
- **Tests revised**, each by the correction or class it shows, with its old text in the diff: the deep-nesting fixtures
  (R12, R13; class C3); three parser fixtures whose acceptance the library corrects as the specification does (`await` in
  a class static block, `yield` in a class field initializer, an operator sequence that is no expression); two that read
  the old parser's backtracking report and three unterminated-token lines (class M, decision D46); and the tests that read
  the moved types' assembly. They are not part C's classification, which is taken at the base commit.

**Checks** on the branch head are recorded with the branch's report, not here; no part's base or after run is taken.

## What it will hold

Each named by the rule, retained when its run is taken, with the commit it ran at:
- `bridge/` - part B's identity runs at W1 and its first parent, and at each dark commit and its first parent, with
  their switch-on outputs (decision D52); *(Noted 2026-11-15: taken, with W1's CI run, at W1 and at the library
  merges since W1; see the table above and "Part B, judged on 2026-11-15".)*
- `side-by-side/w1/` and `side-by-side/w2-base/` - part C's runs, every difference classified, with the generated
  message table of class M (decision D46), written by the side-by-side tool, `tools/side-by-side/`, which W1 adds;
  *(Noted 2026-11-15: `side-by-side/w1/` is taken, the tool's run, read and not judged, with no difference classified,
  since the rule classifies only at the base commit; `side-by-side/w2-base/` waits for the base commit.)*
- `base/` and `after/` - part A's hosted runs (UTC, negative tests), imported unchanged with their digests, and the
  comparison; parts D, E, F, G, H and K's local runs;
- `measurement/` - part I's runs, void on a lane that is not controlled (decision D48); *(Noted 2026-11-18: this
  line reads "`measurement/` - part I's runs, void on a lane that is not controlled". Part I is to be taken as written
  on the controlled lane [`win-x64-r7-5800x`](../lanes/win-x64-r7-5800x/README.md), by the owner's choice of
  2026-11-18 (the note under "Clause 6" below). That run is retained inside `measurement/`, in a folder named for the
  lane, `measurement/win-x64-r7-5800x/`, since the bundle's inputs retain no figure of part I anywhere but in
  `measurement/` ([`inputs/README.md`](inputs/README.md), [`inputs/workloads/README.md`](inputs/workloads/README.md)).
  The folder's name is this note's, not the rule's. No run of part I exists.)*
- `fuzz/` - part J's session;
- `manifest.json` (decision D56), written by the bundle's collector at the evidence step.

## The commits

- **The rule commit**: the last commit on this bundle's branch that changes `decision-rule.md`, and with it the merge
  that brings it into the default branch, named here when it exists. The rule is filed by that merge, before any
  bridge, switch or run, and is never rewritten after it. *(Noted 2026-11-03, at SJL-14.1, before merge: the branch's
  first commit carried the rule as first written; a later commit of the same branch corrects, in place, its class E
  bullet (finding S14-F1, worded plainly after review). Nothing was filed before the merge, so no rule was rewritten.
  The later commit also follows the one that adds `docs/roadmap/SharedLibraries.md`'s SJL-14 section, which the
  rule's header names.)* *(Noted 2026-11-15, at SJL-14.2's tail: the rule commit is
  `6e7ef46f0cb40aa5fb7ad400c677ad338756dfc6`, the branch's last commit that changes `decision-rule.md`, and the merge
  that brings it into the default branch is `d8f146026bd69d2182be9d3c7aa9b85e016179c6`, Broiler-Platform/Broiler.JS#1100.
  `decision-rule.md` has not changed since, through the default branch's head on 2026-11-15, `e4b6b49e`.)*
- **W1, the bridge commit**, and **W2, the after commit**, with their first parents (W2's is the base commit), and any
  dark commit between them: named here when each exists, as the rule's section "The commits, named before they exist"
  defines them. *(Noted 2026-11-15, at SJL-14.2's tail.)*
  - **W1** is `8e5519d8cf81808856e32f2275fcb4d3b98a73f3`, the merge of Broiler-Platform/Broiler.JS#1102 (2026-11-03).
    **Its first parent**, `e2ed7fae6384b484b3b9773fd89dd33e40c01884` (the merge of Broiler-Platform/Broiler.JS#1101),
    descends from the rule commit's merge `d8f14602`. W1 is the only first-parent commit, through `e4b6b49e`, that
    carries any part of the bridge or the switch: no later first-parent merge changes `Parser/Bridge/`,
    `SharedSyntaxBridge`, `ParseThroughBridge`, `BridgeByDefault` or `BROILER_PARSE_THROUGH_BRIDGE`.
  - **The merges to `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*/` since W1** are these, and each also changes other
    product paths (decision D42's list):
    - `29061d65f0ca41c5bd93074083f2018ccedd5c57`, Broiler-Platform/Broiler.JS#1104 (first parent `6f7284da`, the merge
      of Broiler-Platform/Broiler.JS#1103, records only): the token stream behind `ParseRequest.RetainTokens` in
      `Broiler.ECMAScript.Frontend` and its tests; outside `Frontend*/`, `Broiler.ECMAScript/runners/SyntaxDump.cs`,
      `CHANGELOG.md`, `CODE-ASSURANCE.md`, `HUMAN_REVIEW.md`, `README.md` and `assurance.manifest.json` under
      `Broiler.ECMAScript/`.
    - `35da77eca574f5cbd1163c5cd26bd19c3b69e8d5`, Broiler-Platform/Broiler.JS#1105 (first parent `29061d65`): rule
      SL8's move to Hairs.Cli; in `Broiler.ECMAScript.Frontend` only the assurance headers renamed; outside it
      `Broiler.ECMAScript.Algorithms`, `.Architecture.Tests`, `.Conformance`, `.Generation` and `.Intl`, and
      `CODE-ASSURANCE.md`, `HUMAN_REVIEW.md`, `hairs.config.json` and `hairs.manifest.json` under `Broiler.ECMAScript/`.

    Whether they are dark commits as the rule defines one is read in "Part B, judged on 2026-11-15" below.
  - **The other product merges since W1**, which change no `Frontend*/` path and so are no dark commit by any reading:
    part B does not cover them, and part C, at the base commit, takes every change before it.
    - `055a51c7f56e938b2625293d35f37928a1456bdf`, Broiler-Platform/Broiler.JS#1107, and
      `b9ae4b5ad1e5b9d74b1b26516900e151b50fd252`, Broiler-Platform/Broiler.JS#1108 (SJL-9): `Broiler.ECMAScript.Generation`
      and `.Intl` with their tests, `Broiler.ECMAScript/data/` and `runners/`, and the changelog and assurance files under
      `Broiler.ECMAScript/` (and its `README.md`, #1108).
    - `a5f2692e88a9aafeaa850bebe9c3399f6263353b`, Broiler-Platform/Broiler.JS#1110 (SJL-9.5):
      `Broiler.JS/Broiler.JavaScript.BuiltIns`, `.BuiltIns.Tests` and `.Integration.Tests` (its `Broiler.JS/CHANGELOG.md`
      is not named, since the rule's list of the product paths leaves it out).
    - `caa3863cb0f184dfd36baa31937ae4ad281672bd`, Broiler-Platform/Broiler.JS#1114 (SJL-9's clauses 7 and 8):
      `Broiler.JS/Broiler.JavaScript` and its projects `.Ast`, `.BuiltIns`, `.BuiltIns.Tests`, `.Compiler`,
      `.Compiler.Tests`, `.Debugger`, `.Debugger.Tests`, `.Engine`, `.ExpressionCompiler`, `.Expressions`, `.Extensions`,
      `.Integration.Tests`, `.LinqExpressions`, `.Modules`, `.Parser` (`RegExpValidator.cs`) and `.Runtime`;
      `Broiler.JS/Directory.Build.props`, `Broiler.JS/BannedSymbols.HostCulture.txt`, `Broiler.JS/host-culture.globalconfig`;
      and the root's `Directory.Packages.props`.
    - `04f52a07e177fe813c99ecc96a085ca541930866`, Broiler-Platform/Broiler.JS#1115 (the host-culture architecture test):
      `Broiler.JS/Broiler.JavaScript` and its projects `.Architecture.Tests`, `.Ast`, `.BuiltIns`, `.Compiler`,
      `.Debugger`, `.Engine`, `.ExpressionCompiler`, `.Expressions`, `.LinqExpressions`, `.Parser` (`FastTokenStream.cs`),
      `.Portable` and `.Runtime`.

    Every other first-parent merge since W1 changes no product path (#1103, #1106, #1109, #1111, #1112, #1113, #1116).
    Each merge's paths are in [`bridge/merges-since-w1-paths.txt`](bridge/merges-since-w1-paths.txt), each marked
    `product` or `other` by the rule's list of the product paths (decision D42) as written: that list leaves out
    `Broiler.JS/CHANGELOG.md` and keeps all of `Broiler.ECMAScript/` outside its `docs/`, so `Broiler.ECMAScript/CHANGELOG.md`
    is marked `product` and `Broiler.JS/CHANGELOG.md` `other`, and the lists above follow the same marks. A read at the
    default branch's head, `e4b6b49e`, outside part B and not judged, finds the switch-off rows of part B's runs the same
    as W1's ([`bridge/README.md`](bridge/README.md)).
  - **W2 and the base commit** do not exist yet: W2 is held for SJL-11.5 (decision D51's recommended order;
    [`docs/roadmap/SharedLibraries.status.md`](../../roadmap/SharedLibraries.status.md)'s SJL-14 row). The rule's dark
    commits run to the base commit, so their set is still open, and part B's verdict below holds only at the commits
    taken.

## Part B, judged on 2026-11-15

**Written:** 2026-11-15, at SJL-14.2's tail, after part B's runs ([`bridge/`](bridge/README.md)) were taken and read.
The rule, [`decision-rule.md`](decision-rule.md), is not edited.

**The question the library merges put.** The rule defines "A dark commit" as "any default-branch merge between W1
and the base commit that changes only `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*/` (decision D52)", and part B
judges "at W1 and at every dark commit"; it also says "A part whose base was taken at any other commit, or whose
commits fail those conditions, is void." Decision D52 ([`docs/roadmap/SharedLibraries.md`](../../roadmap/SharedLibraries.md))
reads: "Each merge to `Broiler.ECMAScript.Frontend*/` after W1 is a dark window: one merge, judged by the rule's part B
identity with the switch off". #1104 and #1105 each change `Frontend*/` and, with it, other product paths (above), so
D52 makes each a dark window and the rule's "changes only" makes neither a dark commit.

**The reading taken** (the recommended one, under the owner's direction of 2026-10-18, "Please do all recommended
decisions and skip waiting for review."): **D52's.** Each merge that changes `Frontend*/` after W1 is a dark commit for
part B, whatever else it changes, and is judged by part B's identity with its own first parent; the rule's "changes
only" names what a library merge in this window is for, and a merge that also changes other paths is held to the same
identity, which builds and runs the whole product at both commits and so reads what those paths do to the parse and
the compile too.

**With it, a second reading, of the void sentence.** On D52's reading, #1104 and #1105 are part B's commits while they
fail the definition's "changes only", and read as written, that is a part "whose commits fail those conditions", which
is void. The reading taken is narrow: "those conditions" are the conditions the rule sets on the commits it names (W1
one merge, the only first-parent commit with the bridge or the switch, its base descending from the rule commit), which
hold, and not the dark-commit definition, so neither merge voids the part. The rule's words do not settle this; it is a
choice, taken under the same direction, and its alternative is below.

**Under these readings, part B is MET at the commits taken**: at W1 against `e2ed7fae`, at #1104 against `6f7284da` and
at #1105 against `29061d65`, both runs are complete and identical in every field (P's and K's outcome, exception type
and message, and P's and the for-loop pass's dump hashes), and W1's CI run of every test project of `Broiler.JS.slnx`
passed, on Linux and on Windows ([`bridge/identity/`](bridge/identity/), [`bridge/w1-ci/`](bridge/w1-ci/commands.txt)).
The verdict is limited to those commits. The rule's dark commits run to the base commit, which does not exist yet:
under this reading, each later merge to `Frontend*/` before the base commit (a library fix that part C's reading sends
to a dark merge, or any other) is a dark commit, part B is read again at each against its own first parent, and a
NOT MET at any of them makes part B NOT MET.

**The alternatives, not taken.**
- **The rule's words.** Neither #1104 nor #1105 is a dark commit, as each changes more than `Frontend*/`; part B is
  judged at W1 alone, and those merges are product merges outside part B, like #1107, #1108, #1110, #1114 and #1115,
  which part C at the base commit covers. **Under it, part B is MET at W1**; the runs at #1104 and #1105 stay retained
  and read. It is read again at any later merge before the base commit that changes only `Frontend*/`. This is the
  verdict that needs no reading of the rule beyond its words: W1's conditions hold, as its first parent `e2ed7fae`
  descends from the rule commit's merge `d8f14602` and no later first-parent merge, through `e4b6b49e`, carries any
  part of the bridge or the switch.
- **D52's reading with the void sentence as written.** #1104 and #1105 are part B's commits and fail the definition's
  "changes only", so part B's commits "fail those conditions": **under it, part B is void.**

**For the owner, an optional ruling** among the three: "D52's reading, the void sentence read narrowly" (taken: MET at
W1, #1104 and #1105, and read again at each later merge to `Frontend*/` before the base commit), "The rule's words"
(MET at W1, and read again at each later merge that changes only `Frontend*/`), or "D52's reading, the void sentence as
written" (void). The ruling also decides what a later library merge that touches other paths must carry. Nothing is
accepted: no second holder exists to review it, and part B is no clause; every clause of this engine's, 1 to 6, is
unmet.

**The switch-on outputs**, read and not judged (part C judges them at the base commit): the side-by-side tool's B and
K-on rows are the same at #1104's first parent, at #1104, at #1105, and at W1. Neither library merge moved a switch-on
outcome, message or tree over the corpus.

## The side-by-side run at W1, read on 2026-11-15

[`side-by-side/w1/`](side-by-side/w1/README.md) holds the side-by-side tool's run at W1, at P, B, K and K-on with the
dumps, as the tool's README and the rule's part C say, read and not judged: nothing in it is classified, and it makes
no claim about library defects. What it shows, in words (each count is in the folder): every outcome move the
corrections list goes to the outcome they expect. The moves they do not list are each in `outcome-moves.tsv.gz`; most
have the form of class E (P accepts, B refuses, K refuses on both sides) or of class C3 (nesting past the library's
bound), and three inputs of this engine's own parser tests, `J000557`, `J001072` and `J001780`, which the old path
accepted and both B and K-on refuse, are listed by no row of K10. Many refusals change their text, as decision D46
expects, in the generated message tables. `spancheck.py` reads every tree difference of P and B as one of spans alone,
but for the inputs corrections W1a to W1c cite, and labels each `S` or `both-agree`; a `both-agree` row is one its
check cannot decide, for a reader to decide from its two texts, and is not class S by the rule's words.

**Open items for part C at the base commit:** the outcome moves the corrections do not list, those three inputs among
them, and the `both-agree` span differences, each to be classified or found unclassified there (`spans-not-s.tsv.gz`
lists them with both texts).

**Part C's other runs at W1**, every test project with the switch on and test262 locally with the switch off and on,
are not taken here. The rule asks for them at W1 too, retained and read, not judged, and they are still to be taken;
each run names its tested commit, so they can be taken at W1 at any time. The choice taken, under the owner's direction
of 2026-10-18, is to leave them to a later stage, before or with part C's runs at the base commit; the alternative not
taken is to take them in this stage. Part C's verdict reads only the runs at the base commit, so none waits on them.

## What it does not claim

- **No figure.** No count, timing, ratio or score is written in this README; the bundle's runs hold their own. No
  test262 suite total is written here at any step (rule H5).
- **No manifest yet.** The bundle has no `manifest.json` (decision D56); it is written at the evidence step by the
  bundle's collector, so part B's and the side-by-side run's folders are named instead by their `hashes.txt`.
  *(Added 2026-11-15, at SJL-14.2's tail.)*
- **The parse thread's stack.** The rule writes it as "the 256 MiB thread" (its section on the corrections) and "a 256
  MB stack" (parts B and D), both after SJL-13's plan's section 4.4; both name the one stack bundle `sjl-12-001`'s tool
  uses, which that bundle records as "256 MB" and its tool sets as `256 * 1024 * 1024` bytes
  (`docs/evidence/sjl-12-001/tools/parse-corpus/Program.cs:23`).
- **Clause 6** is read as the milestone file states it when part I is read; what it becomes while no controlled lane
  exists is the owner's own choice (decision D48), not made here. *(Noted 2026-11-03, later: the owner has made it.
  On 2026-11-03 the owner wrote: "Please defer the measurement", decision D48's alternative (i), the owner's own dated
  choice and not one taken under the owner's direction of 2026-10-18. Clause 6 is kept as written: met only by a MET
  reading of part I on a controlled lane, and unmet until one exists. Part I's runs on a lane that is not controlled
  go to `measurement/` as before, retained and recorded void. The choice is recorded in
  [`docs/roadmap/SharedLibraries.md`](../../roadmap/SharedLibraries.md)'s SJL-14 section and by a dated note on
  Broiler.VM's decision SJL-D-23; [`decision-rule.md`](decision-rule.md) is not edited, as it leaves the choice to the
  owner and makes none.)* *(Noted 2026-11-18, at SJL-D-23's trigger (a): the note above reads "Clause 6 is kept as
  written: met only by a MET reading of part I on a controlled lane, and unmet until one exists." A controlled lane
  exists, [`win-x64-r7-5800x`](../lanes/win-x64-r7-5800x/README.md), recorded with bundle `sjl-3-001`'s part B run on
  it. On 2026-11-18 the owner was asked "SJL-14 clause 6 (Broiler.JS's parse measurement, part I of the sjl-14-001
  rule) was deferred on 2026-11-03 until a controlled lane exists. Now that win-x64-r7-5800x is recorded, what should
  happen?" and chose "Take it on the lane": "Part I is taken on win-x64-r7-5800x as written, the way part B was. I
  write a short brief for a local session on your workstation; you run it there." Not taken: "Keep it deferred":
  "Clause 6 stays unmet. It is still listed under trigger (b), and nothing is run." This is the owner's own dated
  choice, not one taken under the owner's direction of 2026-10-18. **Part I is to be taken as written on that lane, and
  not yet**: its arms are "B the base commit's build; A the after commit's", and neither commit exists. W2, the after
  commit, is held for SJL-11.5 (the commits above), so part I is not taken before W2 merges. Until then part I has no
  run, and stays as recorded here (void on a lane that is not controlled), and clause 6 is unmet. Part I names no RID: it is judged "on the lane the bundle
  names with its effective configuration". [`decision-rule.md`](decision-rule.md) is not edited.)*
- **Exclusions:** none.

| Exclusion | What | Why |
|---|---|---|
| - | - | - |
