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
- `measurement/` - part I's runs, void on a lane that is not controlled (decision D48);
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
      `Broiler.JS/Broiler.JavaScript.BuiltIns`, `.BuiltIns.Tests` and `.Integration.Tests`.
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
    Each merge's paths are in [`bridge/merges-since-w1-paths.txt`](bridge/merges-since-w1-paths.txt). A read at the
    default branch's head, `e4b6b49e`, outside part B and not judged, finds the switch-off rows of part B's runs the same
    as W1's ([`bridge/README.md`](bridge/README.md)).
  - **W2 and the base commit** do not exist yet: W2 is held for SJL-11.5 (decision D51's recommended order;
    [`docs/roadmap/SharedLibraries.status.md`](../../roadmap/SharedLibraries.status.md)'s SJL-14 row).

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
the compile too. "Those conditions" are read as
the conditions the rule sets on the commits it names (W1 one merge, the only first-parent commit with the bridge, its
base descending from the rule commit), which hold; the definition is not one of them, so neither merge voids the part.
**Under this reading, part B is MET**: at W1 against `e2ed7fae`, at #1104 against `6f7284da` and at #1105 against
`29061d65`, both runs are complete and identical in every field (P's and K's outcome, exception type and message, and
P's and the for-loop pass's dump hashes), and W1's CI run of every test project of `Broiler.JS.slnx` passed, on Linux
and on Windows ([`bridge/identity/`](bridge/identity/), [`bridge/w1-ci/`](bridge/w1-ci/commands.txt)).

**The alternative, not taken: the rule's words.** Neither #1104 nor #1105 is a dark commit, as each changes more than
`Frontend*/`; part B is judged at W1 alone, and those merges are product merges outside part B, like #1107, #1108,
#1110, #1114 and #1115, which part C at the base commit covers. **Under it, part B is MET** as well, at W1; the runs at
#1104 and #1105 stay retained and read. A third reading, that the merges' failing the definition voids the part,
is not taken by either: the definition says which merges are dark commits, not what W1 or its base must be.

**For the owner, an optional ruling:** "D52's reading" (taken: each merge to `Frontend*/` after W1 is a dark commit
for part B, whatever else it changes) or "The rule's words" (only a merge that changes nothing but `Frontend*/`).
Either gives the same verdict on these runs; the ruling decides what a later library merge that also touches other
paths must carry. Nothing is accepted: no second holder exists to review it, and part B is no clause; every clause of
this engine's, 1 to 6, is unmet.

**The switch-on outputs**, read and not judged (part C judges them at the base commit): the side-by-side tool's B and
K-on rows are the same at #1104's first parent, at #1104, at #1105, and at W1. Neither library merge moved a switch-on
outcome, message or tree over the corpus.

## The side-by-side run at W1, read on 2026-11-15

[`side-by-side/w1/`](side-by-side/w1/README.md) holds the side-by-side tool's run at W1, at P, B, K and K-on with the
dumps, as the tool's README and the rule's part C say, read and not judged. What it finds, in words (each count is in
the folder): every outcome move the corrections list goes to the outcome they expect; the moves they do not list take
class E's form (P accepts, B refuses, K refuses on both sides) or class C3's (nesting past the library's bound), but for
inputs of this engine's own parser tests that the old path accepted and the text refuses, corrections no row
lists yet, for part C to classify at the base commit; many refusals change their text, as decision D46 expects, in the
generated message tables; every tree difference of P and B is of spans alone, class S's form or two readings of one
construct's extent, but for the inputs corrections W1a to W1c cite. **No library defect is found, and the run asks
for no dark merge.** Its other runs at W1, every test project with the switch on and test262 locally with the
switch off and on, are not taken here: the rule reads them at W1 only, and part C judges them at the base commit, where
this stage leaves them (the choice taken, under the owner's direction of 2026-10-18; the alternative, taking them at W1
now, is left to a later stage if the owner wants them).

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
  owner and makes none.)*
- **Exclusions:** none.

| Exclusion | What | Why |
|---|---|---|
| - | - | - |
