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
| [`base/part-a/`](base/part-a/imported.txt) | Part A's base run, `.github/workflows/test262-evidence.yml` run 44639106095 at `69a8a13b`, its artifact's entries imported unchanged (the one over 2 MB by its digest), with its run's artifacts and digests, the jobs' runner images and `hashes.txt`; read, not judged *(added 2026-11-22, during W2's base runs)* |
| [`side-by-side/w1/tests-and-test262/`](side-by-side/w1/tests-and-test262/README.md) | Part C's other runs at W1, the test projects and test262 with the switch off and on, taken during W2's base phase, with their logs, machine records, scripts and `hashes.txt` (the files over 2 MB by digest), and a reading of them that classifies nothing; read, not judged *(added 2026-11-22, during W2's base runs)* |
| [`base/local/`](base/local/README.md) | The base commit's local runs at `69a8a13b`: part C's side-by-side tool, test projects and test262 with the switch off and on; part D's tool; part E's collector; part G's base candidate feed with Broiler.JSeal's and Broiler.HtmlBridge's suites against it; part H's API lister; with the chain's logs, machine records, scripts and `hashes.txt` (the files over 2 MB by digest) *(added 2026-11-23, during W2's hold)* |
| [`base/part-c/`](base/part-c/README.md) | Part C at the base commit: every difference of those runs with its class, or unclassified with its reading, the generated message tables of class M, and the scripts that wrote them; part C read NOT MET on the retained runs *(added 2026-11-23, during W2's hold)* |

## What it will hold

Each named by the rule, retained when its run is taken, with the commit it ran at:
- `bridge/` - part B's identity runs at W1 and its first parent, and at each dark commit and its first parent, with
  their switch-on outputs (decision D52); *(Noted 2026-11-15: taken, with W1's CI run, at W1 and at the library
  merges since W1; see the table above and "Part B, judged on 2026-11-15".)*
- `side-by-side/w1/` and `side-by-side/w2-base/` - part C's runs, every difference classified, with the generated
  message table of class M (decision D46), written by the side-by-side tool, `tools/side-by-side/`, which W1 adds;
  *(Noted 2026-11-15: `side-by-side/w1/` is taken, the tool's run, read and not judged, with no difference classified,
  since the rule classifies only at the base commit; `side-by-side/w2-base/` waits for the base commit.)*
  *(Noted 2026-11-22, during W2's base runs: the note before this one reads "`side-by-side/w1/` is taken, the tool's
  run". Part C's other runs at W1 are taken too and retained in [`side-by-side/w1/tests-and-test262/`](side-by-side/w1/tests-and-test262/README.md), read and not
  judged ("W1's owed part C runs, retained" below). `side-by-side/w2-base/` still waits: the base commit's part C runs
  are being taken and are not finished.)* *(Noted 2026-11-23, during W2's hold: the note before this one reads
  "`side-by-side/w2-base/` still waits". The base commit's part C runs are taken and retained in
  [`base/local/`](base/local/README.md), beside the other base runs, and their differences are classified in
  [`base/part-c/`](base/part-c/README.md), with the generated message tables of class M; the folder this line names
  `side-by-side/w2-base/` is not made. See "W2's base, recorded on 2026-11-23" below.)*
- `base/` and `after/` - part A's hosted runs (UTC, negative tests), imported unchanged with their digests, and the
  comparison; parts D, E, F, G, H and K's local runs; *(Noted 2026-11-22: part A's base run is taken and retained in
  `base/part-a/`, "Part A's base run, retained" below; this line's other runs are not yet in `base/`.)* *(Noted
  2026-11-23: the note before this one reads "this line's other runs are not yet in `base/`". The base runs of parts
  D, E, G and H are taken and retained in [`base/local/`](base/local/README.md); parts F and K have no base run, as
  each is taken at the after commit alone. Nothing is in `after/`.)*
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

    *(Noted 2026-11-19, after SJL-11.5's merge and before W2's base: the lists above stop at the default branch's head
    of 2026-11-15, and read "Every other first-parent merge since W1 changes no product path (#1103, #1106, #1109,
    #1111, #1112, #1113, #1116)". Through `fefb4f9c9331b7d16349ed2a1721285f2fb69fef`, the merge of
    Broiler-Platform/Broiler.JS#1127 and the default branch's head on 2026-11-19, the first-parent commits after
    `e4b6b49e` add these, marked by the same list of the product paths. None changes
    `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*/`, so the merges to it since W1 are still #1104 and #1105 alone.*
    - *The product commits, which change no `Frontend*/` path and so are no dark commit by any reading:*
      - *`2f012739cfc4d99e91b22d28a09941c49d8bd646`, Broiler-Platform/Broiler.JS#1118 (SJL-17.5):
        `Broiler.ECMAScript/Broiler.ECMAScript.Conformance.Tests`, and `Broiler.JS/Broiler.JavaScript.BuiltIns`, its
        `.BuiltIns.Tests` and `.Integration.Tests`.*
      - *`cdf5559d5b69113c536f734e9a6ab305a3c71430`, "Update dependencies", a commit made on the default branch
        directly and not a merge (its only parent is `2f012739`): the project files of `Broiler.JS/Broiler.JavaScript`
        and of its projects `.All`, `.Ast`, `.BuiltIns`, `.Clr`, `.Compiler`, `.Debugger`, `.Engine`,
        `.ExpressionCompiler`, `.Expressions`, `.Extensions`, `.Globals`, `.LinqExpressions`, `.Minimal`,
        `.ModuleExtensions`, `.Modules`, `.Parser`, `.Portable`, `.Portable.Compiler`, `.Runtime` and `.Storage`, and the
        root's `Directory.Packages.props`.*
      - *`6fda6ca44c041cea348dd5302c91ff90a0fe67bc`, Broiler-Platform/Broiler.JS#1119: `.BuiltIns`
        (`RegExp/JSRegExp.Broiler.cs`).*
      - *`96cbe4ce39f03e17e061277104898e4071b99cb2`, Broiler-Platform/Broiler.JS#1122: `.BuiltIns.Tests`, `.Runtime`,
        `.Runtime.Tests` and `.Storage`.*
      - *`b2b226cd3158f0b7d990a3f4fecbe75b259b4fa1`, Broiler-Platform/Broiler.JS#1126 (SJL-11.5): `.Ast`, `.BuiltIns`,
        `.BuiltIns.Tests`, `.Integration.Tests`, `.Parser` (`Bridge/BridgeRegularExpressions.cs`,
        `Bridge/BridgeTreeBuilder.Expressions.cs`, `FastScanner.cs` and the project file, and `RegExpValidator.cs`
        deleted), `.Parser.Tests` and `.Runtime`. It is the one first-parent commit after W1 that changes
        `Parser/Bridge/`; how the rule's conditions read it is reading B1 in "Before W2's base, read on 2026-11-19"
        below.*
    - *No product path: #1117, #1120, #1121, #1123, #1124, #1125 and #1127.*

    *Each is in [`bridge/merges-since-w1-paths.txt`](bridge/merges-since-w1-paths.txt), extended to `fefb4f9c` by the
    same command form as its first lines (each first-parent commit's paths against its first parent, renames as git
    detects them), whose lines through #1116 are unchanged; the direct commit's header names it `direct` where a
    merge's names its pull request. No first-parent commit after `e4b6b49e` changes, in a product path, a line that
    names `SharedSyntaxBridge`, `ParseThroughBridge`, `BridgeByDefault` or `BROILER_PARSE_THROUGH_BRIDGE`; two records
    merges, #1117 and #1123, change lines of this bundle and of the plan's tables that name them. The lists above are
    left as written.)* *(Changed the same day, before this note merged, after review: the sentence before the last read
    "No first-parent commit after `e4b6b49e` changes `SharedSyntaxBridge`, `ParseThroughBridge`, `BridgeByDefault` or
    `BROILER_PARSE_THROUGH_BRIDGE`.", which holds only for product paths.)*
  - **W2 and the base commit** do not exist yet: W2 is held for SJL-11.5 (decision D51's recommended order;
    [`docs/roadmap/SharedLibraries.status.md`](../../roadmap/SharedLibraries.status.md)'s SJL-14 row). The rule's dark
    commits run to the base commit, so their set is still open, and part B's verdict below holds only at the commits
    taken. *(Noted 2026-11-19, after SJL-11.5's merge: this bullet reads "W2 is held for SJL-11.5 (decision D51's
    recommended order". SJL-11.5 merged on 2026-11-19 as Broiler-Platform/Broiler.JS#1126,
    `b2b226cd3158f0b7d990a3f4fecbe75b259b4fa1`, and bundle [`sjl-11-001`](../sjl-11-001/README.md) records with it
    that "The hold of decision D16 ended" with that merge; its after records merged as Broiler-Platform/Broiler.JS#1127,
    `fefb4f9c`. That merge is the end of what held W2: W2 is no longer held for SJL-11.5 (reading B2 in "Before W2's
    base, read on 2026-11-19" below). W2 and the base commit still do not exist, and no dark commit has merged since
    #1105. From the first base run until W2 merges, the default branch is held for W2 by the owner's answer 9 of
    2026-11-19 (the same section); that hold has not started. The rest of this bullet stands.)* *(Changed the same
    day, before this note merged, after review: it read "From the first part C base run until W2 merges", the
    question's words; the chosen answer reads "The hold then starts at the first base run".)* *(Noted 2026-11-22,
    during W2's base runs: the note above reads "that hold has not started". It started on 2026-11-19 with the first
    base run, and the default branch's head has been `69a8a13bc446a1714d3c8be9be3cfd742aebee6e` since; see "The hold's
    start" in "W2's base, recorded on 2026-11-22" below. W2 does not exist yet.)*

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

*(Noted 2026-11-19, before W2's base: the optional ruling above also takes a second question, reading B1 in "Before
W2's base, read on 2026-11-19" below: whether SJL-11.5's merge `b2b226cd`, which changed `Parser/Bridge/` and deleted
three items of SJL-14.5's deletion list, is read with decision D51 ("Read with D51", taken: W1's condition holds
through `fefb4f9c`, W2's is checked at W2, and part B's verdict above stands) or by the rule's words alone ("The
rule's words": W1's condition fails, part B, judged at W1, is void, and so is every part judged at the base commit or
W2). The ruling stays optional and blocks nothing; the paragraph above is left as written.)* *(Changed the same day,
before this note merged, after review: the taken reading's parenthesis read "taken: W1's and W2's conditions hold, and
part B's verdict above stands", which says W2's condition holds before W2 exists, and the other's read "W1's condition
fails, part B is void, and so is every part judged at W1 or W2", worded as reading B1 now words it.)*

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
*(Noted 2026-11-22, during W2's base runs: the paragraph above reads "they are still to be taken". They were taken at
W1 from 2026-11-19 to 2026-11-22, before part C's runs at the base commit began, as the choice above has it, and are
retained in [`side-by-side/w1/tests-and-test262/`](side-by-side/w1/tests-and-test262/README.md); see "W1's owed part C runs, retained" in "W2's base, recorded on
2026-11-22" below. They are read, not judged.)*

## Before W2's base, read on 2026-11-19

**Written:** 2026-11-19, after SJL-11.5's merge (`b2b226cd`) and its after records (`fefb4f9c`, the default branch's
head), before W2, its base commit or any of their runs exists. [`decision-rule.md`](decision-rule.md) is not edited,
and nothing here revises a line of it. Nothing is judged, met or accepted here.

### The owner's answer 9 of 2026-11-19: the hold for W2

The owner's own answer, quoted verbatim, and not one taken under the owner's direction of 2026-10-18. Asked (2026-11-19):
"SJL-14's W2 cut-over (the frontend switch removed, the re-scans and facades deleted, the division fallback deleted)
needs its base runs taken at Broiler.JS main's head, with W2 as the very next merge. Any merge in between, including
docs-only ones, voids those base runs. For SJL-11.5 you ordered that hold yourself. May I hold Broiler.JS main the same
way for W2, from the first part C base run until W2 merges? That is expected to take a day or two." Chosen: "Allow the
W2 hold" — "Records and the W2 branch are prepared first, with nothing held. The hold then starts at the first base
run, and nothing merges to Broiler.JS main until W2 merges. I record the hold's start and end, and I dispatch
test262-evidence.yml for part A once at the base and once at the after commit, as for SJL-11.5." Not taken: "Prepare
only, hold later" — "I prepare the records and the W2 branch but take no base run. The hold waits until you say it can
start."

What it means here:
- **W2's hold begins at the first base run and ends with W2's merge.** The question put it as "from the first part C
  base run"; the chosen answer reads "The hold then starts at the first base run", so it starts at the first base run
  of any part, a dispatch of part A's included if one comes first. Nothing merges to the default branch meanwhile,
  records included, so that W2's first parent is the commit the base runs were taken at, as the rule's definition of
  the base commit, W2's first parent, and its void sentence need. Its start and its end are recorded in this bundle
  when each happens. **It has not started**: this record and the W2 branch come first, with nothing held. *(Noted
  2026-11-22: the sentence before this note reads "It has not started"; the hold started on 2026-11-19, as "The
  hold's start" in "W2's base, recorded on 2026-11-22" below records.)* *(Changed
  the same day, before this record merged, after review: the bold sentence read "W2's hold begins at the first part C
  base run and ends with W2's merge.", the question's words and not the answer's.)*
- **Part A's base and after runs are taken by dispatches of `test262-evidence.yml`**, once at the base commit and once
  at the after commit, as for SJL-11.5, with the inputs part A names; the workflow is not changed.
- The freeze of the default branch for the base is thereby the owner's own order, as SJL-11.5's was; no choice is
  taken for it under the direction of 2026-10-18.

### The session's readings, under the owner's direction of 2026-10-18

Each is taken as recommended under the owner's direction of 2026-10-18, "Please do all recommended decisions and skip
waiting for review.", which changes no rule; each is a dated reading filed before any base run, with the alternative
not taken, and none revises a line of the rule.

**B1. The rule's commit conditions, read against SJL-11.5's merge.** The rule reads: "W1 is the only first-parent
commit that carries any part of the bridge or the switch"; "W2 is the only first-parent commit that carries any part of
SJL-14.3 to SJL-14.5 (the switch's removal, the adapter packages, the portable compiler's and the module analysis's
move, the deletions)"; and "A part whose base was taken at any other commit, or whose commits fail those conditions, is
void." SJL-11.5's merge `b2b226cd` changed `Parser/Bridge/BridgeRegularExpressions.cs`, whose `Validate` now calls
`FastScanner.IsValidRegularExpressionLiteral`, Broiler.Regex's validate entry, in place of the parenthesis-balance patch,
the braced-escape decoding and `RegExpValidator`, and `Bridge/BridgeTreeBuilder.Expressions.cs`, whose literal token now
carries the body as written; and it deleted `RegExpValidator`, `DecodeBracedUnicodeEscapes` and the parenthesis-balance
patch, three items of SJL-14.5's deletion list ([`dispositions.md`](dispositions.md)). Decision D51
([`docs/roadmap/SharedLibraries.md`](../../roadmap/SharedLibraries.md)) reads: "**W2 after SJL-11.5**: the provider then
wraps Broiler.Regex's validate entry, and SJL-14 deletes the division fallback only." Broiler.VM's milestone file reads:
"**"`RegExpValidator`"** is deleted if SJL-11.5 has landed when the cut-over merges." `dispositions.md` reads, for
`RegExpValidator`: "deleted if SJL-11.5 has landed, else relocated behind the request's regular-expression provider
(decision D51)".
- **Taken: the conditions read with D51.** The provider change and the deletions `b2b226cd` made are the ones D51's
  order gives SJL-11.5 to make, not a part of SJL-14.2's bridge or of SJL-14.3 to SJL-14.5. W1 is then still the only
  first-parent commit that carries any part of SJL-14.2's bridge or switch, through `fefb4f9c`, and W2's condition is
  checked at W2, with the division fallback the only regular-expression item SJL-14 deletes. Part K, at the after
  commit, finds the three items absent, as its population allows ("each deleted, or relocated behind the provider as
  decision D51 allows and dispositions.md records"). Of [`corrections.md`](corrections.md)'s rows, K9 ("only if the
  provider wraps Broiler.Regex's validate entry") applies and K6 ("only if SJL-11.5 has not landed at the cut-over
  (decision D51)") does not. Part B's verdict above stands at the commits taken.
- **Not taken: the rule's words alone.** `b2b226cd` is a first-parent commit that carries part of the bridge, so W1's
  condition fails; it also carries deletions SJL-14.5 lists, so W2's condition fails before W2 exists. By the void
  sentence, part B, judged at W1, is void, and so is every part judged at the base commit or W2.
- **Added to the owner's optional ruling** in "Part B, judged on 2026-11-15" above, by its note of today.

*(Changed the same day, before this reading merged, after review; the reading taken is not changed.)* K6's quotation
read "only if SJL-11.5 has not landed (D51)", the plan's summary of the row and not `corrections.md`'s words, and it was
attributed to "the corrections"; the alternative's last sentence read "it also carries deletions SJL-14.5 lists, so W2's
condition would fail before W2 exists, and every part judged at the base commit and W2 would be void", and its first
"so W1's condition fails, and by the void sentence part B, judged at W1, is void;", now worded one way here, in the note
on the owner's ruling above and in the plan.

**B2. The hold that held W2 has ended.** The plan's order of SJL-14's work reads "(each merge outside SJL-11.5's span
and SJL-9.5's window)" and "**W2**, preferably after SJL-11.5", and this bundle's commits read "W2 is held for
SJL-11.5". SJL-9.5's window is past: it merged as `a5f2692e` (#1110, the commits above).
- **Taken:** "SJL-11.5's span", which no record defines, is read as the hold of decision D16 under which SJL-11.5's
  bases were taken, which bundle `sjl-11-001` records ended with SJL-11.5's merge `b2b226cd`. The hold that held W2 has
  ended, and W2 may be written, its base taken and W2 merged under the owner's answer 9 above. SJL-11.5's part B, still
  to be taken on the owner's lane, builds arm B from the base commit `5429c406` and arm A from the merge
  (bundle `sjl-11-001`'s README, "Part B, by answer 4"), so it needs no hold of the default branch.
- **Not taken:** reading SJL-11.5's span as lasting until SJL-11.5's part B is taken on the lane, so that W2 waits
  for that run.

**B3** is not a reading here: the freeze of the default branch for the base is the owner's answer 9 above.

**B4. W2 before any merge of SJL-15.2's model.**
- **Taken:** W2 merges before any SJL-15.2 model merge. None can merge yet in any case: decision D-js-revisions' note
  of 2026-11-17 puts the eval-code inputs of probes V029, V030 and V046 in "a later corpus revision of the area,
  classified on both engines' runs as revision 43's additions were, before any SJL-15.2 code merges", and no such
  revision exists; and from the first base run until W2 merges, nothing merges (answer 9). With W2 first, no
  model merge is a dark commit of this rule, and the base commit's `Broiler.ECMAScript/` is the published
  `0.1.0-preview.12`'s (below) unless a dark merge changes it. *(Changed the same day, before this reading merged,
  after review: "from the first base run" read "from the first part C base run", as answer 9's question and not its
  answer has it.)*
- **Not taken:** model merges before W2, each a dark window of SJL-14 under decision D52 ("The same holds for SJL-15's
  model stages"), each with part B against its own first parent, its switch-on outputs judged by part C at the base
  commit.

**B9. Part C's open items, and K10 rows, before the base run.** "The side-by-side run at W1, read on 2026-11-15" above
lists the open items: "the outcome moves the corrections do not list, those three inputs among them, and the
`both-agree` span differences, each to be classified or found unclassified there".
The rule reads, of the rows it classifies by: "The corrections this rule classifies by are `corrections.md`'s K1 to
K10, each with its rows of Broiler.VM's `docs/evidence/sjl-13-001/correspondence.md`, or of this bundle's
inherited-context probes (`inherited-context/`, rows IC1 to IC7 of K10), and its test262 paths". Part C's "How it is
read" reads: "The run at W1 is retained so that a library fix found there can be traced to its dark merge. An
unclassified difference is a bridge or library defect, fixed as a dark merge and run again before W2 merges."
- **Taken:** they are classified at the base commit, from part C's runs there. Any row of K10 they need, found from the
  retained run at W1, is added to [`corrections.md`](corrections.md) (and its generated tables by
  [`tools/records/`](tools/records/write_records.py)) before the first part C base run, as K10's rows W1a to W1c were
  added after W1's run. Such a row goes beyond the rule's words above: it is a row neither of the correspondence nor of
  the inherited-context probes, but SJL-14's own, as `corrections.md`'s note of 2026-11-03 says of W1a to W1c ("K10
  stays "the rows SJL-14's correspondence adds" as filed; these three are SJL-14's own, found by its first run."). It
  is also chosen after the run it classifies was seen: the side-by-side run at W1, whose switch-on outputs part C judges
  at the base commit, has been read (above), and part B's runs were judged on 2026-11-15; no run at the base commit
  exists. This reading revises no line of the rule, so the sentence of its section "Revisions" does not apply to it,
  but a row written after its run was seen is chosen knowing what it admits: it is weaker evidence than a row filed
  before any run, and each such row names its inputs and its cause, as W1a to W1c do, so that it can be checked
  against both texts. An item no class admits at the base commit is unclassified, and by part C's "How it is read" is
  "fixed as a dark merge and run again before W2 merges". From the first base run nothing merges (answer 9), so a fix
  by a dark merge ends the base taken: the dark merge is read by part B against its own first parent, and the base
  runs are taken again at it. Whether such a fix may go inside W2 instead is not read here.
- **Not taken:** a dated revision of the rule that classifies them, which its section "Revisions" makes weaker
  evidence: "A rule revised after any run it judges has been seen ... its decision is then weaker evidence", and runs
  it judges have been seen. Nor the rule's words alone, under which no row beyond the correspondence's and the probes'
  is added and every item none of their rows admits is unclassified.
- **For the owner, an optional ruling** among the three: "Rows from W1's run" (taken), "A dated revision" or "The rule's
  words". It blocks nothing; a ruling given before the first part C base run decides how the open items are classified
  there.

*(Changed the same day, before this reading merged, after review.)* The reading quoted neither the rule's sentence on
the rows it classifies by nor part C's "How it is read", and its taken bullet read, after "as K10's rows W1a to W1c were
added after W1's run", "; no part C, D or A run has been judged. An item no class admits at the base commit is
unclassified, which part C's "How it is read" sends to a fix before W2 merges."; that hid that the rows go beyond the
rule's words, that they are chosen after W1's run was seen, and that a fix by a dark merge ends a base taken under the
hold. The alternative read "a dated revision of the rule that classifies them, which its section "Revisions" makes
weaker evidence once any run it judges has been seen." The optional ruling is new.

**B10. Part I's protocol, filed before any part I run.** Part I reads: "configuration: the harness
Broiler.JS/docs/roadmap/Measurement.md names, on the lane the bundle names with its effective configuration;
interleaved arms; repetitions and warm-up as Measurement.md fixes them". Part I is the owner's run on the lane
`win-x64-r7-5800x`, taken after W2 merges (the note of 2026-11-18 under "What it does not claim"); these points go into
the brief for it:
- **The harness and its path.** No `Broiler.JS/docs/roadmap/Measurement.md` exists; the harness is the one
  [`docs/roadmap/Measurement.md`](../../roadmap/Measurement.md) names, the benchmark project's `--compile-phases`, over
  the real corpora of `CompilePhaseMetrics` that [`inputs/workloads/README.md`](inputs/workloads/README.md) names.
- **Repetitions and their order:** nine, and in each, B, A and A-again each run once, in an order that rotates by
  repetition: B, A, A-again; then A, A-again, B; then A-again, B, A; and again. Measurement.md's acceptance gate, which
  it calls "the single normative acceptance protocol", reads in its item 1: "run candidate and control on the same
  controlled host in a balanced, process-isolated order such as ABBA/BABA; use enough independent pairs for the
  predeclared analysis and repeat a provisionally qualifying result in a fresh confirmation run before acceptance;".
  The order is balanced: with a count that is a multiple of three, each arm runs first, second and third in a third of
  the repetitions. It is process-isolated: each arm's run in each repetition is its own process, over one corpus
  (below). Its item 4 reads: "include every configured repetition in the analysis. Two repetitions are the current
  wiring minimum, not proof that a lane or effect is decision-grade"; every repetition is in Q. Bundle `sjl-3-001`'s
  part B on the same lane took seven ("Seven repetitions. In each, B, A and A-again each run once, in an order that
  rotates by repetition",
  [`measurement-win-x64-r7-5800x/`](../sjl-3-001/measurement-win-x64-r7-5800x/README.md)), under which B runs first in
  the first, fourth and seventh and the others in two each, which is not balanced; nine, the next multiple of three,
  is the session's choice.
- **The harness's own repetitions:** `--compile-phases` takes repetitions as its third argument (Measurement.md:
  "Third argument is repetitions.") and reports each column as the median of that process's repetitions
  (`CompilePhaseMetrics`' `Repeat`); it is passed five, as Measurement.md's example passes it
  (`--compile-phases /path/to/octane 5 codeload-jquery`), the session's choice. **Q**, part I's "the median per
  workload and arm", is the median, over an arm's nine processes, of each process's reported median for that workload;
  N and D are read from those Q as the rule writes them.
- **A fresh confirmation run**, as item 1 asks: if the first run would read part I MET, the whole protocol is taken a
  second time, in fresh processes on the same lane, before part I is read; both runs are retained, and a reading that
  differs between them goes to the owner.
- **Warm-up:** the harness's own order. Measurement.md fixes no warm-up for this harness beyond "`--compile-phases`
  takes its end-to-end check first, and that ordering is the measurement.", and none is added.
- **One corpus per process**, `--compile-phases`' rule as the workloads' README and Measurement.md state it.
- **The manifest**, written before the first run, names the lane's folder and the SHA-256 of its `machine.txt`, as the
  [lane's record](../lanes/win-x64-r7-5800x/README.md) asks: "A sub-bundle measured on the lane names the folder and
  the SHA-256 of its `machine.txt` in its own manifest, written before its first run".
- **Each arm runs its own commit's harness**: arm B the benchmark project at the base commit, arms A and A-again at the
  after commit. How the after commit's harness reaches the quantity part I names after the cut-over is W2's own change
  to record.
- **Not weaker evidence, by the rule's words.** Its section "Revisions" reads: "A rule revised after any run it judges
  has been seen is a new dated file in this directory. It quotes this one whole and says it was written afterwards, and
  its decision is then weaker evidence." These points revise no line of the rule, so that sentence does not apply. Runs
  the rule judges have been seen (part B's, judged on 2026-11-15, and the side-by-side run at W1, read), so a revision
  filed now would be weaker evidence; these points are not one. Filing them before any part I run is what
  Measurement.md's "predeclared analysis" asks. Where Measurement.md fixes a floor or nothing, the choice above is the
  session's, recorded as one.
- **Not taken:** putting each point to the owner before the run; and seven repetitions, as bundle `sjl-3-001`'s part B
  took them. The owner may override any point before part I's first run.

*(Changed the same day, before this record merged, after review.)* The repetitions read "seven, and in each, B, A and
A-again each run once, in an order that rotates by repetition, as bundle `sjl-3-001`'s part B was taken on the same
lane (...). Measurement.md fixes only a floor: "Two repetitions are the current wiring minimum, not proof that a lane or
effect is decision-grade"", which quoted Measurement.md's item 4 alone and not its item 1, and seven in rotation over
three arms is not balanced; the harness's own repetitions, which median Q is, and the confirmation run were not named.
The sentence on weight read "These points revise no line of the rule, and they are filed before any part I run exists,
so neither condition of that sentence holds.", which read the sentence's "any run it judges" as a part I run, where
the rule judges every part and runs it judges have been seen; the conclusion rests on the first condition alone.

### Part G's commits, named before the base commit exists

Part G reads: "population: Broiler.JSeal's test suites and Broiler.HtmlBridge's, at the commits the bundle names, read
before the base commit exists". The commits, named and read on 2026-11-19, before the base commit exists, each the head
of its repository's default branch `main` that day (read from read-only clones, and the same head by `git ls-remote`):
- **Broiler.JSeal**: `a1216a80034e185d69d0d76d9ef855105698c00b`.
- **Broiler.HtmlBridge**: `334974f90c655181bba2ea6587ebb65336154c5a`.

What part G asks, as it says it: "evidence class: consumer parity (not a measurement)"; "configuration: two candidate
feeds, packed by eng/pack.ps1 at the base commit and at the after commit; Broiler.JSeal's eng/test-package-consumer.ps1
with -CandidateFeed and -CandidateVersion for each, its pins unchanged; Broiler.HtmlBridge in a scratch copy whose
package source and Broiler.JavaScript versions are overridden for each candidate, never committed; every test's result
retained"; "comparison: per test, its result against the base candidate and against the after candidate"; "admitted
difference classes: none"; "MET if and only if both suites ran against both candidates, and every test that passes
against the base candidate passes against the after candidate (decision D49)."; and "NOT MET otherwise; and the part is
not judged while either suite has not been run against both candidates." Its "How it is read" adds: "A test that fails
against both candidates is recorded with its cause; the JSeal module adapter's reflection on members SJL-12.5 removed
is such a cause, and a finding of SJL-12.5's." *(Changed the same day, before this record merged, after review: the
evidence class was not quoted, the comparison, the classes and the last line were paraphrased, and "How it is read"'s
sentence was given in part and unattributed, as "A test that fails against both candidates is recorded with its
cause.")* At the commit named, Broiler.JSeal's `eng/test-package-consumer.ps1` takes `-CandidateFeed` and
`-CandidateVersion`, the second as "Family=version", and says "Default pins are never edited". Both suites run at these
two commits against both candidates; no later commit of either repository is part G's population.

### The shared stream's published version

The shared stream's `0.1.0-preview.12` (its packages, `Broiler.ECMAScript.Frontend` among them) is published on
nuget.org (2026-11-16) from `2f012739`, the merge of Broiler-Platform/Broiler.JS#1118: its tag
`ecmascript-v0.1.0-preview.12` and the Frontend package's repository record both name that commit, and `git diff ecmascript-v0.1.0-preview.12 fefb4f9c -- Broiler.ECMAScript` is empty, so the default
branch's `Broiler.ECMAScript/` is that version's sources. Decision D30 (Broiler.VM's JSD-0083) reads: "Broiler.JS packs
the frontend for SJL-14's pristine consumer from its own commit; a Broiler.JS release that carries the bridge waits for
a published version. The publication itself is the owner's own choice, not taken here." W2's merge waits for no
publication, and a published version whose sources equal the default branch's exists while `Broiler.ECMAScript/` does
not change. The source's version suffix still reads `preview.8` (`Broiler.ECMAScript/Directory.Build.props`); the
version W2's packages name is W2's own change to record.

Two texts of [`docs/public-api.md`](../../public-api.md) no longer hold: the heading of SJL-11.5's section, "on branch
`sjl-11-5`, not merged" (SJL-11.5 merged as `b2b226cd`, #1126, with that section as written), and W1's item on the
package dependencies, "which has no published version yet" (of `Broiler.ECMAScript.Frontend`). Both are left as
written here: `docs/public-api.md` is outside CI's list of records, and the plan lists it among W2's own changes, so
W2 corrects each by a dated note.

*(Changed the same day, before this section merged, after review: its first sentence began "`Broiler.ECMAScript`
`0.1.0-preview.12` is published on nuget.org", naming a package id nuget.org does not have, and read "its package's
repository record"; and the paragraph on `docs/public-api.md` is new, the notes that an earlier commit of this change
added to that file having been taken out of it.)*

## W2's base, recorded on 2026-11-22

**Written:** 2026-11-22, during W2's base runs, on branch `sjl-14-w2-base`, with the default branch's head at
`69a8a13bc446a1714d3c8be9be3cfd742aebee6e` (read by `git ls-remote` at 2026-11-22T14:02:16Z).
[`decision-rule.md`](decision-rule.md) is not edited, nothing here revises a line of it, and nothing is judged, met or
accepted here.

### The hold's start

- **The hold of the owner's answer 9 started on 2026-11-19, with the first base run.** Answer 9's chosen answer, in
  short: "The hold then starts at the first base run, and nothing merges to Broiler.JS main until W2 merges." (whole in
  "The owner's answer 9 of 2026-11-19: the hold for W2" above). The first run of W2's base phase was W1's part C runs
  still owed, "the same runs at W1, retained and read, not judged" in part C's configuration, which began at
  2026-11-19T17:51:58Z. Their driver reads the default branch's head by `git ls-remote` before each step and stops if
  it is not `69a8a13b`; its log reads `main=69a8a13bc446a1714d3c8be9be3cfd742aebee6e` before every step (the stage's
  `w1/driver.log`, with `w1/logs/runs.tsv` and `w1/logs/commands.log`, in its run folder outside this repository until
  those runs are retained). *(Noted 2026-11-22, later: the parenthesis before this note reads "in its run folder outside
  this repository until those runs are retained". They are retained now, and those logs with them, at the same paths
  under [`side-by-side/w1/tests-and-test262/`](side-by-side/w1/tests-and-test262/driver.log).)* Part A's base run, the first run at `69a8a13b` itself, was dispatched under the same hold at
  2026-11-19T23:36:38Z ("Part A's base run, retained" below).
- **The commit the base runs are taken at** is `69a8a13bc446a1714d3c8be9be3cfd742aebee6e`, the merge of
  Broiler-Platform/Broiler.JS#1128 (its first parent `fefb4f9c`, the merge of Broiler-Platform/Broiler.JS#1127), the
  default branch's head when the hold started and through this note. It descends from W1 `8e5519d8` and from the rule
  commit's merge `d8f14602`. The rule reads: "**The after commit (W2)** is the default-branch merge that lands
  SJL-14.3 to SJL-14.5, one merge. **The base commit** is its first parent, which descends from W1 and from the rule
  commit"; part A's configuration reads "the base run at the base commit and the after run at the after commit"; and
  "A part whose base was taken at any other commit, or whose commits fail those conditions, is void." So `69a8a13b` is
  the base commit when W2 merges with it as its first parent, and nothing merges to the default branch before W2,
  records included.
- **Three container restarts fell within the stage, and two of them interrupted its runs.** The stage's machine
  snapshots (`w1/logs/*.machine.txt`, each snapshot's time less the age of process 1) give three boots of the machine:
  2026-11-19T17:40:58Z, 2026-11-19T23:35:11Z and 2026-11-22T13:59:07Z. The interrupted runs were resumed from their
  per-step markers, the step that was running taken again from its start, with the default branch's head read before
  every step:
  - on 2026-11-19, with the boot of 17:40:58Z, after the stage's brief was written (16:41:39Z, by the file's time) and
    before the stage's worktree was made (17:43:25Z, by its reflog) and its first step began (17:51:58Z). It
    interrupted no run.
  - on 2026-11-19, with the boot of 23:35:11Z, during W1's switch-on test262 shard 1 (step `t262-on 1`, begun at
    23:10:21Z). The stage's agent was lost to it; the runs went on from 23:36:24Z under one resumable chain of the same
    drivers, which read the head `69a8a13b` and took that shard again from its start at 23:36:37Z.
    `w1/logs/commands.log` names the step twice, and `w1/logs/runs.tsv` keeps the completed run alone.
  - during W1's switch-on test-project step (`tests-on`), begun at 2026-11-20T04:02:25Z, at a time the logs do not
    show. The machine was still up after the step began: its session logs record a shutdown signal at
    2026-11-20T04:11:20Z, the last time they show before the restart, and its current boot reads
    2026-11-22T13:59:07Z. The chain was started again at 2026-11-22T14:00:07Z, read the head `69a8a13b` at 14:00:08Z,
    and took that step again from its start at 14:00:20Z.

  No step ran between an interruption and its resumption.

  *(Noted 2026-11-22, later: the bullet above reads "Three container restarts fell within the stage, and two of them
  interrupted its runs." More restarts have interrupted the chain since, on 2026-11-22 and during the base commit's
  switch-off test262 shard 6, after W1's runs had finished; see "The restarts that interrupted the runs" below.
  The bullet is left as written.)*

### Part A's base run, retained

- **The run.** `.github/workflows/test262-evidence.yml` as the default branch carries it at `69a8a13b`, blob
  `fc38229782ee1b2e93ecca9e990d182f5e44dbf2`: workflow run
  [44639106095](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/44639106095) (attempt 1), dispatched on
  2026-11-19 at 23:36:38Z on `main` with commit `69a8a13bc446a1714d3c8be9be3cfd742aebee6e`, suite_ref
  `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone
  empty and include_negative true, the inputs part A's configuration line fixes, and completed on 2026-11-20 at
  00:40:14Z. Its head commit, its `run.txt`'s dispatched ref and tested commit all read `69a8a13b`, and `run.txt`'s
  inputs line reads those inputs (time_zone empty is not echoed there). Its conclusion is success, and so is every
  job's, at `69a8a13b`, in attempt 1: prepare 135965203630, shard (0) 135965325817, shard (1) 135965325784, shard (2)
  135965325952, shard (3) 135965325759, shard (4) 135965325783, shard (5) 135965325875, shard (6) 135965325908, shard
  (7) 135965325802 and collect 135993015562, whose step "Require a complete run" is among them (GitHub's record of the
  run and its jobs, read 2026-11-22).
- **The artifacts.** Every artifact of the run was downloaded, and each zip's SHA-256 equals the digest GitHub records
  for it ([`part-a-artifacts.txt`](base/part-a/part-a-artifacts.txt), each with its id and digest). The evidence
  artifact `test262-evidence`, id 14588573558, is
  `sha256:12163b9df6e680ddfbd50b3f9356d9bae2f2e2c1cb417ace2045ca7223c647c1`; the zip is named by that digest and not
  committed.
- **Retained** in [`base/part-a/`](base/part-a/imported.txt) as bundle `sjl-11-001` retained its base part A: every
  entry of the evidence artifact unchanged, with [`imported.txt`](base/part-a/imported.txt), `runner-images.txt` (each
  job's runner image, from its own log), `part-a-artifacts.txt` and [`hashes.txt`](base/part-a/hashes.txt) added, and
  a `.gitattributes` so the bytes stay as committed. One entry differs from that bundle's way: `test262-results.json.gz`,
  over 2 MB, is retained by its digest only,
  `20b632f9ebeb49ff926201ca436a86a3b22747cfd3d5cc69bc332610e99480e1`, as `imported.txt` records; it is the entry of
  that name in the zip above, read from the zip after the zip's digest is checked. GitHub keeps the run's artifacts
  until 2027-02-17. `hashes.txt` names every committed file of the folder by its SHA-256 and is
  `a8ab124c2fa6cfc0f8f63bb6f326f0ecb984e3753aea34d44820e176cce81f24`.
- **Its own records** show one shell across the shards and the re-run (`shell-hashes.log`), every shard in the zone the
  merging job expects, `TZ` unset, UTC and tzdata 2026e (`host-zone.log`), and no incomplete shard (`merge.log`). The
  jobs ran on more than one version of the `ubuntu-24.04` image, as `runner-images.txt` lists; the rule fixes the shell
  and the zone, which the records show, and not the image version.
- **Read, not judged.** Part A compares the base run with the after run, and the after commit does not exist.

### W1's owed part C runs, retained

**Written:** 2026-11-22, during W2's base runs, on branch `sjl-14-w2-base`, with the default branch's head at
`69a8a13bc446a1714d3c8be9be3cfd742aebee6e` (read by `git ls-remote` before this record was committed). Nothing here is
judged, met or accepted, and nothing is classified: part C judges only the runs at the base commit.

**Which runs were owed.** Part C's configuration names, at the base commit, the side-by-side tool's run, "every test
project with the switch on through its internal options member" and "scripts/compliance/run_test262.py locally on the
programme's machine through the shell built at that commit with its hidden switch off and on, in UTC, with negative
tests, one worker per core", and then "the same runs at W1, retained and read, not judged"; its comparison reads "per
test, its result off and on; per test262 path and variant, its verdict off and on". The tool's run at W1 was taken on
2026-11-15 ([`side-by-side/w1/`](side-by-side/w1/README.md)); "The side-by-side run at W1, read on 2026-11-15" above
left the others "to a later stage, before or with part C's runs at the base commit". So these runs were owed at W1:
every test project with the switch on, and test262 with the shell's hidden switch off and on. The test projects were
also run with the switch off, the other side of the comparison's "per test, its result off and on". Each matches its
outputs as follows.

**Where, and how.** All at W1, `8e5519d8cf81808856e32f2275fcb4d3b98a73f3`, from a clean `git archive` of it on disk, on
the programme's machine (Linux; its processors in [`archive.txt`](side-by-side/w1/tests-and-test262/archive.txt)), each
step by `step.sh <commit> w1 <step>` through the machine-wide lock and idle gate, with the default branch's head read
before every step, and every test and test262 run under `env -i` with `TZ=UTC` and no `LANG`, `LC_*` or `BROILER_*`
variable. Retained in [`side-by-side/w1/tests-and-test262/`](side-by-side/w1/tests-and-test262/README.md), every file of
the run folder at the same path, but the files over 2 MB, retained by digest only
([`retained.txt`](side-by-side/w1/tests-and-test262/retained.txt),
[`not-retained.sha256.txt`](side-by-side/w1/tests-and-test262/not-retained.sha256.txt)), with the drivers and scripts as
they were on disk (the retained `drive.sh` is the file as it was from 2026-11-19T18:45:47Z, which the chain ran; the
version the first driver started with at 17:51:58Z is not on disk), a `.gitattributes`, and
[`hashes.txt`](side-by-side/w1/tests-and-test262/hashes.txt), SHA-256
`a05692a69063410597387b158bd960685ed879276fe4f7a8f6b3a92bd59a50b5`. The bundle's `manifest.json` (decision D56) is still
written at the evidence step, so the folder is named by its `hashes.txt`, as part B's and the tool's are; the tool's,
[`side-by-side/w1/hashes.txt`](side-by-side/w1/hashes.txt), names the tool run's files and not this folder's, and is
left as written but for its line for that folder's README, which now has dated notes on it. Each step's command line is
in [`logs/commands.log`](side-by-side/w1/tests-and-test262/logs/commands.log), its exit in `logs/runs.tsv`, and its
machine records before and after in `logs/<label>.machine.txt` and `logs/<label>.machine-sh.txt`. Every step's exit
is 0.
- **Every test project, the switch off** (step `tests-off`, 2026-11-19): `dotnet test <project> -c Release --no-build
  --logger trx` for each test project of `Broiler.JS.slnx` under `Broiler.JS/`, on the Release build of the solution
  with the compiler's test-only switch at its default (step `build-off`);
  [`tests-off/`](side-by-side/w1/tests-and-test262/tests-off/summary.txt), its projects' exits in `exits.tsv`, SHA-256
  `90f06972957a31343804cd090a47a596d8ed330da739743e5f9af45798b6b49a`.
- **Every test project, the switch on** (step `tests-on`, begun 2026-11-20 and taken again from its start and completed
  on 2026-11-22): the same, on the Release build with `-p:BroilerParseThroughBridge=true` (step `build-on`), which
  defines `BROILER_PARSE_THROUGH_BRIDGE` and so turns the internal `JSCompilationOptions.BridgeByDefault` on for every
  compile: the switch-on pass over the test projects that decision D43's note in
  [`docs/roadmap/SharedLibraries.md`](../../roadmap/SharedLibraries.md) records "in addition" to the internal
  `JSCompilationOptions.ParseThroughBridge`, which tests and the shell's hidden flag set;
  [`tests-on/`](side-by-side/w1/tests-and-test262/tests-on/summary.txt), `exits.tsv` SHA-256
  `3dd7731b63c7707f53f86906d13bcfc60792d94c6781593b27b04cb86e31f207`.
- **test262, the shell's hidden switch off** (steps `t262-off 0` to `7`, `t262merge-off`, `t262rerun-off`, 2026-11-19):
  `run_test262.py` in shards over the whole selection part A takes, with `--include-negative`, the original and
  Terser variants and one worker per processor (the shards, the runner's options and its timeout are in
  [`retained.txt`](side-by-side/w1/tests-and-test262/retained.txt)), through the shell of the switch-off build, each
  shard after the runner's self-check; merged by `merge_test262_shards.py`; each timed-out path run again alone with one
  worker. The merged report, `t262-off/test262-results.json`, over 2 MB, is retained by digest,
  `f698d6d58ff6b8cc4d88a61e7b3c629f0d82cfe537a71d1d720dbbcd6c6e6231`; its re-run,
  [`rerun-timeouts.json`](side-by-side/w1/tests-and-test262/t262-off/rerun-timeouts.json), is
  `8f7b57fd31779b123d47f5f1d972da578fbe763b600f8fa2678afdb701af1682`.
- **test262, the hidden switch on** (steps `t262-on 0` to `7`, `t262merge-on`, `t262rerun-on`, 2026-11-19 and
  2026-11-20, shard 1 taken again from its start): the same with `--shell-arg=--parse-through-bridge`, through the same
  shell (each shard's shell binaries are the switch-off build's). Merged report `t262-on/test262-results.json`, by
  digest, `305ccd70310137342eb2523ed80d26cb573ac63a6b98f917634c838b0dcca3e7`; re-run
  [`rerun-timeouts.json`](side-by-side/w1/tests-and-test262/t262-on/rerun-timeouts.json),
  `25669463928d5e11f0413cf4bcdd2839cc5265effda699ea25679ce6cc05cb66`.

**What each shows**, read and not judged, in words (the counts are in the folder's
[`counts.txt`](side-by-side/w1/tests-and-test262/counts.txt), written with the per-test and per-case tables by
[`read_local.py`](side-by-side/w1/tests-and-test262/scripts/read_local.py), which classifies nothing):
- **The test projects, off and on.** The same tests run in both. With the switch off, a timing test of the compiler's,
  `DeclarationDenseSourceTests.ManyTopLevelDeclarations_CompileInTimeLinearInTheirCount`, fails, and passes with it on.
  With the switch on, the architecture test that the switch is off in the build,
  `BridgeSwitchTests.The_compilers_test_only_switch_is_off_in_this_build`, fails, by its own message, and so do the
  compiler's deep-nesting tests `DeeplyNestedSourceTests.DeepBinaryExpressionChain_Compiles`,
  `DeepConditionalExpressionNesting_Compiles` and `DeepConditionalExpressionNesting_PastTheCompilationStack_Compiles`,
  refused with the library's nesting messages, and `DeepBinaryExpressionChain_OnAOneMebibyteThread_Compiles`, failing
  its assertion ([`tests-differ.tsv`](side-by-side/w1/tests-and-test262/tests-differ.tsv)).
- **test262, off and on.** The same paths and variants run in both. Most cases whose status moves go from failing off to
  passing on, and a corrections table names every such path; both variants of `test/staging/sm/regress/regress-610026.js`
  pass off and fail on, and the Terser variant of
  `test/language/expressions/dynamic-import/syntax/valid/new-covered-expression-is-valid.js` passes off and is skipped
  on, and no corrections table names either path
  ([`t262-differ.tsv.gz`](side-by-side/w1/tests-and-test262/t262-differ.tsv.gz)).
  `test/staging/sm/regress/regress-1507322-deep-weakmap.js` times out in both, and again in its re-run.
- Which of these moves part C would admit is read only at the base commit, from its own runs. These runs were read on
  2026-11-22, after the base commit's part C runs had begun (the base driver's first step, at 2026-11-22T14:51:15Z).
  Reading B9 above orders that "Any row of K10 they need, found from the retained run at W1, is added to
  [`corrections.md`](corrections.md) ... before the first part C base run". That order can no longer be kept for these
  moves: a row of K10 for any of these paths, were one needed, would be filed after the first part C base run, against
  it. B9 is left as written, for the owner.

### The restarts that interrupted the runs

The runs at W1 began under one driver at 2026-11-19T17:51:58Z; since 2026-11-19T23:36:24Z one resumable chain has run
W1's steps and then the base commit's. Every interrupted step was taken again from its start, after the default branch's
head was read as `69a8a13b`, and no step ran between an interruption and its resumption:
- **During W1's switch-on test262 shard 1**, begun 2026-11-19T23:10:21Z: the restart of 2026-11-19 (above); the chain
  started at 23:36:24Z and took the shard again from its start at 23:36:37Z (`w1/driver.log`, `w1/logs/commands.log`).
- **During W1's switch-on test projects**, begun 2026-11-20T04:02:25Z: the chain's log reads "2026-11-22T14:00:07Z chain
  restarted after the container restart of about 2026-11-20T04:01Z (tests-on was running; it is taken again from its
  start)"; the step was taken again from its start at 2026-11-22T14:00:20Z and completed. The hold's note above reads
  the restart's time as one "the logs do not show", as the step began after the time the chain's log gives.
- **During the base commit's switch-off test262 shard 6**, after W1's runs had finished: the chain's log reads
  "2026-11-22T21:21:40Z chain restarted after a container restart between 2026-11-22T20:09Z and 21:21Z (t262-off shard 6
  was running; it is taken again from its start)" and "2026-11-22T21:52:31Z chain restarted after a container restart
  between 2026-11-22T21:21Z and 21:52Z (t262-off shard 6 was running; it is taken again from its start)". The base
  driver read the head `69a8a13b` before each start of that shard. Those runs are still being taken and are not read
  here.

The chain's log as it stood on 2026-11-22T21:59Z is retained as
[`chain.log`](side-by-side/w1/tests-and-test262/chain.log), and W1's driver log as it stood then as
[`driver.log`](side-by-side/w1/tests-and-test262/driver.log): the chain runs W1's driver again first at each of its
starts, so its lines after W1's last step's exit at 2026-11-22T14:51:14Z are that driver's "all given steps done" and
its skips of W1's finished steps after the restarts, and any later lines are not here.

## W2's base, recorded on 2026-11-23

**Written:** 2026-11-23, during W2's hold, on branch `sjl-14-w2-base`, with the default branch's head at
`69a8a13bc446a1714d3c8be9be3cfd742aebee6e` (read by `git ls-remote` before this record was committed).
[`decision-rule.md`](decision-rule.md) is not edited, nothing here revises a line of it, no row is added to
[`corrections.md`](corrections.md) or its tables, and nothing is judged, met or accepted here but part C's reading,
in the rule's words.

### The base commit's local runs, retained

**Where, and how.** All at the base commit `69a8a13bc446a1714d3c8be9be3cfd742aebee6e`, from a clean `git archive` of it
on disk, on the programme's machine (Linux; its processors in [`archive.txt`](base/local/archive.txt)), from
2026-11-22T14:51:15Z to 2026-11-23T11:54:38Z, each step by `step.sh <commit> base <step>` or `bstep.sh <commit> base
<step>` through the machine-wide lock and idle gate, with the default branch's head read as `69a8a13b` before every
step ([`driver.log`](base/local/driver.log)), every test, test262 and tool run under `env -i` with `TZ=UTC` and no
`LANG`, `LC_*` or `BROILER_*` variable. Retained in [`base/local/`](base/local/README.md), every file of the run folder
at the same path, but the files over 2 MB, retained by digest only ([`retained.txt`](base/local/retained.txt),
[`not-retained.sha256.txt`](base/local/not-retained.sha256.txt)), with the chain's log, the memory log, the drivers and
scripts as they were on disk, a `.gitattributes`, and [`hashes.txt`](base/local/hashes.txt), SHA-256
`f49f3ac48270313364641378dec6f8ce1f1c2bff852bef41f9b9b01edc34f21c`. Each step's command line is in
[`logs/commands.log`](base/local/logs/commands.log) and in its own `logs-<step>/commands.log`, its exit in
`logs/runs.tsv`, and its machine records before and after in `logs/<label>.machine.txt` and `.machine-sh.txt`.
- **Part C, the side-by-side tool** (step `tools`, 2026-11-23): the tool built in Release outside the tree and run
  over every input and variant of `conformance/parse/corpus.manifest` at P, B, K and K-on with `--dump`, exit 0; the
  report, over 2 MB, by digest, `443fbdbf08d0e4f778b91a174f0b8ba1c384fd3df0bf046a02364aabc2808e72`; then at P and B
  with `--text`, whose rows are the first run's byte for byte, and `spancheck.py` over the dumps' text, exit 0
  ([`spancheck-counts.txt`](base/local/part-c-tool/spancheck-counts.txt);
  [`spans-not-s.tsv.gz`](base/local/part-c-tool/spans-not-s.tsv.gz),
  `f01006426d3636fad7b8f4a0033381148c2fa8c946b0cb4f8be2d4aaaa91fd0f`; the text and the whole listing by digest in
  [`part-c-tool/not-retained.sha256.txt`](base/local/part-c-tool/not-retained.sha256.txt)). A reading aid in the same
  step, meant to give the library's code for each variant B refuses where P accepts, exited 134 on a type it looked up
  in the wrong assembly and wrote nothing; it is no run the rule names.
- **Part C, every test project, the switch off and on** (steps `tests-off`, 2026-11-22, and `tests-on`, 2026-11-23):
  `dotnet test <project> -c Release --no-build --logger trx` for each test project of `Broiler.JS.slnx` under
  `Broiler.JS/`, on the Release build with the switch at its default (`build-off`) and with
  `-p:BroilerParseThroughBridge=true` (`build-on`), both builds with no warning; step exits 0;
  [`tests-off/exits.tsv`](base/local/tests-off/exits.tsv),
  `d64e064512eaf42531fac669082bbc4653e774fa1e639e552bab0527eb8cec9b`, and
  [`tests-on/exits.tsv`](base/local/tests-on/exits.tsv),
  `3dd7731b63c7707f53f86906d13bcfc60792d94c6781593b27b04cb86e31f207`.
- **Part C, test262, the shell's hidden switch off and on** (steps `t262-off 0` to `7`, `t262merge-off` and
  `t262rerun-off`, 2026-11-22; `t262-on 0` to `7`, `t262merge-on` and `t262rerun-on`, 2026-11-22 and 2026-11-23):
  `run_test262.py` in shards over the whole selection part A takes, with `--include-negative`, the original and
  Terser variants and one worker per processor, through the switch-off build's shell, the second with
  `--shell-arg=--parse-through-bridge`, each shard after the runner's self-check and with the same shell binaries;
  merged by `merge_test262_shards.py`; each timed-out path run again alone with one worker; step exits 0. The merged
  reports, over 2 MB, by digest: `t262-off/test262-results.json`,
  `93204e75d1973a873dff3bb08b2aa9b8a093094a98c1030f1837c62d8b60c8b4`, and `t262-on/test262-results.json`,
  `3bc1b4f04600a1f84be60b190279347e7b537432a8e6dbbada2e286bc7ebd3f2`; their re-runs
  [`t262-off/rerun-timeouts.json`](base/local/t262-off/rerun-timeouts.json),
  `9fd0a9732f75accb3213e4a5c303119644c6003fa78330d93149b1f13f2a78f8`, and
  [`t262-on/rerun-timeouts.json`](base/local/t262-on/rerun-timeouts.json),
  `4c423acdb0fdfbc45d126a3c23137b7f36905ce5fd4b734c30885f320f2683a2`.
- **Part D** (step `tools`): bundle `sjl-12-001`'s tool, byte-unchanged, built in Release outside the tree and run in
  its default mode (entries P and K) and with `--dump --order manifest`, exit 0 each:
  [`part-d/parse-corpus-c.tsv.gz`](base/local/part-d/parse-corpus-c.tsv.gz),
  `defb09e1824091f81ebbd6142a5ff365792864421bf4ccac954ffc25354ab3cb`, and, over 2 MB, by digest,
  `part-d/parse-corpus-d1.tsv.gz`, `d0a7be4b79f65cdd70942362dfd9db8cbd153b08062f630b63d4d1c17188a4f4`, as part B's runs
  were taken.
- **Part E** (step `phase5`, 2026-11-23): `scripts/performance/collect_phase5.py --configuration Release --rid
  linux-x64`, exit 1; [`part-e/phase5-report.json`](base/local/part-e/phase5-report.json),
  `1bbaec44dadcc3befb83499c7ef7a3110622fd7076f74d921249dfb6ca4b745c`; the regenerated program
  [`part-e/FibonacciProgram.generated.g.cs`](base/local/part-e/FibonacciProgram.generated.g.cs),
  `1e7e5565f4bbee3624149f6883e3c7ecadb2c52817755cea52b7a8896441e3f0`, beside the checked-in copy's digest
  ([`checked-in.sha256.txt`](base/local/part-e/checked-in.sha256.txt)); kept as taken and not run again.
- **Part G, the base candidate** (step `pack`, 2026-11-23): `eng/pack.ps1 -Configuration Release -ProjectSet Engine
  -Version 0.1.0-preview.1401` and `-ProjectSet Shared` into a local feed, and `eng/verify-feed.ps1` on each, exit 0
  each; the packages' digests in [`part-g/candidate-feed.sha256.txt`](base/local/part-g/candidate-feed.sha256.txt),
  `e2dd33e1f2f975d9b560410993ae7ecbdbe2d9b3e494a900d3c30cbe5187d63e`.
- **Part G, Broiler.JSeal** (step `jseal`, 2026-11-23) at `a1216a80034e185d69d0d76d9ef855105698c00b`, named on
  2026-11-19: `eng/test-package-consumer.ps1 -CandidateFeed <dir> -CandidateVersion Broiler.JavaScript=0.1.0-preview.1401`,
  its pins unchanged. Its first attempts exited 1 and are kept ([`part-g/jseal-attempt-1/`](base/local/part-g/jseal-attempt-1/report.json),
  "Restore Broiler.JSeal before a candidate run."; [`part-g/jseal-attempt-2/`](base/local/part-g/jseal-attempt-2/report.json),
  NU1102 on Broiler.Regex 0.1.0-preview.6); each stopped on an omission of the session's harness, not of the product:
  Broiler.JSeal's `eng/package-consumer/README.md` requires the shipping projects restored in Release-VM before a
  candidate run, and the candidate directory to "contain every package it introduces, at the versions they declare".
  The attempt taken with both, restored first and with a candidate directory of the feed's packages and the published
  Broiler.Regex, Broiler.Unicode.Algorithms and Broiler.Unicode.Segmentation 0.1.0-preview.6 archives, each checked
  against its recorded SHA-512 ([`jseal-candidate-external.txt`](base/local/part-g/jseal-candidate-external.txt)),
  exited 0: [`part-g/jseal/report.json`](base/local/part-g/jseal/report.json),
  `24f87e1dbe1527b8271156df5f8938d48734db7556c94dd93f6a0f3bf19d9b6f`. The step script's earlier texts are kept with
  it ([`scripts/`](base/local/scripts/bstep.sh)).
- **Part G, Broiler.HtmlBridge** (step `html`, 2026-11-23) at `334974f90c655181bba2ea6587ebb65336154c5a`, named on
  2026-11-19, in a scratch copy whose package source and Broiler.JavaScript versions are overridden for the candidate
  ([`scratch-overrides.diff`](base/local/part-g/htmlbridge/scratch-overrides.diff)), never committed: `dotnet
  restore`, `build` and `test` of its solution, exit 0 each; the TRX, over 2 MB, by digest,
  `0ffa01cb166919951163075ec18b9c059907bf569c88dba902b89524c504ba31`, and the Broiler.JavaScript packages it resolved
  ([`resolved-broiler-javascript.txt`](base/local/part-g/htmlbridge/resolved-broiler-javascript.txt)).
- **Part H** (step `api`, 2026-11-23): the API lister of `docs/evidence/sjl-0-001/api/` over every assembly the packages
  of `eng/pack.ps1`'s two sets carry, exit 0; one listing per assembly in [`part-h/api/`](base/local/part-h/projects.txt).

**What each shows**, in words (the counts are in the files):
- **Part E's collector** reports "offline compiler output differs from the checked-in program". The regenerated program
  differs from the checked-in `FibonacciProgram.g.cs` only in the form of its constants: by the repository's full
  history, the checked-in copy was last written at `3a8f3022` (2026-07-17), before `ef676932` (SJL-3.7, 2026-10-13),
  since which the portable compiler writes each number constant as `System.BitConverter.UInt64BitsToDouble(0x...UL)`.
  The sample published with Native AOT and ran, with no dynamic code (the report's `nativeAot` fields). Part E is judged
  at the after commit only; at the base this is its record. Regenerating the sample is a separate W2 stage's, not this
  record's. *(Changed the same day, before this record merged: the sentence after the colon read "the checked-in copy
  was last written at `68f9a87a` (Broiler-Platform/Broiler.JS#1061), before `588ca262` (SJL-12.5), whose emitter writes
  each constant as `System.BitConverter.UInt64BitsToDouble(0x...UL)`", names read from this machine's shallow clone, at
  whose graft boundaries both commits stand; the chain's log records the correction at 2026-11-23T12:28:11Z, and
  `scripts/chain2.sh`, retained as it ran, keeps the earlier names. The part E record itself is unchanged.)*
- **Part G**: both suites ran against the base candidate; Broiler.JSeal's consumer completed, and every test of
  Broiler.HtmlBridge that ran passed. **The after run must use the same harness**: Broiler.JSeal restored in Release-VM
  first, and a candidate directory holding every package the after commit's packages introduce, at the versions they
  declare.
- **Parts D and H**: their reports, which compare only with the after commit's.
- **Part C**: classified below.

**The restarts.** Container restarts interrupted the chain's base steps; each interrupted step was taken again from its
start, after the default branch's head was read as `69a8a13b`, and no step ran between an interruption and its
resumption. As [`chain.log`](base/local/chain.log) states them: during the switch-off test262 shard 6, "a container
restart between 2026-11-22T20:09Z and 21:21Z" and "between 2026-11-22T21:21Z and 21:52Z" (recorded already in "The
restarts that interrupted the runs" above); during the switch-on test262 shard 5, "a container restart between
2026-11-23T02:49Z and 05:06Z", "between 2026-11-23T05:12Z and 06:30Z" and "between 2026-11-23T06:35Z and 07:04Z";
and during `pack`, "a container restart between 2026-11-23T10:46Z and 11:10Z", whose first start the gate log shows
waiting at the idle gate on build-server processes left by part E's publish, none of its commands begun. From
2026-11-23T07:07Z `scripts/memwatch.sh` logged memory beside the chain ([`memwatch.log`](base/local/memwatch.log)), and
the chain's log reads its lines up to the last one before the restart of about 10:46Z as showing that restart was not
memory exhaustion; recorded as read, not judged. The chain stopped by design at part E's exit 1;
`scripts/chain2.sh` took `pack`, `api`, `jseal` and `html`, stopped at `jseal`'s first exit 1, and `scripts/chain3.sh`
took `jseal` and `html` and wrote "chain done". *(The section "The restarts that interrupted the runs" above reads
"Those runs are still being taken and are not read here"; they are finished and read here. It is left as written.)*

**What the chain did not take, or did not finish.** Every run the rule asks at the base commit was taken. Part E's
step has no done marker, as it exited 1 and is kept as taken; the reading aid of the `tools` step wrote no output;
`FAILED` in the run folder reads `jseal`'s second attempt and was not removed when the third passed. Part D's control,
"a copy of the base report with one outcome flipped and one message changed, which the comparison must report as two
differences", belongs to the comparison with the after commit and is not taken. Parts F, I, J and K have no base run:
each is taken at the after commit (part I on the owner's lane, with the base commit's build as its arm B).

### Part C at the base commit, classified

[`base/part-c/`](base/part-c/README.md) holds every difference of the runs above that part C's comparison names, each
with its class or found unclassified, the generated message tables of class M, and the scripts that wrote them, run
twice with the same bytes, from the retained runs alone; [`hashes.txt`](base/part-c/hashes.txt), SHA-256
`ce3b2ba550b0a4080f567ff75ce753e98f62354ac175badfded20021959bfa29`. The corrections it reads are K1 to K10 as the base commit holds them,
unchanged since `232230dc` (`232230dcd2160803a3d97123abfb7e856441b442`, 2026-11-03), before the first part C base step;
this branch adds no row of K10, as reading B9's "before the first part C base run" has passed. How each class is read
is in that folder's README; the library's code for a refusal is not in the run, so class E is read from the bridge's
text, a reader's reading: every E is provisional, listed with the rows B's text gives rather than its one row, and the
unclassified list is a lower bound until E is read by code. The run that would settle it is the `tools` step's reading
aid with the bridge's type looked up in `Broiler.JavaScript.Parser`, on a build of the base commit behind the gate,
when the orchestrator decides. *(Changed the same day, after review, before this record merged: the digest read `ce949d7bafbbe170d60a7ff4ff4d6e23642816927305d42d5c8033f4c3930da1`,
as `classify_c.py` was run again with two span readings no longer read S and class E marked provisional; the
paragraph ended at "a reader's reading.", and named `232230dc` by eight digits only.)*
- **W1's open items at the base.** `J000557` is C1 by K8 (the correspondence's P13, which the classification names),
  and `J001072` and `J001780` by K7 (D37); the outcome moves no corrections table lists are each C1, C3 or E, but for
  those below (E provisional, above); every `both-agree` span line is read S from its two texts but for two readings,
  where P's span agrees with the node's text, which are unclassified below; W1's unlisted test262 moves appear at the
  base as at W1, and are unclassified below. *(Changed the same day, after review, before this record merged: the bullet read "or E, but for those below; every
  `both-agree` span line is read S from its two texts;".)*
- **Unclassified**, each with both sides and a reading of its cause in that folder's README:
  - P accepting, B refusing and K refusing on both sides, where the code B's text gives is not one the classification
    lists first, so the header's class E does not admit it: a getter with a parameter (`T035220`, `T039055`,
    `T047023`, `J000058`, `J001174`), an arrow function as a class heritage naming a private name (`T034966`,
    `T034969`, `T046767`, `T046770`), and a parenthesized literal as an assignment target (`J000040`, `J001147`), each
    in both variants; read as no bridge defect and not the harness, but the library's code against the
    classification's expectation, which is the library's or the classification's to answer;
  - the architecture test `BridgeSwitchTests.The_compilers_test_only_switch_is_off_in_this_build`, failing in the
    switch-on build by its own design: the harness's;
  - `test/language/statements/for-of/head-await-using-bound-names-in-stmt.js` (original), failing off and passing on,
    which no row of K1 to K10 lists: by part A's C1 line, "a C1 row that none explains is recorded as a finding against
    the correspondence";
  - `test/staging/sm/regress/regress-610026.js` (both variants), passing off and failing on with no output and no
    reason, a non-zero exit and not a time-out: read as likely the bridge's or the library's parse of its doubled-block
    source, slower with the switch on and ending near the runner's bound, perhaps at its CPU-time limit; not shown by
    the records, and decided by the one case run again with the switch-on shell recording its exit status, CPU time and
    peak memory; *(changed the same day, after review, before this record merged: after "no output" the item read ":
    the retained records do not show whether the bridge's or the library's parse of its source, or the runner near its
    bound, is the cause;")*
  - `test/language/expressions/dynamic-import/syntax/valid/new-covered-expression-is-valid.js` (Terser), passing off and
    skipped on, as the runner finds the minified body invalid: the harness's;
  - every tree difference with a span line where P's span ends at the end-of-input token and B's at the same offset as
    a number, both giving exactly the node's text: not S, as P's span agrees with the text; read as the old parser's
    dump writing the end as the token's name, not a bridge or library defect and not the harness;
  - the member, optional-chain and import-call spans of `T036087`, `T039546` and `T051374` (both variants), where P's
    span ends after the node's closing bracket and B's leaves it out, as the AST type takes the end from the child:
    the opposite of S; read as the span B's tree carries for the node, not the library's parse and not the harness.
    *(The last two items added the same day, after review, before this record merged; they were read S. The item
    before them ended "the harness's.")*
- **Part C: read NOT MET on the retained runs**, for the unclassified differences listed above. Not judged beyond that
  (EX-30). *(Changed the same day, after review, before this record merged: after "above." the bullet read "Every run
  at the base commit is complete, and every other difference is in C1, C3, E, M or S and is listed.", which went
  beyond the reading and which the two items above and class E's reading contradict.)* Part C's
  "How it is read" sends "An unclassified difference" to "a bridge or library defect, fixed as a dark merge and run
  again before W2 merges"; what follows from each item is not decided here.

### Parts A, D, E, G and H at the base

Each compares the base with the after commit, and the after commit does not exist: their base runs are retained (part
A's in [`base/part-a/`](base/part-a/imported.txt), the others in [`base/local/`](base/local/README.md)) and read, not
judged.

## What it does not claim

- **No figure.** No count, timing, ratio or score is written in this README; the bundle's runs hold their own. No
  test262 suite total is written here at any step (rule H5). *(Noted 2026-11-19, before W2's base: reading B10 in
  "Before W2's base, read on 2026-11-19" names, in words, the numbers of repetitions part I's protocol takes; they are
  the protocol's parameters, fixed before any part I run, and not a figure of any run. The sentence above is read of
  measured figures, and is left as written.)*
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
  names with its effective configuration". [`decision-rule.md`](decision-rule.md) is not edited.)* *(Noted
  2026-11-19, after SJL-11.5's merge: the note above reads "W2, the after commit, is held for SJL-11.5 (the commits
  above), so part I is not taken before W2 merges." SJL-11.5 has merged, as `b2b226cd`, and W2 is no longer held for
  it (the commits above). Part I is still not taken before W2 merges, since its arms are the base commit's build and
  the after commit's and neither exists; its protocol points are filed before any part I run by reading B10 in "Before
  W2's base, read on 2026-11-19" above. Clause 6 is unmet. [`decision-rule.md`](decision-rule.md) is not edited.)*
- **Exclusions:** none.

| Exclusion | What | Why |
|---|---|---|
| - | - | - |
