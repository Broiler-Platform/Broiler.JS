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
| [`decision-rule-revised-2026-11-23.md`](decision-rule-revised-2026-11-23.md) | The rule's dated revision, filed by the owner's answers 10 and 11 of 2026-11-23 after part C's base runs were seen, and so weaker evidence: part C admits the differences of groups 1 to 5, 7, 8 and 9 those answers name, and part D those of groups 1 to 3, 8 and 9, each class naming its entries, and nothing wider; it quotes the rule whole *(added 2026-11-23, records part 5)* *(Changed the same day, after review, before this record merged: the cell read "parts C and D admit the differences those answers name", wider than the revision, which admits groups 4, 5 and 7 in part C alone.)* |
| [`corrections.md`](corrections.md) and [`corrections/`](corrections/k-rows.tsv) | The corrections K1 to K10, each with its rows and test262 paths; the classes E and T with their rows and sites; finding S14-F1; the probes' rows *(Noted 2026-11-23, after review: finding S14-F2, against the correspondence, is recorded in "W2's base, records part 5" below, not in `corrections.md`, which the rule's revision of 2026-11-23 leaves unedited.)* |
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
| [`base/part-a/`](base/part-a/imported.txt) | Part A's base run, `.github/workflows/test262-evidence.yml` run 44639106095 at `69a8a13b`, its artifact's entries imported unchanged (the one over 2 MB by its digest), with its run's artifacts and digests, the jobs' runner images and `hashes.txt`; read, not judged *(added 2026-11-22, during W2's base runs)* |
| [`side-by-side/w1/tests-and-test262/`](side-by-side/w1/tests-and-test262/README.md) | Part C's other runs at W1, the test projects and test262 with the switch off and on, taken during W2's base phase, with their logs, machine records, scripts and `hashes.txt` (the files over 2 MB by digest), and a reading of them that classifies nothing; read, not judged *(added 2026-11-22, during W2's base runs)* |
| [`base/local/`](base/local/README.md) | The base commit's local runs at `69a8a13b`: part C's side-by-side tool, test projects and test262 with the switch off and on; part D's tool; part E's collector; part G's base candidate feed with Broiler.JSeal's and Broiler.HtmlBridge's suites against it; part H's API lister; with the chain's logs, machine records, scripts and `hashes.txt` (the files over 2 MB by digest) *(added 2026-11-23, during W2's hold)* *(Noted 2026-11-23, records part 4: the three of those files the after comparisons read, part C's side-by-side report, part D's dump report and Broiler.HtmlBridge's TRX, are now kept whole there; "the files over 2 MB by digest" is left as written)* |
| [`base/part-c/`](base/part-c/README.md) | Part C at the base commit: every difference of those runs with its class, or unclassified with its reading, the generated message tables of class M, and the scripts that wrote them; part C read NOT MET on the retained runs *(added 2026-11-23, during W2's hold)* *(Noted 2026-11-23, records part 5: the cell before reads "part C read NOT MET on the retained runs"; part C is read again there under the rule's revision of 2026-11-23, read NOT MET on the retained runs for `test/staging/sm/regress/regress-610026.js` alone, which the revision does not admit; "W2's base, records part 5" below)* |
| [`after/part-a/`](after/part-a/imported.txt) | Part A's after run, `.github/workflows/test262-evidence.yml` run 45264304825 at the after commit `83a9bd82`, retained as `base/part-a/` retains the base run, with the rule's comparison of it with the base in `comparison/` *(added 2026-11-24)* |
| [`after/local/`](after/local/README.md) | The after commit's local runs of parts D, E, F, G, H, J and K, retained by `base/local/`'s convention, with the driver's scripts, logs and machine records *(added 2026-11-24)* |
| [`after/owed-runs/`](after/owed-runs/README.md) | The runs the after readings owed, taken 2026-11-24 after the after runs: the base commit's dump text (part D), Broiler.JSeal's base side per test (part G), single-case compiles (part J) *(added 2026-11-24)* |
| [`after/part-d/`](after/part-d/README.md), [`after/part-f/`](after/part-f/reading.txt), [`after/part-g/`](after/part-g/per-test.txt), [`after/part-h/`](after/part-h/reading.txt), [`after/part-j/`](after/part-j/findings.txt), [`after/part-k/`](after/part-k/hits.tsv) | The after comparisons and reading aids of parts D, F, G, H, J and K, each with its script; the parts are read in "W2's after commit, recorded on 2026-11-24" below *(added 2026-11-24)* |

## W2's code, written on 2026-11-19 (on branch `sjl-14-w2`, not merged)

**Written:** 2026-11-19, with W2's code, on a branch from the default branch's `fefb4f9c`, before any base run and before
the base commit exists. Nothing here is a run or a reading of a part; the rule is not edited. The commits are the
branch's, each named "SJL-14: W2 stage N, ..." for the plan's stage order (`docs/roadmap/SharedLibraries.md`, "The work,
in order"), and W2 is the one merge that lands them. *(Added 2026-11-19, in review of the branch:)* the stage-4
continuation (`8632939e`, "stage 4, continued") follows stage 5 (`c4b0bf14`) in the branch's history, and the review's
fixes follow stage 6 and the bundle's commits, each named "SJL-14: W2, review fix..."; the checks were taken on the
branch's head from a clean archive, not on each stage's commit, and stage 1's commit alone (`34c4e7eb`) leaves the
deep-nesting fixtures failing until its continuation (`4493624b`).

**The session's decisions under the owner's direction of 2026-10-18**, "Please do all recommended decisions and skip
waiting for review.", each taken as recommended, with its alternative not taken; none revises a rule line:
- **B5, the shared stream's version.** `Broiler.ECMAScript/Directory.Build.props`' `VersionSuffix` moves from
  `preview.8` to `preview.12` in W2's package stage (stage 4), so that the Parser package's dependency on
  `Broiler.ECMAScript.Frontend` names the version published from tag `ecmascript-v0.1.0-preview.12`, whose
  `Broiler.ECMAScript/` equals the default branch's at `fefb4f9c`, and this branch's but for that file's `VersionSuffix`
  and its comment (the section "The shared stream's published version" of the records of 2026-11-19 reads it). Moving it
  publishes nothing. *(Corrected 2026-11-19, in review: this read "whose `Broiler.ECMAScript/` equals this branch's",
  which the suffix's own change makes untrue; so did `docs/public-api.md`'s "Package dependencies" and the file's
  comment, corrected with it.)* *Not taken:* having `eng/pack.ps1` resolve the shared dependency from
  the `ecmascript-v*` tags.
- **B6, part F's control.** The pristine consumer packs both sets, the engine closure, which packs the shared libraries
  it references, and the shared set's other packable projects, and maps this repository's ids to the local feed alone by
  package source mapping (`scripts/packaging/test_pristine_consumer.py`, stage 6), so that removing
  `Broiler.ECMAScript.Frontend` from the feed fails the restore with the missing-package error rather than restoring
  the published version from nuget.org; `--control-remove` takes the control. The decision reads "map `Broiler.*`";
  the mapping names the prefixes `Broiler.JavaScript.` and `Broiler.ECMAScript.`, since `Broiler.Regex` and
  `Broiler.Unicode.*`, which other repositories publish, are not packed here and come from nuget.org. `scripts/` is no
  product path. *Not taken:* a dated rule revision of the control, which would be weaker evidence by the rule's
  "Revisions" and goes to the owner by precedent. *(Added 2026-11-19, in review:)* part F reads "eng/pack.ps1 and
  eng/verify-feed.ps1 at the after commit into a local feed; scripts/packaging/test_pristine_consumer.py against that
  feed". The script packs its own feed by default; `--feed`, repeatable, copies the packages of the directories it names
  (`eng/pack.ps1`'s Engine and Shared outputs) into the consumer's private feed and packs nothing, and `--control-remove`
  then removes the package from that copy alone. Part F's run passes `--feed` with both outputs, for the check and for
  its control, so that the consumer reads the feed `eng/pack.ps1` made. The control passes only on a missing-package
  error that names the package removed.
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
- **B8 (W2's own bridge changes), a reading added 2026-11-19, in review, before any base run.** The rule reads: "W1 is
  the only first-parent commit that carries any part of the bridge or the switch"; "W2 is the only first-parent commit
  that carries any part of SJL-14.3 to SJL-14.5 (the switch's removal, the adapter packages, the portable compiler's and
  the module analysis's move, the deletions)"; and "A part whose base was taken at any other commit, or whose commits
  fail those conditions, is void." W2 itself changes the bridge: `Bridge/BridgeRequest.cs` (the request's Unicode
  tables), `Bridge/BridgeTokens.cs` (the token chain the facades read) and `Bridge/SharedSyntaxBridge.cs` (the token
  chain and the shared tree the portable compiler's and the module analysis's readers take), each for an item of
  SJL-14.3 to SJL-14.5, and B8's fix would add to them. *Taken:* W1's condition is read as covering the bridge as
  SJL-14.2 landed it and every first-parent commit before the base commit: no first-parent commit but W1 carries any
  part of the bridge or the switch through the base commit. The bridge's changes W2 makes are SJL-14.3 to SJL-14.5's,
  which the rule's second sentence gives to W2 as its own, the switch's removal and the facades' tokens among them, so
  W2's carrying them is W2's condition met, not W1's failed. Main's reading B1 reads the same lines against SJL-11.5's
  merge `b2b226cd`; this one reads them against W2. *Not taken:* the rule's words alone: W2 is a first-parent commit that
  carries part of the bridge, so W1's condition fails at W2, and by the void sentence every part judged at the base
  commit or at W2 is void, as is part B, judged at W1. *Added to the owner's optional ruling* beside B1's (the section
  "Part B, judged on 2026-11-15" on the default branch, by its note of 2026-11-19), with the alternative.
- **B10 (code points), two code points.** *(Labelled 2026-11-19, in review: this read "B10, two code points"; it is
  not the records' reading "B10 (part I's protocol)" of the same day, "Part I's protocol, filed before any part I run",
  which the default branch carries and B7 above cites, and the two share the number because the map's B10 named both.)*
  `FastScanner.IsValidRegularExpressionLiteral`, Broiler.Regex's validate entry, stays where it
  is, and the bridge's regular-expression provider calls it (`dispositions.md` lists it on no deletion row). *Not
  taken:* moving it into `BridgeRegularExpressions`. And row R24 of K10 is corrected by an overload of the public
  `DirectEvalSupport.Execute` whose new parameter carries the class's heritage, with its row in
  [`docs/public-api.md`](../../public-api.md) ("Public API changes, 2026-11-19") (stage 3). *Not taken:* leaving R24
  uncorrected at SJL-14.

**Where W2's code departs from the records, and why**, each recorded here before any base run:
- **`FastParseException` stays in `Ast`**, not moved or forwarded: its public field `Token` is a `FastToken`, which stays
  in `Ast`. *(Changed 2026-11-19, in review: this read "`dispositions.md` records it by a dated note of the same day". The
  note is removed from `dispositions.md`, which was committed with the rule, before any change it judges, and which
  parts F, H and K are judged against, so that W2 does not revise it; the departure is recorded here alone, and
  `dispositions.tsv` agrees with `dispositions.md` again.)* `dispositions.md`'s bullet reads "**`FastParseException` is
  moved and forwarded, not marked obsolete.**" It cannot move: `Broiler.JavaScript.Expressions` cannot name `FastToken`,
  a type of `Ast`, which references it. W2 keeps `FastParseException` in `Ast`, in its namespace, not moved, not
  forwarded and not marked obsolete; it stays a non-tree type by that bullet's reason, which `Runtime` catches, the
  facade throws and SJL-12's tool catches, and its public surface does not change. The alternatives were to move
  `FastToken` with it, and with it `SpanLocation` and `FastKeywords`, which `dispositions.md` marks obsolete, or to change
  the field's type, a breaking change the facade must not make. Part H reads the type against `dispositions.md`'s bullet
  as written; whether `dispositions.md` counts as a rule file, and whether this departure is admitted, are the owner's,
  before any run of parts F, H or K.
- **The two parser tests that read the old parser's message text** (decision D46, "the texts hosts and tests read are
  kept (... and the texts this engine's tests assert)"): `ParserTests` asserted `Contains("Unexpected token Negate: ! at
  1, 1")` with `"the end of the script"`, and with `"SemiColon at 1, 2"`, the old parser's backtracking report naming
  the statement's first token and the furthest; W2 asserts the library's report of the token where the parse stopped,
  `Equal("Unexpected token EOF at 1, 2")` and `Equal("Unexpected token ; at 1, 2")`, and the tests' comments call it
  class M. *(Recorded 2026-11-19, in review: this list and the "Tests revised" bullet below had called the change class
  M, decision D46, where D46 keeps those texts.)* It departs from D46. *Alternative not taken:* keeping the asserted texts
  through the bridge's message table, which would make the table reproduce the old parser's backtracking for those two
  shapes. The owner may rule otherwise, with the other reserved points. The three unterminated-token lines are position
  moves, which D46 names as class M.
- **The portable compiler references `Broiler.Regex`** (privately), beyond decision D53's list ("`Portable.Compiler.csproj`
  references the frontend and `Portable`, and `Broiler.JavaScript.Expressions` only if it uses a non-tree type that
  moves there"): its regular-expression provider answers the library with Broiler.Regex's validate entry and its flags,
  which `Expressions` does not expose. *Recorded 2026-11-19, in review*; `docs/architecture/dependencies.md`'s note says
  the same. *Alternative not taken:* a flags-aware validator exposed from `Expressions`, a new member there. The closure
  is unchanged: Broiler.Regex was in it through `Ast` and `Parser` at the base.
- **`FastParser.Combine`'s conditional form** throws `NotSupportedException`; it parsed the false branch from the stream
  until W2. Decision D50 keeps "every member" of the facade. *Recorded 2026-11-19, in review.* *Alternative not taken:*
  parsing the false branch from the stream's position through the bridge, as `Export` and `NonDeclarativeStatement` do,
  which would parse a text that is not a program on its own. `docs/public-api.md` names it.
- **`FastScope` and `FastScopeItem`** record nothing and answer as a scope that declares no name, since no parser fills
  them (*changed 2026-11-19, in review*: they had kept a private record of the names a caller added, a working name record
  rather than a facade over the bridge, which part K's MET clause admits only as "an obsolete forwarder over the bridge").
  `dispositions.md` lists them `facade-obsolete`. *Alternative not taken:* keeping the name record and reading it as that
  disposition by a note in `dispositions.md`.
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
  the old parser's backtracking report (a departure from D46, above) and three unterminated-token lines (class M,
  decision D46); and the tests that read the moved types' assembly. They are not part C's classification, which is taken
  at the base commit. *(Changed 2026-11-19, in review: the bullet read "two that read the old parser's backtracking report
  and three unterminated-token lines (class M, decision D46)".)*

**The marks' diagnostic identifier.** *(Changed 2026-11-19, in review, before W2 merged and before any base run:)* the
one identifier decision D50 gives the marks (`dispositions.md`, "one diagnostic identifier, suppressed by one `NoWarn`
in the repository root's `Directory.Build.props`"), which names no value, is `BJS0002`. The branch had given it
`BJS0001`: `ObsoleteMarks.cs` read `internal const string DiagnosticId = "BJS0001";`, the root `Directory.Build.props`'
`NoWarn` ended `;SYSLIB0013;BJS0001` under a comment opening "BJS0001, added at window W2", and `docs/public-api.md`
("The marks") and `Broiler.JS/CHANGELOG.md` named `BJS0001`. That identifier is already
`KeyStringGetOrCreateLoopAnalyzer`'s (`Broiler.JavaScript.JSClassGenerator`), which BuiltIns, Engine, Globals,
Modules, Network and Runtime reference as an analyzer, so the root `NoWarn` silenced that analyzer in every project.
`BJS0002` is the next identifier of the repository's `BJS` scheme, which has no other; no part was run under the old
identifier, so no reading changes. `KeyStringGetOrCreateLoopAnalyzer` keeps `BJS0001`, unsuppressed.

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
  each is taken at the after commit alone. Nothing is in `after/`.)* *(Noted 2026-11-24: the note before this one reads "Nothing is in `after/`." Part A's after run
  and the after commit's local runs of parts D, E, F, G, H, J and K are retained in `after/`, with the comparisons; see
  "W2's after commit, recorded on 2026-11-24" below.)*
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
    start" in "W2's base, recorded on 2026-11-22" below. W2 does not exist yet.)* *(Noted 2026-11-24: the note before this one reads "W2 does not exist yet." W2 merged on
    2026-11-23 as `83a9bd82a5d258d3001468095ebf43f9efc3e9a7`, the merge of Broiler-Platform/Broiler.JS#1129, whose first
    parent is `69a8a13b`; it is the after commit, and the hold ended with it. See "W2's after commit, recorded on
    2026-11-24" below.)*

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
`f9819e16d86651ed3dead2c3ca1f9b829a8f523161b2554a243eac7f8d634628`. *(Changed the same day, after review, before this
record merged: the digest read `f49f3ac48270313364641378dec6f8ce1f1c2bff852bef41f9b9b01edc34f21c`, before
`retained.txt` and the folder's README added the records below on Broiler.JSeal's per-test results, the files kept
by digest only that the after comparisons read, and the build trees.)* *(Noted 2026-11-23, records part 4: the
sentence above reads "but the files over 2 MB, retained by digest only"; three of them, part C's side-by-side report,
part D's dump report and Broiler.HtmlBridge's TRX, are now kept whole in `base/local/` at their paths, and the
folder's `hashes.txt` digest is the one "W2's base, records part 4" below gives. The sentence and the digest above
are left as written.)* Each step's command line is in
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
  history, the checked-in copy was last written at `3a8f3022` (`3a8f3022b54c490973ecb134d7fdd4be0fde8cdf`,
  2026-07-17), before `ef676932` (`ef6769322b2189d413f1c3649df3e7aa6aaffcda`, SJL-3.7, 2026-10-13), since which the portable compiler writes each number constant as `System.BitConverter.UInt64BitsToDouble(0x...UL)`.
  The sample published with Native AOT and ran, with no dynamic code (the report's `nativeAot` fields). Part E is judged
  at the after commit only; at the base this is its record. Regenerating the sample is a separate W2 stage's, not this
  record's. *(Changed the same day, before this record merged: the sentence after the colon read "the checked-in copy
  was last written at `68f9a87a` (Broiler-Platform/Broiler.JS#1061), before `588ca262` (SJL-12.5), whose emitter writes
  each constant as `System.BitConverter.UInt64BitsToDouble(0x...UL)`", names read from this machine's shallow clone, at
  whose graft boundaries both commits stand; the chain's log records the correction at 2026-11-23T12:28:11Z, and
  `scripts/chain2.sh`, retained as it ran, keeps the earlier names. The part E record itself is unchanged.)* *(Changed
  again the same day, after review: the forty-digit ids are added beside the eight-digit names, which this machine's
  shallow clone cannot resolve; GitHub's commit pages give them.)*
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
*(Added the same day, after review, before this record merged:)* Not taken either: every test's result of
Broiler.JSeal's suites, which part G asks retained and compares per test. The consumer's test commands ran with no test
logger, so [`report.json`](base/local/part-g/jseal/report.json) keeps each suite's summary and its skipped tests' names,
not each passed test; whether the base run is taken again with per-test output or read by summary and skipped names is
the orchestrator's to decide, and either way the after run's harness should keep a TRX for the same suites. Three files
kept by digest only are read by the after comparisons, and their only copy is in the run folder in the session's
scratch space, which is not durable: part C's report, part D's dump report and Broiler.HtmlBridge's TRX
([`retained.txt`](base/local/retained.txt)); the run folder is to be kept until those comparisons are taken, unless the
orchestrator has them kept whole in the bundle, as `side-by-side/w1/` and `bridge/` kept such files. *(Noted
2026-11-23, records part 4: the sentence before reads "Three files kept by digest only are read by the after
comparisons, and their only copy is in the run folder in the session's scratch space, which is not durable" and "the
run folder is to be kept until those comparisons are taken, unless the orchestrator has them kept whole in the
bundle". They are now kept whole in [`base/local/`](base/local/README.md) at their paths, unchanged, by a decision
taken under the owner's direction ("W2's base, records part 4" below), so the run folder no longer holds their only
copy. The sentence is left as written.)* And, a departure
from this stage's brief: the `bin` and `obj` trees of the base build and of W1's build, outside the bundle, were deleted
by this stage after its first commits and before review, where the brief kept that folder read-only until the commit
was verified. Nothing retained lived there, but the shell binaries' digests can no longer be checked against the
binaries that ran, and any further run (the reading aid that would give the library's code, a single case) needs a
build of the base commit behind the gate.

### Part C at the base commit, classified

[`base/part-c/`](base/part-c/README.md) holds every difference of the runs above that part C's comparison names, each
with its class or found unclassified, the generated message tables of class M, and the scripts that wrote them, run
twice with the same bytes, from the retained runs alone; [`hashes.txt`](base/part-c/hashes.txt), SHA-256
`b9a8fd92c477cb64d7ee91695ba7dc7700b336501bd6acb07cec989dc4660c47`. The corrections it reads are K1 to K10 as the base commit holds them,
unchanged since `232230dc` (`232230dcd2160803a3d97123abfb7e856441b442`, 2026-11-03), before the first part C base step;
this branch adds no row of K10, as reading B9's "before the first part C base run" has passed. How each class is read
is in that folder's README; the library's code for a refusal is not in the run, so class E is read from the bridge's
text, a reader's reading: every E is provisional, listed with the rows B's text gives rather than its one row, and the
unclassified list is a lower bound until E is read by code. The run that would settle it is the `tools` step's reading
aid with the bridge's type looked up in `Broiler.JavaScript.Parser`, on a build of the base commit behind the gate,
when the orchestrator decides. *(Changed the same day, after review, before this record merged: the digest read `ce949d7bafbbe170d60a7ff4ff4d6e23642816927305d42d5c8033f4c3930da1`,
as `classify_c.py` was run again with two span readings no longer read S and class E marked provisional; the
paragraph ended at "a reader's reading.", and named `232230dc` by eight digits only. Changed again for the folder
README's forty-digit id: the digest read `ce3b2ba550b0a4080f567ff75ce753e98f62354ac175badfded20021959bfa29`.)*
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
  again before W2 merges"; what follows from each item is not decided here. *(Noted 2026-11-23, records part 5: the
  bullet reads "Part C: read NOT MET on the retained runs, for the unclassified differences listed above" and "what
  follows from each item is not decided here". The owner decided by answers 10 and 11 of 2026-11-23: a dated revision
  of the rule admits every item above but `regress-610026.js`, and part C, read again under it, is read NOT MET on the
  retained runs for that path alone ("W2's base, records part 5" below). The bullet is left as written.)*

### Parts A, D, E, G and H at the base

Each compares the base with the after commit, and the after commit does not exist: their base runs are retained (part
A's in [`base/part-a/`](base/part-a/imported.txt), the others in [`base/local/`](base/local/README.md)) and read, not
judged.

## W2's base, records part 4, recorded on 2026-11-23

**Written:** 2026-11-23, during W2's hold, on branch `sjl-14-w2-base`, with the default branch's head read as
`69a8a13bc446a1714d3c8be9be3cfd742aebee6e` by `git ls-remote` before every step and before this record was committed.
**What it records:** reading aids and single-case re-runs that settle part C's open readings at the base, **not new
runs of the rule**: none replaces a retained run, each is recorded as a reading aid, as W1's records name theirs, and
nothing here is a verdict. Part C's reading stands as taken: read NOT MET on the retained runs.
[`decision-rule.md`](decision-rule.md) is not edited, nothing here revises a line of it, and no row is added to
[`corrections.md`](corrections.md) or its tables. Every file is in [`base/part-c/aids/`](base/part-c/aids/logs/runs.tsv)
and [`base/part-c/scripts/`](base/part-c/scripts/read_base.sh), described in [`base/part-c/README.md`](base/part-c/README.md)'s
"Records part 4"; that folder's [`hashes.txt`](base/part-c/hashes.txt) now reads SHA-256
`992e87c1e547fe4b97e1e4a1b49cb9baa182296a120c186703ddf7a05c2c806c`, and [`base/local/`](base/local/README.md)'s
[`hashes.txt`](base/local/hashes.txt) `8952bf24ab404e788d6975cb19261166125eef0795ffcb528092e6730048145e`. *(The two
digests the section "W2's base, recorded on 2026-11-23" names for those files, base/local's
`f9819e16d86651ed3dead2c3ca1f9b829a8f523161b2554a243eac7f8d634628` and base/part-c's
`b9a8fd92c477cb64d7ee91695ba7dc7700b336501bd6acb07cec989dc4660c47`, are of the folders before this record; they are
left as written there.)* *(Changed the same day, after review, before this record merged: part C's folder's digest
read `491803453c610a1401f9e1ddd03176f056bcd692b2bd3f9bbe4cc67af815cce6`, before that folder's README took the
review's corrections; and the parenthesis named the two earlier digests "`f9819e16…` and `b9a8fd92…`" without their
folders.)*

**Each aid and re-run**, every step by `stepD.sh` through the machine-wide lock and idle gate, its command line, exit
and machine records in [`base/part-c/aids/logs/`](base/part-c/aids/logs/commands.log), every engine run under `env -i`
with `TZ=UTC` and no `LANG`, `LC_*` or `BROILER_*` variable *(changed the same day, after review, before this record
merged: of these, the table re-runs `d-read-*` were taken by `readD.sh`, not `stepD.sh`, under the same `run.sh`, as
[`base/part-c/aids/logs/commands.log`](base/part-c/aids/logs/commands.log) shows; the sentence read "every step by
`stepD.sh`" alone)*:
- **A build of the base commit** `69a8a13bc446a1714d3c8be9be3cfd742aebee6e` (steps `d-base-archive`, `d-base-build`,
  `d-base-terser`, 2026-11-23): a clean `git archive` on disk outside every working tree, `dotnet build Broiler.JS.slnx
  -c Release --disable-build-servers` as the base runs' `build-off` took it, exit 0 with no warning
  ([`build.log`](base/part-c/aids/base/build.log), [`archive.txt`](base/part-c/aids/base/archive.txt), the shell's
  binaries' digests in [`shell-binaries.sha256`](base/part-c/aids/base/shell-binaries.sha256)); its trees were removed
  when the runs ended. *(Added the same day, after review, before this record merged:)* Built the same way, but not
  the same bytes: every assembly of the rebuilt shell built from the repository's sources differs by digest from those
  of the shell the retained runs used ([`shell-binaries-off.sha256`](base/local/shell-binaries-off.sha256)); only the
  package assemblies match. The likely cause, not confirmed, is the build tree's path compiled into them
  (`/home/user/js-w2baseD-build/69a8a13b/tree` here, `/home/user/js-w2base-build/69a8a13b/tree` for the retained
  runs). The bridge-codes and parse-time aids ran on their own builds of the library from the same sources
  ([`codes-binaries.sha256`](base/part-c/aids/base/codes-binaries.sha256),
  [`ptime-binaries.sha256`](base/part-c/aids/base/ptime-binaries.sha256)), whose library assembly's digests differ
  again. The re-runs and the aids' outputs are read as from the same commit's sources, not from the same bytes.
- **The bridge-codes reading aid, corrected** (step `d-base-codes`): the `tools` step's aid with the bridge's types
  looked up in `Broiler.JavaScript.Parser`, where they are ([`base/part-c/scripts/bridge-codes/`](base/part-c/scripts/bridge-codes/Program.cs);
  the aid as it ran at the base step is kept in [`base/local/scripts/bridge-codes/`](base/local/scripts/bridge-codes/Program.cs)),
  over every input variant P or B refused in the base commit's side-by-side report, exit 0, every variant answered:
  [`bridge-codes.tsv`](base/part-c/aids/base/bridge-codes.tsv),
  `48c299af425bda5a104f74ab84ef1940778afe0a2425b5b7a07a16b2a15c3f43`.
- **Part C's tables, written again** with class E read by that code (runs `d-read-nocodes` and `d-read-codes-1` to
  `d-read-codes-4` of [`base/part-c/scripts/read_base.sh`](base/part-c/scripts/read_base.sh), exit 0 each): without
  the code the scripts write every earlier table byte for byte; with it, the same bytes in each run (runs
  `d-read-codes-1` and `-2` read the code file at `base/part-c/aids/bridge-codes/bridge-codes.tsv`, from where it was
  moved to `base/part-c/aids/base/` before `d-read-codes-3`; its digest there was not recorded, the four runs' outputs
  are the same bytes, and the file is `48c299af…` above; *added the same day, after review, before this record
  merged*). Three tables
  changed, each digest before and after in that folder's README: `differences.tsv.gz` (it was
  `b6fcb0389ec57f68987bd97d3895cd846575d2d281b8631ba908e5c5791f6c7d`, now
  `e918f46254ae5de50a2705927864cd8af311e8c026b5ae30dc23ecb20cfbee0e`), `classes.txt` (it was
  `caa7dee42609fe9c44116b57b4393da16325d0213a4b24ba0b1d0fbed3a1786f`, now
  `2bb2c4659bf01cbaa383f92f12dfc596540f30b6ec2a6e0d97717ebcffccda76`) and `unclassified.tsv` (it was
  `afce208f34568ed0d6db4af0667646bb4ccab07a14eb18c7721fddd39d93dc4b`, now
  `1622df7102aa57fa94730542b05df91843f5c2f8099feeefc8dc26166820194f`).
- **A parse-time reading aid** (step `d-base-ptime`, exit 0): `regress-610026.js`'s eval sources parsed at the old
  parser, the library alone and the bridge, each in its own process
  ([`ptime.tsv`](base/part-c/aids/base/ptime.tsv), `054bec53d5bd1cb7a79d08463c79a235d9d72504176cfea271d3734813ca4ff3`).
- **`test/staging/sm/regress/regress-610026.js`, one case, both variants** (steps `d-base-case-off` and
  `d-base-case-on`, exit 0 each): `scripts/compliance/run_test262.py` restricted to that path, with the shards' options
  otherwise, through the base build's shell with the hidden switch off and on, a recording shim giving each shell
  process's end, processor time and peak memory: the runner's records
  [`case-off/result.json`](base/part-c/aids/base/case-off/result.json),
  `63b7724fb6d0ad791a5240dc108cfe33c19133f3ff42414debd0ad7a884e068f`, and
  [`case-on/result.json`](base/part-c/aids/base/case-on/result.json),
  `7d01f624cd9c3b1bdb4c31ceb223822ca09a8f041db4bc79b01b5794a6afb0d1`, and the shim's
  [`case-off/shim.log`](base/part-c/aids/base/case-off/shim.log) and [`case-on/shim.log`](base/part-c/aids/base/case-on/shim.log).
- **The same case at W2's head** `029bab4c4f2457fed1896ab6c4d224350d809b40` (branch `sjl-14-w2`, read-only; steps
  `d-w2-archive`, `d-w2-build`, `d-w2-terser`, `d-w2-case-w2`, exit 0 each), built the same way, run with no switch
  argument as W2 has no switch: [`case-w2/result.json`](base/part-c/aids/w2/case-w2/result.json),
  `409f96180839aabb702f8233a5b94167e3143282fe8f967220e999ac73f6638f`, and its
  [`shim.log`](base/part-c/aids/w2/case-w2/shim.log).
- **Readings at their sites**, from the base commit's sources and the retained dumps, with nothing run: the
  `B-short-bracket` spans, the `for`-`of` case, and bundle `sjl-12-001`'s canonical dump for `P-eof-same-text`.
- **Three files kept whole**: part C's side-by-side report, part D's dump report and Broiler.HtmlBridge's TRX, the
  files the after comparisons read that were kept by digest only, are now in [`base/local/`](base/local/README.md) at
  their paths, unchanged, each matching its recorded digest; `not-retained.sha256.txt`, `hashes.txt`,
  `retained.txt` and `.gitattributes` there say so by dated notes. A decision taken under the owner's direction; the
  alternative not taken was to keep them only in the run folder, which the session's container does not keep.

**What each shows**, in words (the figures are in the files), read and not judged:
- **Class E by code.** Every outcome move read E from the bridge's text holds by the library's code, those read so only
  by narrowing to the classification's first rows included; none becomes unclassified, no E is provisional, and the
  unclassified differences are the same set. The three groups of items 1 to 3 read by code as the rows B's text gave:
  EX0009 for the getter with a parameter, EX0008 for the arrow function as a class heritage, EE0033 for the
  parenthesized literal as an assignment target; none is among the rows the classification lists first, and each stays
  unclassified.
- **`regress-610026.js`.** With the switch off both variants pass alone. With it on, and at W2's head, each variant's
  shell process ends on `SIGXCPU`, the signal of the CPU-time limit the runner sets to the test's timeout, with no
  output, which the runner records as a plain failure with no reason. The parse-time aid shows the library's parse
  alone close to the old parser's, and the bridge's, which adds the bridge's work beyond the library's parse (the
  token chain and the tree's construction, which the aid does not separate), far above both: the runner's
  limit stops the case, reached by the bridge's cost on these sources, not the library's. W2 does not change it. The
  aid parses each source under the script goal in a fresh process, not inside the case. *(Changed the same day, after
  review, before this record merged: "which adds the bridge's work ... does not separate)" read "which adds the tree's
  construction"; the last sentence is added.)*
- **`B-short-bracket`.** B's span is the AST type's derivation, a member ending where its property ends
  (`AstMemberExpression.cs:9` at the base commit, built by the bridge at `BridgeTreeBuilder.Expressions.cs:331`), the
  same derivation by which both trees end every other computed member before its `]`; P takes the bracket only through
  its comma expression's span. A span the bridge's tree carries, in `Broiler.JS/`, not the library's.
- **`for`-`of`, `head-await-using-bound-names-in-stmt.js`.** The early error is EE0056, the library's code its own; with
  the switch off the module host runs the body, as the old parser, which checks EE0056 by the stage table's row,
  misses it for this input (an `await using` head), as P's acceptance in the side-by-side report and the retained
  switch-off test262 record show; and the corpus's K
  refuses the input only for awaiting at the top level without the permission, so the corrections' tables explain it
  by the correspondence's D31 bucket and list no K row for it; as written, none does. (The library's row table says in
  EE0056's notes that the `using` heads, this test by name, "are EP rows"; the code it gives is EE0056's, `JSE1417`.)
  *(Changed the same day, after review, before this record merged: after "runs the body," the bullet read "as the old
  parser and compiler do not check it, and the corpus's K"; the parenthesis is added.)*
- **`P-eof-same-text`.** The canonical dump writes an end token of the end-of-file type as `eof` and any other as an
  offset, in one function; P's tree ends those nodes at the end-of-file token and B's at the node's last token. The
  tool's README and its rule describe the form, and say nothing of how `eof` is read against the same offset written
  as a number. This bundle's own span check, [`spancheck.py`](tools/side-by-side/spancheck.py), reads `eof` as the
  source's length, which is why these lines come out `both-agree` with identical texts; a reading only. *(Added the
  same day, after review, before this record merged.)*

What follows from each remaining unclassified item, under part C's "How it is read" and readings B8 and B9, is the
owner's to decide; nothing here decides it. *(Noted 2026-11-23, records part 5: the owner has decided, by answers 10
and 11 of 2026-11-23, recorded in "W2's base, records part 5" below; the sentence is left as written.)*

## W2's base, records part 5: the rule's revision of 2026-11-23, recorded on 2026-11-23

**Written:** 2026-11-23, during W2's hold, on branch `sjl-14-w2-base`, with the default branch's head read as
`69a8a13bc446a1714d3c8be9be3cfd742aebee6e` by one `git ls-remote` at 2026-11-23T19:19:01Z, before this record's
three commits of 19:31:47Z to 19:31:48Z; no later read was kept for them. **What it records:** the owner's
answers 10 and 11 of 2026-11-23, the rule's dated revision they file, and part C read again under it on the same
retained runs; nothing of the engine was built or run, and nothing was dispatched; only the reading scripts were run.
*(Changed the same day, after review, before this record merged: the paragraph read "by `git ls-remote` before the
commit", which no kept read shows for each commit, and "with nothing built, run or dispatched", which the reading runs
contradict. A read before each review-fix commit gave the same head.)* [`decision-rule.md`](decision-rule.md) is not edited, and no row
is added to [`corrections.md`](corrections.md) or its tables.

**The owner's answers 10 and 11 of 2026-11-23**, the owner's own and not ones taken under the owner's direction of
2026-10-18, quoted verbatim:
- Answer 10. Asked (2026-11-23): "SJL-14 W2: part C reads NOT MET at the base. After investigation (verified), no
  library defect was found, so no dark merge is needed and the base runs can stand. The remaining unclassified
  differences: (1-3) 11 inputs where the old parser wrongly accepted and the bridge correctly refuses, but with a
  different first error row than the classification expects; (4) the switch's own architecture test; (5) a for-of
  `await using` test that moves fail→pass (an improvement) with no correction row; (7) one Terser-minified variant the
  runner skips; (8) 3,625 dump lines where P writes the end of input as `eof` and B writes the same offset as a number;
  (9) 6 computed-member spans the AST type derives the same way in both trees. Groups 1-3, 8 and 9 will also reappear
  in part D at the after run. Admitting them after the runs were seen needs a dated rule revision, which the precedents
  (your SJL-11 answers 7 and 8) treat as yours. How should I proceed?" Chosen: "Revise rule (Recommended)" — "I draft
  one dated revision of sjl-14-001's rule from your answer, admitting groups 1-5, 7, 8 (and 9 if you choose C below)
  for part C and part D, marked as weaker evidence written after the runs were seen. Part C is then re-read on the same
  retained runs, nothing re-run, and W2 proceeds to its PR." Not taken: "Merge as NOT MET" — "Change no rule. Record
  part C as read NOT MET at the base, with the unclassified list, and proceed to W2's PR and merge anyway (nothing is
  accepted under EX-30). Part D at the after run will then also read these as regressions."; "Hold W2" — "Stop before
  W2's PR. Leave the hold on Broiler.JS main and the base runs as they are until you review the decision summary
  yourself."
- Answer 11. Asked (2026-11-23): "Group 9 (6 variants): the bridge ends a computed member like `a[b, c]` where its
  property ends, before the `]`, because AstMemberExpression derives its end that way (the old parser included the `]`
  only by its comma-expression overshoot). Fix it in W2 or admit it?" Chosen: "Admit by revision (Recommended)" —
  "Include group 9 in the dated revision. W2's trees stay as they are; no other span changes." Not taken: "Fix in W2"
  — "Change the after tree so a computed member ends at its `]`. More faithful to the source text, but it changes every
  computed member's span in the after tree (part D would read those as class S). Needs the parse-corpus check and the
  tests re-run."

The question's groups are part C's unclassified items 1 to 9 ([`base/part-c/README.md`](base/part-c/README.md)), by
their numbers; group 6 is not in it. The question's count for group 8 is of input variants, one row each of
`base/part-c/unclassified.tsv`, not of dump lines.

**The revision**, [`decision-rule-revised-2026-11-23.md`](decision-rule-revised-2026-11-23.md), SHA-256
`1f9c7e539716b9d498c45592ca057ecb65e6543dcd613dfc09d2d4a6ba6b921e`, written after part C's base runs were seen and
therefore weaker evidence, as the rule's section "Revisions" reads: "A rule revised after any run it judges has been
seen is a new dated file in this directory. It quotes this one whole and says it was written afterwards, and its
decision is then weaker evidence." It quotes the rule whole and the lines it changes, and changes only these: part C
admits six classes, each naming its entries, rev-code (groups 1 to 3: the inputs and the library's codes EX0009,
EX0008 and EE0033 by name), rev-eof (group 8: `eof` read as the same offset as the source's length, one span, for the
input variants part C's base reading lists), rev-bracket (group 9: the member, optional-chain and import-call spans of
`T036087`, `T039546` and `T051374`), rev-switch-test (group 4), rev-for-of (group 5, listed with K7 and D35, with the
library's row table's note that the `using` heads are EP rows) and rev-minifier (group 7); part C's MET line reads
with them; part D admits rev-code, rev-eof and rev-bracket, for the same entries, and its MET line and its "How it is
read" read with them. Listing group 5 with K7 and D35 is the revision's drafting from the decision summary the
question rested on; answer 10 names no row. **Group 6 is not admitted**: `test/staging/sm/regress/regress-610026.js`
(both variants) is a bridge defect the bundle's decision B8 (recorded with W2 on branch `sjl-14-w2`: "One part C finds
unclassified at the base commit is fixed inside W2, on this branch, and part C reads NOT MET for that input unless a
class admits it.") sends inside W2, which fixes it by `e0b2d17bda6e578c0a8213f5263374f0fdb13ded` and its review fix
`80b04d26fca6138c4337b000bc621876ec78cd7e`. Part A is not revised.

**Part C, read again** on the same retained runs by [`base/part-c/scripts/read_base.sh`](base/part-c/scripts/read_base.sh)
with the scripts taught the revision's classes, through the machine-wide lock and idle gate, the same bytes twice;
without the revision the scripts write the earlier tables byte for byte. Each run, the tables that changed with their
digests before and after, and the check of the new classes against the earlier `unclassified.tsv`, are in
[`base/part-c/README.md`](base/part-c/README.md)'s "Records part 5", with the runs' logs in
[`base/part-c/logs-revised/`](base/part-c/logs-revised/runs.tsv). That folder's [`hashes.txt`](base/part-c/hashes.txt)
now reads SHA-256 `0e898cfaaae7185b1d8d8c95a88a64b53e5ec755ccf3be0162f01b4b9dec7927`. *(The section "W2's base, records part 4" names that file's digest as
`992e87c1e547fe4b97e1e4a1b49cb9baa182296a120c186703ddf7a05c2c806c`, of the folder before this record; it is left as
written there.)*
- **Part C: read NOT MET on the retained runs**, under the rule's revision of 2026-11-23, for
  `test/staging/sm/regress/regress-610026.js` (both variants), passing with the switch off and failing with it on,
  which no class admits. Not judged beyond that (EX-30). The reading of the section "W2's base, recorded on
  2026-11-23", under the rule as written, stays on record as taken. *(Changed the same day, after review, before this
  record merged: after "which no class admits" the reading read "; the tables read every other difference in a
  class.", which went beyond the reading; the counts are the tables'.)*
- **Finding S14-F2, against the correspondence**, as part A's C1 line asks ("a C1 row that none explains is recorded as
  a finding against the correspondence and is a regression"):
  `test/language/statements/for-of/head-await-using-bound-names-in-stmt.js`, original variant, fails with the switch
  off and passes with it on, and no row of K1 to K10 explains it: the corrections' tables explain the path by the
  correspondence's D31 bucket alone, while what the shell shows is the early error EE0056 that the old parser misses
  for an `await using` head, which K7's D35 ("Lexical redeclarations Broiler.JS misses (async functions and generators in blocks, catch and
  others)"; cut after "misses" before review, as the revision's quote is) describes, and the library's
  row table calls the `using` heads, this test by name, EP rows. Recorded here, for the correspondence's owner;
  [`base/part-c/README.md`](base/part-c/README.md)'s "Records part 5" gives the sites. Part A's lines are not
  revised, so part A's after comparison reads this path as part A's C1 line reads it.
- **What part D's after reading will admit**, under the revision: part C's C1, C3, E, T, M and S as the rule words them,
  and rev-code, rev-eof and rev-bracket for the inputs and input variants the revision names, each read at entry point
  P as the base commit's old parser against the after commit's `FastParser` facade; every other difference is a
  regression. Groups 4 to 7 are not in part D's population. No part D after run exists.

**After review, 2026-11-23**, before this record merged, with the default branch's head read as
`69a8a13bc446a1714d3c8be9be3cfd742aebee6e` by `git ls-remote` before each review-fix commit (each read, with its time,
is kept in the session's notes). [`base/part-c/scripts/classify_c.py`](base/part-c/scripts/classify_c.py) read
rev-eof for any input variant whose differing lines were all S or the `eof` reading, without testing that the variant
is one the revision names by the earlier `unclassified.tsv` (git blob `4cb883804fcea72f29621a05d8476dc0168f5b47`); it
now reads that blob by its digest and admits by rev-eof only the input variants it lists, so that a part D reader built
from it admits no other. Read again on the same retained runs through the same lock and gate, the tables are the same
bytes; [`base/part-c/README.md`](base/part-c/README.md)'s "Records part 5" gives the runs, with their logs in
[`base/part-c/logs-fix/`](base/part-c/logs-fix/runs.tsv), and how this record reads the revision's quoted reason
(which in the blob goes on with a count) and its D35 quote (the row's opening words). Finding S14-F2 is recorded in
this section, not in [`corrections.md`](corrections.md). That folder's [`hashes.txt`](base/part-c/hashes.txt) now
reads SHA-256 `52b87e74d8fc5d921378fc3833eac6508dfd1eb72f7b9102bf59f0591765964c`; the paragraph "Part C, read again"
above names its digest of before this change, and is left as written. Part C's reading under the revision is
unchanged.

## W2's after commit, recorded on 2026-11-24

**Written:** 2026-11-24, on branch `sjl-14-after` from the after commit, with the default branch's head read as
`83a9bd82a5d258d3001468095ebf43f9efc3e9a7` by `git ls-remote` before each of this record's commits.
[`decision-rule.md`](decision-rule.md) and [`decision-rule-revised-2026-11-23.md`](decision-rule-revised-2026-11-23.md)
are not edited, no row is added to [`corrections.md`](corrections.md) or its tables, and nothing retained in
[`base/`](base/local/README.md) changes. Each part is read in the rule's words, under the revision where the revision
changes the part (parts C and D), on the files named with it; nothing is judged beyond the reading, and nothing is
accepted (EX-30).

### The after commit and the end of the hold

- **W2 merged** as `83a9bd82a5d258d3001468095ebf43f9efc3e9a7`, the merge of Broiler-Platform/Broiler.JS#1129
  (2026-11-23T20:54:36Z), its first parent the base commit `69a8a13bc446a1714d3c8be9be3cfd742aebee6e` and its second
  `a2d554a9a0b2062ecd354a64e04f0fbe163c1c4f`, the head of `sjl-14-w2`. It is **the after commit** the rule names ("the
  default-branch merge that lands SJL-14.3 to SJL-14.5, one merge"), and `69a8a13b`, at which every base run was taken,
  is its first parent, as the rule's "The base commit is its first parent" asks.
- **The hold of the owner's answer 9 ended** with that merge ("nothing merges to Broiler.JS main until W2 merges"). The
  default branch's head has been the after commit since, through this record's commits.

### Part A's after run, retained

- **The run.** `.github/workflows/test262-evidence.yml` at the after commit, blob
  `fc38229782ee1b2e93ecca9e990d182f5e44dbf2`, the blob the base commit carries and the base run ran: workflow run
  [45264304825](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/45264304825) (attempt 1), dispatched on
  2026-11-23 at 20:55:16Z on `main` with the base run's inputs but for the commit, conclusion success, and success for
  every job at `83a9bd82`. Its head commit, its `run.txt`'s dispatched ref and tested commit all read the after commit,
  and `run.txt`'s inputs line reads the inputs part A fixes (time_zone empty is not echoed there). Between the two
  commits `scripts/compliance/run_test262.py` changes in two comments only, and the comparison and merge scripts not at
  all.
- **Retained** in [`after/part-a/`](after/part-a/imported.txt) as [`base/part-a/`](base/part-a/imported.txt) retains
  the base run: every entry of the evidence artifact unchanged, but `test262-results.json.gz`, over 2 MB, by its digest
  only, as the base's; every artifact of the run downloaded and its zip's SHA-256 equal to the digest GitHub records
  ([`part-a-artifacts.txt`](after/part-a/part-a-artifacts.txt)); the evidence artifact is
  `sha256:84f74d007d7376eccd96c14a9831c430095cf54bf3406b07544d94ddd3ab8c8e`. The base run's evidence zip was downloaded
  again for the comparison and matches the digest the base records give it,
  `sha256:12163b9df6e680ddfbd50b3f9356d9bae2f2e2c1cb417ace2045ca7223c647c1`. The folder's
  [`hashes.txt`](after/part-a/hashes.txt) names every file by its SHA-256.

### Part A, read

The comparison is the rule's, `scripts/compliance/compare_test262_paths.py` as committed at SJL-0.9 (unchanged at the
after commit), with each run's re-run of its timed-out path, and its classification file
([`after/part-a/comparison/`](after/part-a/comparison/comparison.log): `classifications.json`, `comparison.json`,
`comparison.log`, `compare-checks.log`, `completeness.log`, `zone-records.log` and `named-rows.log`).
- **Complete and comparable:** every expected shard reported in both runs, no configuration failure, one suite revision,
  one shell in each run (and in each re-run), every shard in the zone, `includeNegative` true in both
  ([`completeness.log`](after/part-a/comparison/completeness.log)); the two runs' host-zone records read alike, and are
  one file byte for byte ([`zone-records.log`](after/part-a/comparison/zone-records.log)); the script finds the runs
  comparable (it exits 1, for rows it does not admit, not 2). The same RID, linux-x64; the runner image versions
  differ, which the rule does not fix ([`imported.txt`](after/part-a/imported.txt)). The script's two checks pass: the
  after run against itself changes nothing, and a copy with one row flipped reports that row
  ([`compare-checks.log`](after/part-a/comparison/compare-checks.log)).
- **Every changed row but two** fails at the base and passes after on a path `corrections/k-paths.tsv` lists for a row
  of K1 to K10, and is C1, listed in `classifications.json` with its path, its variant and its correction (part C's
  reading of a test262 case, `base/part-c/scripts/classify_local.py`); and each such path is listed by name, in
  backquotes, in `Broiler.JS/CHANGELOG.md`'s section "The cut-over to the shared syntax library", which names K1 to K10
  by row (checked path by path at the after commit, 2026-11-24, in review of this record), as C1's "and in the
  changelog" asks. No row is C3 or T. *(Revised 2026-11-24, in review: this sentence first read "the changelog's
  section "The cut-over to the shared syntax library" names K1 to K10 by row".)*
- **The two others:**
  - `test/language/statements/for-of/head-await-using-bound-names-in-stmt.js`, original variant, fails at the base and
    passes after, and no row of K1 to K10 explains it: part A's C1 line reads "a C1 row that none explains is recorded
    as a finding against the correspondence and is a regression". It is finding S14-F2, recorded in "W2's base,
    records part 5" above; the revision's rev-for-of admits it at part C alone ("Part A is not changed"). Read here as
    part A's C1 line reads it: a regression.
  - `test/language/expressions/dynamic-import/syntax/valid/new-covered-expression-is-valid.js`, Terser variant, passes
    at the base and is skipped after, the runner's record giving `minifier-invalid-output` (node refuses the minified
    text, which the after commit's frontend refuses too, where the base's parser accepted it). Part A's last class line
    reads "every other difference is a regression, including a row that passes at the base and fails, times out or is
    skipped after outside C3"; it is not C3. A regression. The revision's rev-minifier admits it at part C alone.
- **`test/staging/sm/regress/regress-610026.js`** (decision B8, group 6), both variants, passes at the base and passes
  after, within the per-test timeout in each, so it is no changed row
  ([`named-rows.log`](after/part-a/comparison/named-rows.log)); B8's fix in W2 holds in part A's runs.
- **Part A: read NOT MET on the retained runs**, for those two rows (the files above;
  [`after/part-a/hashes.txt`](after/part-a/hashes.txt) names each by digest). Not judged beyond that (EX-30). *(Noted
  2026-11-24, later: the owner's answer 12 of 2026-11-24 filed the revision
  [`decision-rule-revised-2026-11-24.md`](decision-rule-revised-2026-11-24.md); part A is read again under it in
  "W2's after commit, records part 2, recorded on 2026-11-24" below. This reading stays as taken.)*

### The after commit's local runs, retained

The after driver's runs at the after commit, taken 2026-11-23 from 21:21Z to 22:59Z behind the machine's lock and idle
gate with the default branch's head read as the after commit before every step, are retained in
[`after/local/`](after/local/README.md) by [`base/local/`](base/local/README.md)'s convention: every file unchanged at
its path, the one over 2 MB the comparisons do not read whole by digest, the two they read whole kept whole
(`partd/parse-corpus-d1.tsv.gz`, `html/htmlbridge/trx/htmlbridge.trx`), the scripts as they ran and as they were
started, and the machine records; [`after/local/retained.txt`](after/local/retained.txt) names every step, its
command, its outputs, its exit (every step exited 0) and every departure from the base's harness. The runs owed to the
readings, taken 2026-11-24 after every after run, are in [`after/owed-runs/`](after/owed-runs/README.md): the base
commit's dump text for part D, Broiler.JSeal's base side per test for part G, and single-case compiles for part J.

### Part D, read

[`after/part-d/`](after/part-d/README.md) classifies every difference of the comparison the rule names ("per input,
variant and entry point: outcome, exception type and message; per input, the dump hashes") with part C's own
classifier, unchanged, the base commit's P read against the after commit's P (the adapter's `FastParser` facade) and
the base's K against the after's K, under the revision's part D classes; its
[`cross-check.txt`](after/part-d/cross-check.txt) finds the classifier's rows and `compare_parse_corpus.py
acceptance`'s list to be one set, and the dump rows to be every input variant whose dump hash differs.
- **Complete:** every input and variant at both entry points at both commits, the manifest's digest equal
  (`acceptance.log`). **The control is reported:** the copy of the base report with one outcome flipped and one message
  changed is reported as exactly those two differences ([`control.log`](after/part-d/control.log), from
  `after/local/dcontrol/`).
- **Outcomes and messages:** every difference is in C1, C3, E, M or rev-code ([`parser/classes.txt`](after/part-d/parser/classes.txt));
  class E and rev-code are read by the after commit's codes (`after/local/dcodes/bridge-codes.tsv`), rev-code's inputs
  each with the code the revision names.
- **The parser trees' dumps:** every difference is in C1, S, rev-eof or rev-bracket, each listed with its lines
  ([`parser/`](after/part-d/parser/unclassified.tsv) lists none unclassified). These are the base's part C classes of
  the same input variants: the base commit's P and K rows and parser dump hashes are, field for field, the base
  side-by-side run's P and K, and the after commit's its B and K-on
  ([`identity-with-part-c.txt`](after/part-d/identity-with-part-c.txt)).
- **The dumps after the compiler's for-loop pass:** the dump report hashes each accepted variant's tree twice, as the
  parser built it (`parser-sha256`) and after the compiler's for-loop pass (`pass-sha256`), the "--dump mode (before and
  after the compiler's for-loop pass)" part B's configuration names and part D's configuration takes. Part C compared
  parser trees only, so these differences were never read before. Most are in S, rev-eof, rev-bracket or C1, but a set
  of input variants ([`pass/unclassified.tsv`](after/part-d/pass/unclassified.tsv)) differ in span lines no class
  admits ([`pass/unclassified-lines.txt`](after/part-d/pass/unclassified-lines.txt)): spans of nodes the pass builds or
  copies from the parser tree, such as a `for` head's declaration moved into the loop's body (read there as a
  declaration in statement position, whose after span leaves out a `;` the base's takes), a temporary's identifier that
  takes its span from a destructuring pattern's edge, and the blocks and expressions the pass synthesises, whose spans
  in both trees agree with no source text. Every such input variant's parser-tree difference is S or rev-eof, and none
  of their differing lines is outside spans; the rule has no class for a pass-tree span that follows an admitted
  parser-tree span.
- **Part D: read NOT MET on the retained runs**, for those pass-tree differences (the files above;
  [`after/part-d/hashes.txt`](after/part-d/hashes.txt)). The base commit's pass text, which the reading needs, was not
  in the base records (part C's base text holds parser trees only); it was taken at the base commit after the after
  runs by the rule's own tool, its report identical line by line to the base's retained dump report
  ([`after/owed-runs/dtextbase/`](after/owed-runs/dtextbase/identity-with-base-d1.txt)), so it describes the base run's
  trees and weakens nothing. Not judged beyond that (EX-30). *(Noted 2026-11-24, later: the owner's answer 13 of
  2026-11-24 filed the revision [`decision-rule-revised-2026-11-24.md`](decision-rule-revised-2026-11-24.md); part D
  is read again under it in "W2's after commit, records part 2, recorded on 2026-11-24" below. This reading stays as
  taken.)*
- *(Noted 2026-11-24, after the comparison, in review of this record.)* The paragraphs above and
  [`after/part-d/README.md`](after/part-d/README.md) name the two commits' dump texts as "kept outside the repository,
  by digest" ([`after/part-d/texts.sha256.txt`](after/part-d/texts.sha256.txt)). On 2026-11-24, once the comparison was
  done, both texts (`/home/user/js-w2read-build/69a8a13b/text/dumps-p.txt.gz` and
  `/home/user/js-w2after-build/83a9bd82/text/dumps-p.txt.gz`), `read_d.sh`'s work files
  ([`after/part-d/work.sha256.txt`](after/part-d/work.sha256.txt)) and the after commit's candidate feed were removed
  with the build folders that held them; they remain named by digest only. So `parser/`'s and `pass/`'s tables
  regenerate only after the after driver's `dtext` step and the owed `dtextbase` step are taken again (through
  `run.sh`, a build at each commit) and `read_d.sh` is run on their texts; the parts that need no text
  (`acceptance.log`, `acceptance.json.gz`) regenerate from the retained reports. And the texts' own bytes were not
  shown reproducible: `identity-with-partd.txt` and `identity-with-base-d1.txt` compare the dump reports' rows, with
  the `#` lines (among them each assembly's identifier) left out, not the texts. *(Noted 2026-11-24, later: the
  review's re-take of both texts, retained in [`after/owed-runs/verifier-retake/`](after/owed-runs/verifier-retake/runs/logs/runs.tsv),
  reproduced their digests; "W2's after commit, records part 2, recorded on 2026-11-24" below records it. This sentence is left as written.)*

### Part E, read

- **The tests** of `Broiler.JS/Broiler.JavaScript.Portable.Tests` in Release at the after commit all pass
  ([`after/local/ptests/`](after/local/ptests/trx/Broiler.JavaScript.Portable.Tests.trx)).
- **The collector**, `scripts/performance/collect_phase5.py --rid linux-x64` at the after commit: the regenerated
  program is identical to the checked-in `FibonacciProgram.g.cs` (`matchesCheckedInProgram` true, the two digests
  equal, [`after/local/phase5/`](after/local/phase5/phase5-report.json)), the sample publishes with Native AOT, runs and
  answers as the collector requires (no validation error), with no dynamic code (`dynamicCodeSupported` and
  `dynamicCodeCompiled` false). At the base the program differed in the form of its constants
  ([`base/local/README.md`](base/local/README.md)); W2's `8133ea0c` regenerates the checked-in copy, and the rule
  judges the after commit alone.
- **The closure** of `Broiler.JavaScript.Portable.Compiler` at the after commit (its project and package references,
  transitive, and its publish output) names no `Broiler.JavaScript.Runtime` and no `Broiler.JavaScript.Storage`
  ([`after/local/closure/`](after/local/closure/runtime-storage-lines.txt)).
- **Part E: read MET on the retained runs** (the files above; [`after/local/hashes.txt`](after/local/hashes.txt)).

### Part F, read

- **Packing and feed verification pass** for both sets at the after commit ([`after/local/pack/`](after/local/pack/logs/runs.tsv));
  the Shared set packs at the source's own `0.1.0-preview.12` (decision B5).
- **The pristine consumer passes** against that feed (`after/local/pristine/`), and **its control fails as stated**:
  with `Broiler.ECMAScript.Frontend` removed, the restore fails `NU1101` naming that package (`after/local/pcontrol/`).
- **The marks.** [`after/part-f/reading.txt`](after/part-f/reading.txt) reads the metadata reader's every row
  (`after/local/obsolete/obsolete-marks.tsv`, the after packages of `Ast` and `Parser`) against `dispositions.tsv`'s
  type lines: every type `dispositions.md` marks obsolete (`obsolete`, `facade-obsolete`) carries `[Obsolete]` with
  `BJS0002` and a message naming `1.0.0`, the removal version decision D50 names, and none is missing; the non-tree
  types are forwarded to `Broiler.JavaScript.Expressions`. **The one row not marked** is
  `Broiler.JavaScript.Ast.Misc.FastParseException`: `dispositions.md` gives it `moved-forwarded`, which asks no mark
  ("not marked obsolete (SJL-D-25 (a))"), so part F's line ("every public type dispositions.md marks obsolete carries
  the mark") asks nothing of it. That it stays in `Ast` and is not forwarded is W2's recorded departure ("Where W2's code
  departs" above; `docs/public-api.md`'s row for it), read at part H.
- **Part F: read MET on the retained runs** (the files above).

### Part G, read

- **Broiler.HtmlBridge**, per test, against the base candidate (`base/local/part-g/htmlbridge/trx/`) and the after
  candidate (`after/local/html/htmlbridge/trx/`): the same tests on both sides, and every test that passes against the
  base candidate passes against the after candidate ([`after/part-g/per-test.txt`](after/part-g/per-test.txt)).
- **Broiler.JSeal**, per test. The base run of 2026-11-23 kept summaries and skipped names but no passed test by name
  (`base/local/retained.txt`, "Owed"), and part G's comparison is "per test, its result against the base candidate and
  against the after candidate", with "every test's result retained". The base side is therefore taken again here, after
  the after runs: the base's own harness (`base/local/scripts/bstep.sh`'s `jseal` step as its attempt 3 ran it) against
  the kept base candidate feed, whose every file matches the base's digests, with the candidate directory the base
  run's byte for byte, and the after run's one addition, a TRX of each test project
  ([`after/owed-runs/jsealbase/`](after/owed-runs/jsealbase/jseal-trx.tsv)). Its report equals the base run's in every
  leaf but times, paths and the digests of Broiler.JSeal's own projects, which the consumer builds again in a fresh
  folder ([`report-against-base-run.txt`](after/owed-runs/jsealbase/report-against-base-run.txt)). Against it, in both
  configurations, the same tests on both sides, and every test that passes against the base candidate passes against
  the after candidate ([`after/part-g/per-test.txt`](after/part-g/per-test.txt)). **What it weakens:** JSeal's base
  side is a second run of the base's harness on the base's bytes, taken after the after runs were seen, not the run of
  2026-11-23 itself; the inputs it ran on are those of that run, checked by digest, and it ran no choice of this
  stage's.
- **Part G: read MET on the retained runs** (the files above; [`after/owed-runs/hashes.txt`](after/owed-runs/hashes.txt)).

### Part H, read

[`after/part-h/read_h.py`](after/part-h/read_h.py) compares the API lister's listings at the base commit
(`base/local/part-h/api/`) and at the after commit (`after/local/api/api/`), every assembly of both pack sets, per
member, and reads every member removed, changed or newly obsolete against `docs/public-api.md`'s rows at the after
commit (its section "Public API changes, 2026-11-19 (window W2 ...)"), row by row, and against `dispositions.tsv`
([`after/part-h/members.tsv`](after/part-h/members.tsv), [`after/part-h/reading.txt`](after/part-h/reading.txt)):
- **Only `Ast`, `Compiler`, `Expressions` and `Parser` differ**; every other listing is the same at both commits.
- **`Parser`:** every member newly obsolete, by its type's mark, which names `BJS0002` and `1.0.0`: public-api.md's row
  "Every public type of `Broiler.JavaScript.Parser` ... and every member of each", (b) obsolete, removal `1.0.0`;
  `dispositions.tsv` gives each `facade-obsolete`. `FastTokenStream.UndoMark()` keeps its own mark of SJL-12.5
  unchanged, as its row (`kept-obsolete-no-version`) says. No member is removed.
- **`Ast`:** every member of the tree's types newly obsolete, with `BJS0002` and `1.0.0`: the row "Every public type of
  `Broiler.JavaScript.Ast` but the non-tree types below and `FastParseException`", (b) obsolete, removal `1.0.0`;
  `dispositions.tsv` gives each `obsolete`. The non-tree types' members leave `Ast` and arrive in `Expressions` with the
  same namespace, signature and attributes, and `Ast` forwards each type (the after commit's metadata reader): the row
  for them, (a) forwarder; `dispositions.tsv` gives each `moved-forwarded`. `FastParseException` is unchanged.
- **`Compiler`:** one member added, `DirectEvalSupport.Execute`'s overload with `inDerivedConstructor`, which has its row
  ("added"); an added member is not one part H's line asks a disposition of.
- No member is removed, changed or newly obsolete without a row, and `dispositions.tsv` agrees with every row that
  covers one.
- **Part H: read MET on the retained runs** (the files above).

### Part I

Not taken: part I is the owner's run on the controlled lane `win-x64-r7-5800x`, arm B built from the base commit and
arms A and A-again from the after commit, under reading B10's protocol, by the collector committed at the after
commit, [`collect-part-i.py`](collect-part-i.py); its brief is with the owner. Its outputs are to be retained in
`measurement/win-x64-r7-5800x/` (and a confirmation run in a folder of its own beside it), as the note of 2026-11-18
under "What it will hold" and [`inputs/workloads/README.md`](inputs/workloads/README.md) require: "No figure produced
from them is retained anywhere in this repository except in the bundle's `measurement/` sub-bundle." **Pending on the
owner's lane.**

### Part J, read

[`after/part-j/findings.txt`](after/part-j/findings.txt) lists the session's findings and the control's by seed, type
and message, from the after run's sessions (`after/local/fuzz/session/`, `after/local/fcontrol/session/`):
- **The control is reported:** in the scratch copy whose bridge throws on a template literal, the session reports
  `System.InvalidOperationException` findings for the seeds that hold template literals; the revert is checked by
  digest and against a clean archive (`after/local/fcontrol/revert-check.txt`).
- **The session reports findings**, of two seeds alone, every one also in the control:
  - **T037823** (`test/language/expressions/import.meta/distinct-for-each-module.js`, goal module): mutants that compile
    throw `Broiler.JavaScript.Runtime.JSException` "import.meta is not available: this module has no module record".
    The seed itself throws it, alone and unloaded ([`after/owed-runs/jsingle/cases.tsv`](after/owed-runs/jsingle/cases.tsv)).
    It is the compiler's ReferenceError for `import.meta` in module code compiled without a module record
    (`Broiler.JS/Broiler.JavaScript.Compiler/Expressions/FastCompiler.VisitMeta.cs`, unchanged since `68f9a87a`, before
    W1), which its documentation calls deliberate ("a deterministic ReferenceError naming that rather than a silent
    `undefined`"), and the session compiles every module seed through `CoreScript.Compile(..., isModule: true)` with no
    module record. Read: the engine's throw site is not W2's change (no commit from `68f9a87a` to the after commit
    touches that file); the input defect, a module seed compiled where no module record exists, is the session's, and
    the session is W2's own tool, committed to the bundle with the after commit's change (`8a0928f4`, "SJL-14: W2, part
    J's mutation session and part K's deletion search, committed with the after commit's change", and W2's review fixes
    `a352fd1c`; neither is in the base commit), whose `tools/fuzz/Program.cs` calls `CoreScript.Compile(...,
    isModule: module)` with no module record. By the rule's reading ("a finding is any other exception type") each is a
    finding. *(Revised 2026-11-24, in review of this record before it merged: this sentence first read "Read: a
    fuzz-harness input defect (a module seed compiled where no module record exists), not W2's".)*
  - **V000002** (`inputs/broiler-vm/generated/refuse-more-locals-than-the-frame-admits.js`, goal script, a generated
    program of very many declarations): mutants that the frontend accepts compile past the session's bound per compile.
    The seed itself, and each of its timed-out mutants, compiled alone with nothing else running, takes longer than the
    bound ([`cases.tsv`](after/owed-runs/jsingle/cases.tsv), each `within-bound` "no"): the timeouts reproduce unloaded;
    they are the seed's compile cost against the header's bound, not the machine's load. Part J's rule asks no base-side
    run, so whether the base compiles it within the bound is not read.
- **Part J: read NOT MET on the retained runs**, for those findings (the files above). Not judged beyond that (EX-30).
  *(Noted 2026-11-24, later: the owner's answer 14 of 2026-11-24 ordered a corrected session and filed the revision
  [`decision-rule-revised-2026-11-24.md`](decision-rule-revised-2026-11-24.md); part J is read again on the corrected
  session in "W2's after commit, records part 2, recorded on 2026-11-24" below. This reading stays as taken.)*

### Part K, read

[`after/part-k/hits.tsv`](after/part-k/hits.tsv) reads every hit of the after run's search
(`after/local/delsearch/search.txt`) again from the after commit's tree and sorts it as the search's README says the
reading does: facade members, the compiler's own `ContainsLegacyOctalLiteral` (and its helper) that `dispositions.md`
keeps until SJL-15.5, other types' members of the same name (the bridge's, the library's, a token's), and text (code
comments, and lines of changelogs, assurance records and manifests). **The sort is by file and by the line's own
text** ([`read_k.py`](after/part-k/read_k.py)'s docstring): a code line of a facade file is sorted a facade member, a
code line under the bridge's folder the bridge's own member, and a code line under `Broiler.ECMAScript/` the library's
own; only the lines it names one by one are read singly. It does not read each hit's declaration: the bridge's and the
library's hits are not listed with the declaration that shows them another type's member, and a facade file's hit is
not checked against `dispositions.tsv`'s member lines.
- **No listed implementation is found, as far as that sort reads:** the old parser's partial files are absent; the
  scanning code's distinctive names, the division fallback, the re-scans, the direct-eval prefix and the
  parenthesis-balance marker have no hit but text, lines sorted to other types' members and the compiler's own kept
  `ContainsLegacyOctalLiteral`; every code hit in the facade files is a line of a facade file `dispositions.md` keeps
  over the bridge, or, read singly, a name of another type used there.
- **The search's narrowed scopes.** The rule's configuration searches "over Broiler.JS/ and Broiler.ECMAScript/ outside
  docs/". The committed search ([`tools/deletion-search/search.py`](tools/deletion-search/search.py): a name that is a
  common word of the engine's code "is searched in the file that declared it at 33d3093b") took these items in
  narrower scopes, each printed in `search.txt`'s section header: the members FastParser's partial files declared, in
  the parser's folder `Broiler.JS/Broiler.JavaScript.Parser`; FastScanner's common names, in its `FastScanner.cs`;
  FastTokenStream's scanning code, in its `FastTokenStream.cs`; FastKeywordMap's table, in its `FastKeywordMap.cs`;
  CharExtensions' scanner members, in the parser's folder; the parser's half of FastScope, in its `FastScope*.cs`; and
  the direct-eval prefix's text, in `Broiler.JS/Broiler.JavaScript.Compiler`. That is a departure from the rule's
  configuration. [`after/part-k/scope.txt`](after/part-k/scope.txt), by [`scope_k.py`](after/part-k/scope_k.py) at the
  after commit, reads it against the rule's scope: every file of the rule's scope that declares one of the listed
  types, at `33d3093b` and at the after commit, lies inside the narrowed scope that type's names were searched in, and
  the parser's project compiles no file from outside its folder, so the narrowing hides no declaration of a listed
  type's own member, and a reference to one could only name a member declared there. What it leaves out are the same
  words outside those scopes, which `scope.txt` counts name by name and does not list (other types' members and text),
  and the prefix's text in two files of `Broiler.ECMAScript/` (the library's own strict prologue, in its determinism
  tests and its probe, not `DirectEvalSupport`'s). Read as the rule is written, those are further hits that are neither
  facade members nor relocations; under reading K1 below they are not hits of a listed member.
- **The changelogs' lines** are inside part K's scope ("over Broiler.JS/ and Broiler.ECMAScript/ outside docs/"; both
  changelogs are under those folders, and the rule's list of product paths, which leaves them out, is not part K's
  scope). `Broiler.JS/CHANGELOG.md:951` (`--parse-through-bridge`, `BroilerParseThroughBridge`; with `:949`,
  `JSCompilationOptions.ParseThroughBridge`) is the changelog's entry of 2026-11-03 for W1, prose naming the test-only
  switch as W1 added it; the switch, which `dispositions.md` lists as "added at W1, removed at W2", has no declaration
  or reference left in code (its section's other names have no hit). The changelog's lines `:79` (W2's own entry,
  correction K6's row T26, "at the base, DecodeBracedUnicodeEscapes rewrites the pattern text"), `:216` and `:217`
  (SJL-11.5's entry) name `DecodeBracedUnicodeEscapes` and `RegExpValidator` in the same way, as removed; and
  `Broiler.ECMAScript/CHANGELOG.md:1002`, the library's own changelog, names its `UriCodec`'s `HexDigitValue`, the
  library's member of the same name as FastScanner's deleted one.
- **The control is reported:** a deleted member declared again in a scratch copy is found, and after the revert the copy
  equals the clean tree (`after/local/delsearch/control.txt`, `revert-check.txt`).
- **Part K: read NOT MET on the retained runs**, as the rule is written. Its MET line asks that "every hit is a facade
  member dispositions.md lists as an obsolete forwarder over the bridge or a relocation decision D51 allows", and the
  changelogs' lines above, the code comments, the manifests' and assurance records' lines and the other types' members
  of the same name (`hits.tsv`'s sorts other than the facade members and the compiler's kept member) are neither; and
  the narrowed scopes depart from its configuration. The model the rule names, "as bundle sjl-11-001's part E runs its
  E-1 search", needed explicit words for exactly these hits: E-1 searches "over Broiler.JS/ but CHANGELOG.md" and
  excepts "a hit the bundle lists with the declaration that shows it is another type's member of the same name"
  ([`../sjl-11-001/decision-rule.md`](../sjl-11-001/decision-rule.md), part E). Part K restates the scope with neither
  carve-out. The files: `after/local/delsearch/` ([`after/local/hashes.txt`](after/local/hashes.txt)) and
  `after/part-k/` ([`after/part-k/hashes.txt`](after/part-k/hashes.txt)). Not judged beyond that (EX-30). *(Noted
  2026-11-24, later: the owner's answer 15 of 2026-11-24 filed the revision
  [`decision-rule-revised-2026-11-24.md`](decision-rule-revised-2026-11-24.md), worded on E-1's exceptions, and
  reading K1 was not taken; part K is read again under the revision in "W2's after commit, records part 2, recorded on 2026-11-24" below. This reading stays as
  taken.)*
- **Reading K1, put to the owner and not taken here:** the rule's hits are "each listed member's declarations and
  references" (its configuration's words), so a line of prose or a comment that names a member, or another type's
  member of the same name, is none, and the narrowed scopes hide no declaration of a listed type's member
  (`scope.txt`); under it every hit of a listed member is a line of a facade file `dispositions.md` keeps, and part K
  would read MET. It is the session's, under the owner's direction of 2026-10-18; the owner may confirm it, take it as a
  dated revision worded on E-1's exceptions (weaker evidence, written after the run was seen; such a revision would
  also want the bridge's and the library's hits listed with their declarations, which `hits.tsv` does not give), or
  leave part K read NOT MET. *(Revised 2026-11-24, in review of this record before it merged: the reading bullet first
  read "Part K: read MET on the retained runs, by the session's reading K1", with reading every word hit as a hit "Not
  taken: ... part K reads NOT MET"; and the second bullet read "No listed implementation is found:" and "every hit in
  the facade files is a facade member `dispositions.md` keeps over the bridge, a comment, or a name of another type
  used there".)*

### Awaiting the owner

Parts A, D, J and K read NOT MET (part K with reading K1 put to the owner); each, with its cause as read, the rule's
options and a recommendation, is in the session's decision summary for the owner, sent to the owner with this
record's report on 2026-11-24 and not retained in this repository; the owner's choices are recorded here by dated
notes when made. Part I is pending on the owner's lane. No fix and no rule change is made here. *(Revised 2026-11-24,
in review of this record before it merged: this paragraph first read "Parts A, D and J read NOT MET, and part K rests
on reading K1; each, with its cause as read, the rule's options and a recommendation, is in the session's decision
summary for the owner.")*

## W2's after commit, records part 2, recorded on 2026-11-24

**Written:** 2026-11-24, on branch `sjl-14-after`, with the default branch's head read as
`83a9bd82a5d258d3001468095ebf43f9efc3e9a7` by `git ls-remote` before each of this record's commits (each read, with
its time, is kept in the session's notes). **What it records:** the owner's answers 12 to 15 of 2026-11-24; the rule's
dated revision they file; part J's corrected session at the after commit, which answer 14 orders; part K's listing
aid, which answer 15 asks for first; reading aids taken by others after the after runs, retained; quotes of the
section above corrected; and parts A, D, J and K read again. [`decision-rule.md`](decision-rule.md) and
[`decision-rule-revised-2026-11-23.md`](decision-rule-revised-2026-11-23.md) are not edited, no row is added to
[`corrections.md`](corrections.md) or its tables, and nothing retained in [`base/`](base/local/README.md) or in the
after runs' folders changes but [`after/owed-runs/README.md`](after/owed-runs/README.md), by a dated note, and the
`hashes.txt` of `after/owed-runs/` and of `after/part-k/`, which name the files added there; the first readings of the
section above stay on record, each with a dated note beside it that points here. Nothing is judged beyond the readings, and nothing is accepted (EX-30).

*(Noted 2026-11-24.)* The paragraph above says "the `hashes.txt` of `after/owed-runs/` and of `after/part-k/`, which
name the files added there"; `after/owed-runs/hashes.txt` also replaces its line for that folder's `README.md`, whose
digest the dated note there changes.

### The owner's answers 12 to 15 of 2026-11-24

The owner's own, not ones taken under the owner's direction of 2026-10-18, quoted verbatim:
- Answer 12. Asked (2026-11-24): "SJL-14 part A (test262 at W2's merge 83a9bd82) reads NOT MET on two rows, both correct
  behaviour after W2: (1) for-of/head-await-using-bound-names-in-stmt.js [original] fails at the base and passes after,
  with no K row explaining it; (2) the Terser variant of dynamic-import/.../new-covered-expression-is-valid.js passes at
  the base and is skipped after, because Terser's output is invalid JS that the new frontend correctly refuses. Your
  2026-11-23 revision admitted both for parts C and D only. No code fix applies. What should be done for part A?"
  Chosen: "Revise for 2 rows (Recommended)" — "Dated revision extending rev-for-of and rev-minifier to part A for
  exactly these two path-variant rows. Weaker evidence, since written after the run was seen." Not taken: "Leave NOT
  MET" — "Keep part A read NOT MET on record and move on (nothing is accepted under EX-30 anyway)."
- Answer 13. Asked (2026-11-24): "SJL-14 part D (parse corpus) reads NOT MET only because of the second dump: the tree
  after the compiler's for-loop pass, which part C never compared. For 4,364 input variants that tree differs only in
  span lines of nodes the pass builds or copies. Their parser-tree differences are all in admitted classes, and nothing
  differs outside spans. The pass is ForLoopPreparation.cs (SJL-12.5's, not W2's), so a fix there cannot change the
  reading at 83a9bd82. What should be done for part D?" Chosen: "Revise by list (Recommended)" — "Dated revision
  admitting a pass-tree dump difference for exactly the variants in after/part-d/pass/unclassified.tsv (by blob), whose
  parser-tree difference is admitted and whose differing lines are all spans. Weaker evidence." Not taken: "Leave NOT
  MET" — "Keep part D read NOT MET on record and move on."
- Answer 14. Asked (2026-11-24): "SJL-14 part J (fuzz session) reads NOT MET on two seeds. T037823 (56 findings): module
  seeds are compiled without a module record, so the compiler throws its documented import.meta ReferenceError; this is
  a defect in the session tool W2 added, not in the engine. V000002 (22 timeouts): I compiled the seed and mutants at
  the base commit as a reading aid. The seed takes about 15.5 s on both sides, past the 10 s bound, and 21 of 22 mutants
  are equally slow at the base. The 22nd is refused by the old parser at the base and accepted after. So the slowness is
  not W2's. What should be done for part J?" Chosen: "Fix tool + revise V2 (Recommended)" — "Commit a corrected session
  that gives module seeds a module record, run it at 83a9bd82 (no rule change; recorded as corrected after the findings
  were seen), and write a dated revision for V000002's compile-past-bound by name." Not taken: "Revise both seeds" —
  "One dated revision reading both seeds' findings, by name, as not findings; no re-run."; "Leave NOT MET" — "Keep part
  J read NOT MET on record and move on."
- Answer 15. Asked (2026-11-24): "SJL-14 part K (deletion search) reads NOT MET as written. Most of its 310 hits are
  changelog lines (e.g. CHANGELOG.md:949/951 naming W1's removed switch), comments, records, and other types' members
  that share a name. The rule's own model, sjl-11-001's E-1 search, needed explicit carve-outs for exactly these (it
  excludes CHANGELOG.md and other types' same-named members); part K has neither. Nothing listed is still implemented.
  What should be done for part K?" Chosen: "Revise like E-1 (Recommended)" — "Dated revision admitting changelog,
  comment and record lines and other types' same-named members, each listed with its declaration, plus the narrowed
  scopes as read in scope.txt. A listing aid runs first; nothing is re-run. Weaker evidence." Not taken: "Confirm
  reading K1" — "Accept the session's reading (prose, comments and other types' members aren't 'declarations and
  references' of a listed member), so part K reads MET with no revision."; "Leave NOT MET" — "Keep part K read NOT MET
  on record and move on."

### The rule's revision of 2026-11-24

[`decision-rule-revised-2026-11-24.md`](decision-rule-revised-2026-11-24.md), SHA-256 `d706070cda19ee77e80e21667625c831614008c1ee84e608b017b6159bb05936`, written after every
after run and its first reading were seen, and after the reading aids below and part J's corrected session, and
therefore weaker evidence, as the rule's section "Revisions" reads: "A rule revised after any run it judges has been
seen is a new dated file in this directory. It quotes this one whole and says it was written afterwards, and its
decision is then weaker evidence." It quotes the answers and the rule whole and the lines it changes, and changes only
these: part A admits `test/language/statements/for-of/head-await-using-bound-names-in-stmt.js` [original] by
rev-for-of and `test/language/expressions/dynamic-import/syntax/valid/new-covered-expression-is-valid.js` [terser] by
rev-minifier, each for that one path and variant, and its MET line reads with them (answer 12); part D admits, by
rev-pass, a difference of the dump after the compiler's for-loop pass of an input variant
[`after/part-d/pass/unclassified.tsv`](after/part-d/pass/unclassified.tsv) lists (blob
`c7339ab407b3c7a8aecb92b8faa221103f8bcf5a`), whose parser-tree difference is in an admitted class and whose every
differing pass-tree line is a span line, and its MET line and "How it is read" read with it (answer 13); part J's
reading takes a compile past the bound of the seed V000002
(`conformance/parse/inputs/broiler-vm/generated/refuse-more-locals-than-the-frame-admits.js`, blob
`12412cacaebf9c23753e3f2045227abf30a793c4`), or of a mutant of it the frontend accepts, as no finding (answer 14, its
second half); part K's MET line admits, as bundle `sjl-11-001`'s E-1 does, lines of the changelogs, code comments and
records, and hits listed with the declaration that shows them another type's member of the same name, and its
configuration takes the narrowed scopes as [`after/part-k/scope.txt`](after/part-k/scope.txt) reads them (blob
`61cb2509849e28829225ce077f50790c954408a4`) (answer 15). Answer 14's first half, the corrected session, needs no rule
change: the revision records in one sentence that it runs under the rule's unchanged words, corrected after the
findings were seen. **What it does not admit**, among others, as it says: the first session's T037823 findings, which
stay findings of that session; and the hits of part K that are the words of the bridge's message strings, which
answer 15's kinds do not name.

*(Noted 2026-11-24.)* Three points of this record and of the revision's account of itself are not exact. Each is left
as written and made exact here; the revision is not edited.
- **The revision has two versions.** It was committed first by `eb017a7fd9af67844bc48bfc4a2cabd6ba2f02a8` as blob
  `01a2ca527a7668811dfc81d1388e5fd8e711337d`. `a07031092ed4beef8937739eb60f2eb7f45d032e` ("SJL-14: the revision of
  2026-11-24 names part K's message-string hits by their lines alone") then replaced its entry "Part K's message
  strings", which read: "**Part K's message strings.** Seven hits that
  [`after/part-k/listing.tsv`](after/part-k/listing.tsv) lists are words of message strings of the bridge's
  `BridgeMessages.Text` (its lines 90, 100, 113 and 120 to 123); a message string is not a line of a changelog, a code
  comment or a record, and the word names no member of another type. Answer 15 names no such kind, and this file
  admits none: they are read as part K's MET line, as revised, reads them." The result is blob
  `13aa6dadcbfe34057be15e6b69b3027ee032c53c`, whose SHA-256 is the one named above; nothing else of the file changed.
  That commit's message says "Made before any reading under the revision is recorded"; that is not so. Between the
  two commits, `eda2e899dd6a47366a21a97981f178090389e448` retained
  [`after/part-j-corrected/findings.txt`](after/part-j-corrected/findings.txt) and its script `read_jc.py`, which
  read the corrected session under the revision's part J, as blob `01a2ca52` had it. That reading stands: the two
  blobs differ in that entry alone, and every line of part J (item 6 of "What changed" and the entry "Part J's first
  session's T037823 findings") is the same in both. The second readings below were written against blob `13aa6dad`.
- **The revision names part J's corrected session in four places, not one.** Its entry "Part J's first session's
  T037823 findings" says "This file names it only in this sentence."; and this section says above that "the revision
  records in one sentence that it runs under the rule's unchanged words". The revision also names the session in its
  "Written" paragraph ("and part J's corrected session at the after commit, which answer 14 orders and which this file
  does not change"), in "How it reads" ("Part J is read on its corrected session, as answer 14 orders.") and in "What
  it decides, and what it does not" ("and part J's corrected session had been seen"). Each of those three says what
  was seen before the file was written or which session part J is read on; none changes a line of the rule, and the
  revision's part J admits only the compiles past the bound of V000002 and of its accepted mutants.
- **When it was written, against the corrected session's runs.** The revision's first commit (06:23:17Z) came after
  the corrected session's step ended (06:14:59Z) and while its control was still running (06:15:41Z to 06:27:16Z,
  [`after/part-j-corrected/logs/runs.tsv`](after/part-j-corrected/logs/runs.tsv)); its one later change (06:28:24Z)
  came after the control ended. So the revision's "It is written **after every after run was seen and read**" and
  "and part J's corrected session had been seen", this section's "written after every after run and its first reading
  were seen, and after the reading aids below and part J's corrected session", and "Part J, read again"'s "the
  revision was written after both were seen" hold for the corrected session and not for its control. A run seen later
  takes nothing from the revision's evidence, and the revision leaves the control's template-literal findings as
  findings.

### Part J's corrected session, retained

The bundle's mutation session, [`tools/fuzz/`](tools/fuzz/README.md), is corrected by
`53afd382f2559d8c27cc42cbaf496af5d4ed161d` ("SJL-14: part J's session compiles module seeds with a module record
(answer 14)"), committed on its own: a seed whose goal is `module` now compiles with the parameters the engine's module
host compiles a module's body with, `import` and `#module` (`JSModuleContext`'s `ModuleParameters` at the after
commit), `#module` being the module record the compiler's module prologue and `import.meta` read; nothing else changes
(the tool's README, by a dated note). The session and its control were then taken at the after commit exactly as the
after driver's `fuzz` and `fcontrol` steps took the first, from a clean archive of `83a9bd82` with the corrected tool's
files written over their paths from their blobs, and are retained in
[`after/part-j-corrected/`](after/part-j-corrected/README.md), which names each blob, the one departure of order (the
control's clean comparison archive taken after the session, for the disk rule) and every file by its SHA-256
([`hashes.txt`](after/part-j-corrected/hashes.txt)). **Recorded as corrected after the findings were seen**, and so
weaker evidence: the tool was changed knowing what its first session found. The first session's records in
`after/local/fuzz/` and `after/local/fcontrol/`, and part J's first reading above, stay as they are.
[`after/part-j-corrected/findings.txt`](after/part-j-corrected/findings.txt), by
[`read_jc.py`](after/part-j-corrected/read_jc.py), reads both sessions:
- **The corrected session** compiles the same mutants as the first (the same seeds, indices, operators and mutant
  SHA-256s), and every process exits 0. The mutants of T037823 that were findings of the first session now compile,
  accepted; no other mutant's outcome moves. Its findings are V000002's alone, each a compile past the bound per
  compile, of the same mutants as the first session's, each of which
  [`after/owed-runs/jsingle/cases.tsv`](after/owed-runs/jsingle/cases.tsv) shows the frontend accepts when it is
  compiled alone.
- **Its control** compiles the same mutants. In the scratch copy whose bridge throws on a template literal it reports
  `System.InvalidOperationException` findings ("part J control: the bridge throws on a template literal") for the seeds
  that hold template literals, as the first control did, besides V000002's compiles past the bound; the revert is
  checked by digest and against a clean archive ([`revert-check.txt`](after/part-j-corrected/fcontrol/revert-check.txt)).
  Its T037823 mutants move as the session's do.

### Part K's listing aid

[`after/part-k/listing.tsv`](after/part-k/listing.tsv), by [`list_k.py`](after/part-k/list_k.py), lists every hit of
[`after/part-k/hits.tsv`](after/part-k/hits.tsv) that `hits.tsv` does not sort a facade member, each with the line at
the after commit that shows what it is: for a code line, the declaration of each occurrence of the hit's name (the
bridge's own methods, the library's syntax tree's types and members, the old tree's and the engine's members, the
library's own methods, the compiler's `FastCompiler` members `dispositions.md` keeps until SJL-15.5, and
`System.HashCode.Combine`, of the .NET base class library), every declaration checked against the tree; for a changelog
line, its entry's heading; for a record line, the member it names; for a comment, the line itself. A reading aid with
no run, taken after part K's first reading and before the revision; it reads git objects alone, and regenerates byte
for byte. It sorts the hits `hits.tsv` sorted OTHER in `FastScanner.cs` as the facade's own, `State` and its members,
which `dispositions.md`'s row for `FastScanner` keeps ("the public `FastScanner` keeps its facade members (...,
`State` and its members)"); and it finds hits that are no declaration or reference of any member: the words
"Class", "Decorators", "Function" and "Import" in message strings of the bridge's `BridgeMessages.Text`
(`Broiler.JS/Broiler.JavaScript.Parser/Bridge/BridgeMessages.cs`, lines 90, 100, 113 and 120 to 123), which
`hits.tsv` had sorted, by their folder, as the bridge's own members. [`after/part-k/hashes.txt`](after/part-k/hashes.txt)
names both new files.

### Reading aids taken after the after runs, retained

In [`after/owed-runs/`](after/owed-runs/README.md), by its dated note of 2026-11-24, each copied unchanged from the
session's notes:
- [`after/owed-runs/verifier-retake/`](after/owed-runs/verifier-retake/runs/logs/runs.tsv): the review of the section
  above took both commits' dump texts again from clean archives through the same `run.sh` and found their SHA-256
  `7343c06232cd45b2efa4b91d601fb3e512016c0344fd2992a357eefa3127c50d` (the base commit's) and
  `0db1abaf25b28b9224a2187aae79d87f3f40b7f4023acb4781ed5ba88afb65f4` (the after commit's), the digests part D's tables
  were read from; and it took `jsingle` again, with the same outcomes.
- [`after/owed-runs/jbase/`](after/owed-runs/jbase/README.md): the seed V000002 and its findings compiled one per
  process at the **base** commit with the single-case aid, bound and cap of `jsingle`, taken for the owner's question
  14: the seed and every mutant but one accepted and past the bound at the base as after; `V000002-47.js` refused at the base by
  the old parser ("Unexpected token Identifier: var at 40284, 1") and accepted after.

*(Noted 2026-11-24.)* The section above, part D, reads: "And the texts' own bytes were not shown reproducible:
`identity-with-partd.txt` and `identity-with-base-d1.txt` compare the dump reports' rows, with the `#` lines (among
them each assembly's identifier) left out, not the texts." The verifier's re-take shows them reproducible: a fresh build
and run at each commit gave texts with the digests part D's tables were read from. The sentence is left as written.

### Quotes of the section above, corrected

*(Noted 2026-11-24.)* These quotes of the section "W2's after commit, recorded on 2026-11-24" are not exact; each is
left as written and corrected here.
- "The after commit and the end of the hold" quotes the rule as "The base commit is its first parent"; the rule
  reads "**The base commit** is its first parent" (across two lines of its section "The commits, named before they
  exist"), which the quote gives without the bold markup.
- "Part D, read" quotes `after/part-d/README.md` as naming the dump texts "kept outside the repository, by digest";
  that README reads "kept outside the repository by digest", with no comma.
- "Part J, read" quotes `FastCompiler.VisitMeta.cs`'s documentation as "a deterministic ReferenceError naming that
  rather than a silent `undefined`"; the file reads "a deterministic ReferenceError naming that rather than a silent
  <c>undefined</c>" (across two lines of its `<remarks>`).

### Part A, read again

Under the rule and both revisions, on the same retained runs and comparison, with nothing re-run
([`after/second-reading/reading.txt`](after/second-reading/reading.txt), by
[`read_2.py`](after/second-reading/read_2.py), from `after/part-a/comparison/comparison.json` and
`classifications.json`):
- **Complete and comparable**, as the first reading reads them: every shard reported in both runs, one suite revision,
  one shell, every shard in the zone, `includeNegative` true in both; the host-zone records alike; the script finds the
  runs comparable.
- **Every changed row** but two is C1 by the classification file, with its path, its variant and its correction, and
  each such path is named in the changelog's section "The cut-over to the shared syntax library", as the first reading
  checked. **The two others** are the rows the revision names: `test/language/statements/for-of/head-await-using-bound-names-in-stmt.js`
  [original], failing at the base and passing after, in rev-for-of, listed with K7 and its row D35, the library's row
  table's note on EE0056, `JSE1417`, and finding S14-F2; and
  `test/language/expressions/dynamic-import/syntax/valid/new-covered-expression-is-valid.js` [terser], passing at the
  base and skipped after with `skipKind=minifier-invalid-output` ([`named-rows.log`](after/part-a/comparison/named-rows.log)),
  in rev-minifier. No row is C3 or T, and no other row is changed.
- **Part A: read MET on the retained runs**, under the revision of 2026-11-24 (weaker evidence; the files above and
  [`after/part-a/hashes.txt`](after/part-a/hashes.txt)). The first reading, "read NOT MET on the retained runs", stays
  on record. Not judged beyond that (EX-30).

### Part D, read again

Under the rule and both revisions, on the same retained tables ([`after/second-reading/reading.txt`](after/second-reading/reading.txt)):
- **Complete, and the control is reported**, as the first reading reads them.
- **Outcomes, messages and the parser trees' dumps:** every difference is in C1, C3, E, M, S, rev-code, rev-eof or
  rev-bracket and is listed, as the first reading found.
- **The dumps after the compiler's for-loop pass:** every difference is in C1, S, rev-eof or rev-bracket, or is one of
  the input variants [`after/part-d/pass/unclassified.tsv`](after/part-d/pass/unclassified.tsv) lists (its SHA-256 the
  one the revision names), each of which has a parser-tree difference in S or rev-eof and no outcome or message row
  outside part D's classes, and whose differing pass-tree lines are all span lines
  ([`pass/unclassified-lines.txt`](after/part-d/pass/unclassified-lines.txt): none is not a span line): rev-pass,
  listed there with its lines. That count was taken from `read_d.sh`'s work file `pass-spans.tsv`, kept by digest
  ([`after/part-d/work.sha256.txt`](after/part-d/work.sha256.txt)), written from the two dump texts whose bytes the
  verifier's re-take above reproduced.
- **Part D: read MET on the retained runs**, under the revision of 2026-11-24 (weaker evidence; the files above and
  [`after/part-d/hashes.txt`](after/part-d/hashes.txt)). The first reading stays on record. Not judged beyond that
  (EX-30).

### Part J, read again

On the corrected session ([`after/part-j-corrected/`](after/part-j-corrected/README.md)), under the rule's unchanged
configuration and the revision's part J:
- **The control is reported:** the control's session reports findings, `System.InvalidOperationException` for the
  seeds that hold template literals, which the revision leaves findings
  ([`findings.txt`](after/part-j-corrected/findings.txt)).
- **No finding is reported:** the corrected session's findings are compiles past the bound of mutants of the seed
  V000002 (`conformance/parse/inputs/broiler-vm/generated/refuse-more-locals-than-the-frame-admits.js`, blob
  `12412cacaebf9c23753e3f2045227abf30a793c4`), each a mutant the frontend accepts, which the revision's part J reads as
  no finding; there is no other exception type, no lost process and no compile past the bound of another seed.
- **Part J: read MET on the retained runs** of the corrected session, under the revision of 2026-11-24. It is weaker
  evidence twice over: the session was corrected after the first session's findings were seen, and the revision was
  written after both were seen (the files above; [`after/part-j-corrected/hashes.txt`](after/part-j-corrected/hashes.txt)).
  The first reading, on the first session, stays on record. Not judged beyond that (EX-30).

### Part K, read again

Under the rule and both revisions, on the same retained search and its tables ([`after/second-reading/reading.txt`](after/second-reading/reading.txt),
from `hits.tsv` and `listing.tsv`):
- **No listed implementation is found**, as the first reading reads it, and **the control is reported**.
- **The scopes:** the search's narrowed scopes are taken as the configuration's scope, as `scope.txt` reads them.
- **Every hit** is a facade member (the hits `hits.tsv` sorts so, and those of `FastScanner`'s `State` the listing
  sorts as the facade's own), a line of a changelog, a code comment or a record, or a hit listed with the declaration
  that shows it another type's member of the same name, **but the words** of the bridge's message strings in
  `BridgeMessages.cs` (lines 90, 100, 113 and 120 to 123: "Function declarations are not allowed as the body of a loop
  or with statement", "Class constructor may not be an accessor, a generator or an async method", "Import attribute has
  duplicate key '{0}'", "Unexpected token in the Function constructor's source", "... parameters", "... body", and
  "Decorators are not supported here"), each a hit of a listed member's name (`Function`, `Class`, `Import` and
  `Decorators`, members FastParser's partial files declared) that is neither a facade member, a relocation, a line of a
  changelog, comment or record, nor another type's member of the same name.
- **Part K: read NOT MET on the retained runs**, under the revision of 2026-11-24, for those message-string hits (the files
  above; [`after/part-k/hashes.txt`](after/part-k/hashes.txt)). The revision admits only the kinds answer 15 names,
  and the owner's question did not name message strings; they go to the owner. The first reading stays on record. Not
  judged beyond that (EX-30).

*(Noted 2026-11-24.)* The reading above counts among the hits "listed with the declaration that shows it another
type's member of the same name" one hit that has no declaration in the repository: `listing.tsv`'s row for `Combine`
at `Broiler.JS/Broiler.JavaScript.Parser/FastParserOptions.cs:112`, whose declaration column reads "- :: -" and whose
note reads "System.HashCode, of the .NET base class library: no declaration in the repository". It did not name that
hit. It is read as admitted with the .NET type named in place of a file and line, which the revision's "listed with the
declaration that shows it" does not say in so many words; this point goes to the owner beside the message strings, so
that a further revision can settle both. The reading of part K does not move: it is read NOT MET for the
message-string hits either way.

*(Noted 2026-11-24, later.)* The reading above reads "**Part K: read NOT MET on the retained runs**, under the revision
of 2026-11-24, for those message-string hits", and the note above says of `Combine` "this point goes to the owner
beside the message strings, so that a further revision can settle both". The owner's answer 16 of 2026-11-24 filed that
revision, [`decision-rule-revised-2026-11-24-k.md`](decision-rule-revised-2026-11-24-k.md), and part K is read a third
time under it in "W2's after commit, records part 3, recorded on 2026-11-24" below. This reading and the note above
stay as written.

### Awaiting the owner

Part K's message-string hits, read above, go to the owner; no rule change and no fix is made for them here.
Part I is pending on the owner's lane `win-x64-r7-5800x`, as the section above says.

*(Noted 2026-11-24, later.)* The paragraph above reads "Part K's message-string hits, read above, go to the owner; no
rule change and no fix is made for them here." The owner answered (answer 16 of 2026-11-24), and the rule change it
orders is recorded in "W2's after commit, records part 3, recorded on 2026-11-24" below; no fix is made. The paragraph
is left as written.

## W2's after commit, records part 3, recorded on 2026-11-24

**Written:** 2026-11-24, on branch `sjl-14-after`, with the default branch's head read as
`83a9bd82a5d258d3001468095ebf43f9efc3e9a7` by `git ls-remote` before each of this record's commits (each read, with
its time, is kept in the session's notes). **What it records:** the owner's answer 16 of 2026-11-24; part K's listing
aid, extended; the rule's dated revision answer 16 files; and part K read a third time.
[`decision-rule.md`](decision-rule.md), [`decision-rule-revised-2026-11-23.md`](decision-rule-revised-2026-11-23.md)
and [`decision-rule-revised-2026-11-24.md`](decision-rule-revised-2026-11-24.md) are not edited, no row is added to
[`corrections.md`](corrections.md) or its tables, and nothing retained in [`base/`](base/local/README.md) or in the
after runs' folders changes but [`after/part-k/hashes.txt`](after/part-k/hashes.txt), which names the two files added
there; [`after/second-reading/`](after/second-reading/README.md) is not changed. The first and second readings of part
K stay on record, each with a dated note beside it that points here. Nothing is judged beyond the reading, and nothing
is accepted (EX-30).

### The owner's answer 16 of 2026-11-24

The owner's own, not one taken under the owner's direction of 2026-10-18, quoted verbatim:
- Answer 16. Asked (2026-11-24): "SJL-14 part K still reads NOT MET after today's revision. Seven of its hits are
  words inside the bridge's error messages, not code: "Function", "Class", "Import" and "Decorators" in
  BridgeMessages.cs lines 90, 100, 113 and 120–123 (for example "Class constructor may not be an accessor, a
  generator or an async method"). Answer 15 admitted only changelog, comment and record lines and other types'
  members, so these hits are not covered. A second point would go in the same revision: one hit, `Combine`, belongs to
  .NET's System.HashCode, so the listing names that .NET type instead of a file and line. How should part K be
  settled?" Chosen: "Revise for msg text (Recommended)" — "One more dated revision, also weaker evidence because the
  runs were already seen. It admits words inside string literals, each listed with the message table that holds it,
  and lets a member of a .NET library type be shown by naming that type. Part K is then read again, likely MET. It
  goes on the same records branch before the PR." Not taken: "Leave K NOT MET" — "Change no rule. Part K stays NOT
  MET on record. I open the records PR as the branch stands; nothing is accepted either way (EX-30)."

### Part K's listing aid, extended

[`after/part-k/listing-2.tsv`](after/part-k/listing-2.tsv), by [`list_k2.py`](after/part-k/list_k2.py), beside
[`listing.tsv`](after/part-k/listing.tsv), which is not changed. `listing.tsv` already gave each message-string hit the
member whose table holds it, `BridgeMessages.Text`, with its declaration's file and line; the extension lists each such
hit again with the message identifier of its arm and its literal, and lists the hit `Combine` with its type and that
type's assembly, which `listing.tsv` did not name: `System.HashCode`, in the reference assembly `System.Runtime`. It
holds a row only when every check its header names held. **How the literals were checked to be message texts only**,
each from the after commit's git objects: every such hit line is one arm of the switch that `Text` is, a message
identifier mapped to a text, and the hit's word stands in the text's literal alone, never in the identifier the
switch looks up; `Text` is private, named outside comments only at its declaration and at one call, whose value is
the template `Render` fills and returns; `BridgeMessages.cs` names no reflection or lookup API (the script lists those
it looked for); outside that file, in part K's scope, `BridgeMessages` is named only in a documentation comment and in
the call of `BridgeMessages.Render` in `SharedSyntaxBridge.cs`; and each literal's text is found elsewhere in that
scope only on a test's assertion of a rendered message (`ImportAttributeEnforcementTests.cs`, for the import
attribute's text). **How the library hit was checked:** the line calls `HashCode.Combine` in a file that imports
`System`, no file of the tree declares a type `HashCode` or an alias of it, the project targets the framework of the
reference pack read, and that pack's `System.Runtime.xml` documents `System.HashCode` and a `Combine` taking as many
arguments as the call passes (the pack's version is in the table's header). The name's resolution is read from the
source and the reference pack; no compiled assembly was read. A reading aid with no run, taken after part K's second
reading and before the revision; it regenerates byte for byte (`python3 -I list_k2.py listing.tsv <repository>
<reference pack folder> listing-2.tsv`, run from `after/part-k/`). `after/part-k/hashes.txt` names both new files.

### The rule's revision of 2026-11-24-k

[`decision-rule-revised-2026-11-24-k.md`](decision-rule-revised-2026-11-24-k.md), SHA-256
`fc835b99b42ede25354afe369ce2b1c26f0dd6b716eaf8dd5cbab4dbb8693f98`, committed once, written after part K's after run,
its first and second readings, the revision of 2026-11-24 and part K's reading aids, the extension above among them,
had been seen (it names the branch's commit it was written at), and therefore weaker evidence, as the rule's section
"Revisions" reads: "A rule revised after any run it judges has been seen is a new dated file in this directory. It
quotes this one whole and says it was written afterwards, and its decision is then weaker evidence." It quotes answer
16 and the rule whole, and the lines of the revision of 2026-11-24 it changes, and changes part K alone: its MET line
also admits words inside a string literal that is a message text, listed with the message table that holds the
literal (the member and its declaration, its file and line at the after commit), where the string is no member name
used by reflection or lookup; and a member of a type outside the repository, of a .NET library, is shown by naming
that type and its assembly in place of a file and line. It says how the literals were checked, by the extension above,
and that the revision of 2026-11-24's entry on part K's message strings stays true of that file's words.

*(Noted 2026-11-24, after review.)* The revision's account of what was seen, and its quote of answer 16, are made
exact here; the revision is not edited.
- **Its statement of what was seen is its "Written" paragraph.** That paragraph says it was written "after every run,
  reading aid and reading that branch holds up to that commit was seen", and the list after its colon names part K's
  items only: the branch at that commit also held, and the session had seen, the after runs of the other parts and
  their readings, among them part J's corrected session and the second readings of parts A, D and J. The revision
  changes nothing of those parts and, outside its quote of the rule, names none of them. Two later passages restate
  parts of that paragraph and add nothing to it: "Why it is written" quotes this README's second reading of part K and
  its dated note on the hit `Combine`, and "What it decides, and what it does not" says it "was written after part K's
  after run, its first and second readings and the reading aids named at the top of this file had been seen", a
  shorter list than the paragraph's.
- **Answer 16's quote follows its rendered text.** The session's copy of the answer writes the quotation marks inside
  the question as escaped marks (`\"`); the revision, and this README's section "The owner's answer 16 of
  2026-11-24", write them as plain ones, which is how the copy renders. Apart from that and the line breaks, the
  quotes are the copy's text.

### Part K, read a third time

Under the rule and its three revisions, on the same retained search and its tables
([`after/third-reading/reading.txt`](after/third-reading/reading.txt), by
[`read_3.py`](after/third-reading/read_3.py), from `hits.tsv`, `listing.tsv` and `listing-2.tsv`, each checked by its
SHA-256, and the control's record):
- **No listed implementation is found**, as the first reading reads it, and **the control is reported**
  (`after/local/delsearch/control.txt`: the deleted member declared again is found, and the revert equals the commit's
  blob).
- **The scopes:** the search's narrowed scopes are taken as the configuration's scope, as `scope.txt` reads them (its
  SHA-256 the one the revision of 2026-11-24 names).
- **Every hit** is a facade member (the hits `hits.tsv` sorts so, and those of `FastScanner`'s `State` the listing
  sorts as the facade's own), a line of a changelog, a code comment or a record, a hit listed with the declaration
  that shows it another type's member of the same name, **the hit `Combine`** in `FastParserOptions.cs`, shown by its
  .NET library type `System.HashCode` and its assembly `System.Runtime` in place of a file and line, or **the words of
  the bridge's message strings** in `BridgeMessages.cs` (lines 90, 100, 113 and 120 to 123), each listed with its
  message table `BridgeMessages.Text` and its declaration, each literal a message text and no member name used by
  reflection or lookup. `read_3.py` reads no hit as admitted by none of these.
- **Part K: read MET on the retained runs**, under the revision of 2026-11-24-k with the revisions of 2026-11-23 and
  2026-11-24 (weaker evidence: written after the runs and the first and second readings were seen; the files above,
  [`after/part-k/hashes.txt`](after/part-k/hashes.txt) and
  [`after/third-reading/hashes.txt`](after/third-reading/hashes.txt)). The first reading (NOT MET as the rule is
  written) and the second (NOT MET under the revision of 2026-11-24, for the message-string hits) stay on record. Not
  judged beyond that (EX-30).

### Awaiting the owner

Part I is pending on the owner's lane `win-x64-r7-5800x`, as the section "W2's after commit, recorded on 2026-11-24"
says.

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
