# Bundle SJL-14-001's decision rule: Broiler.JS on the shared syntax library, judged by test262 per path, by the bridge's identity with the switch off, by the side-by-side run, by the parse corpus across the cut-over, by the portable compiler and its Native AOT sample, by the packages, by the downstream consumers, by the public API, by its parse measurement, by fuzzing and by the deletion search

**Written:** 2026-11-03, at milestone SJL-14 (work package SJL-14.1) of the shared JavaScript libraries programme,
with this repository's `docs/roadmap/SharedLibraries.md` section "Broiler.JS on the shared syntax library (SJL-14)" and
Broiler.VM's JSD-0083, before any change it judges exists: no bridge, no switch, no adapter package and
no deletion. Committed in the same change: [`corrections.md`](corrections.md), [`dispositions.md`](dispositions.md)
and [`inputs/`](inputs/README.md) (part I's workloads, part J's seeds). **Milestone judged:** SJL-14,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-14-broiler-js-syntax.md`: its Broiler.JS clauses 1 to 5,
clauses 2 and 5 as revised on 2026-11-03 before any evidence (decisions D50 and D49), and clause 6 ("Broiler.JS's
parse measurement meets its rule.") as the milestone file states it when part I is read: what clause 6 becomes while
no controlled lane exists is the owner's own choice (decision D48), and this file does not make it. **Owner:** the Broiler.JS maintainer. **Reviewer:** none. Every role this rule names is
held by one person (the programme records this as Broiler.VM's exception EX-30), and this rule does not claim that
anyone independent has read it.

**What this file is.** Eleven predeclared decisions, one per part. Part A is template T2 and part I template T5. Part
B is an identity, part C a side-by-side comparison, part D the parse corpus across the cut-over with bundle
`sjl-12-001`'s tool, part E layer L10, part F layer L11, part G layer L8, part H layer L12, part J layer L9 and part K
the deletion search. Each part decides alone. It holds no figure. The bundle has a `manifest.json` (decision D56).

**Bundle `sjl-12-001`'s tool** (parts B and D) is used byte-unchanged. From the after commit it builds with the
obsolete marks' diagnostic identifier suppressed by the `NoWarn` of the repository root's `Directory.Build.props`, which
it inherits (decision D50), and the `FastParser` facade it calls throws today's exception types, `FastParseException`
for a refusal. A build of the tool that fails at any commit makes the part that needs it void, not MET.

**The corrections this rule classifies by** are [`corrections.md`](corrections.md)'s K1 to K10, each with its rows of
Broiler.VM's `docs/evidence/sjl-13-001/correspondence.md`, or of this bundle's inherited-context probes
([`inherited-context/`](inherited-context/README.md), rows IC1 to IC7 of K10), and its test262 paths, listed in `Broiler.JS/CHANGELOG.md` by
name at the cut-over (decision D45). **JSD-0080 decision D27's listed refusals** are not corrections: they are an
implementation limit, class C3, enumerated by differencing the corpus outcomes on the 256 MiB thread the SJL-12
baselines used (decision D47).

**The stage and timing classes** (decisions D45 and D64), named before any run with their rows in
[`corrections.md`](corrections.md):
- **E, early-error stage** - an input accepted at entry point P (the parser alone) at the side the base stands for,
  and refused at P or B on the other side with a SyntaxError whose shared code is a `syntax` row of the stage table,
  K refusing on both sides, where either the row's catalogue `js_stage` names only compiler sites
  (`SyntaxValidation`, `FastCompiler.EarlyErrors`, `Visit*`, `DirectEvalSupport`; [`corrections/class-e.tsv`](corrections/class-e.tsv)),
  or bundle `sjl-13-001`'s classification expects the input refused in configuration J with that row among the rows
  whose codes may come first (a row this engine checks partly in its parser and partly in its compiler,
  [`corrections/partly.tsv`](corrections/partly.tsv), or a parser row its parser missed for that input; decision D45 as
  revised on 2026-11-03, finding S14-F1 in [`corrections.md`](corrections.md)); listed with its row. The library
  checks at the parse what this engine checked only in its compiler.
- **T, timing** - for those of the E rows whose check ran lazily or after other code ran (a `Visit*` check at a
  function's first call), an input or test262 row whose SyntaxError this engine raised later than the specification's
  early-error point, or not at all when the function was never called, and raises before evaluation after; listed with
  its row.

**The product paths** (decision D42): `Broiler.JS/` except `Broiler.JS/benchmarks/` and `Broiler.JS/CHANGELOG.md` -
which includes `Broiler.JS/Broiler.JavaScript/`, the engine's main project, whose folder has no suffix -
`Broiler.ECMAScript/` outside `Broiler.ECMAScript/docs/`, and the root's `Directory.Packages.props`,
`Directory.Build.props`, `Directory.Build.targets`, `global.json` and `NuGet.config`.

**The commits, named before they exist.**
- **The rule commit** carries this file.
- **The bridge commit (W1)** is the default-branch merge that lands SJL-14.2's bridge with the switch off by default,
  one merge. **Its base** is its first parent, which descends from the rule commit; W1 is the only first-parent commit
  that carries any part of the bridge or the switch.
- **The after commit (W2)** is the default-branch merge that lands SJL-14.3 to SJL-14.5, one merge. **The base commit**
  is its first parent, which descends from W1 and from the rule commit; W2 is the only first-parent commit that
  carries any part of SJL-14.3 to SJL-14.5 (the switch's removal, the adapter packages, the portable compiler's and the
  module analysis's move, the deletions).
- If either window had to land in more than one merge, each is named in this bundle, its base is the first one's first
  parent, and its after runs are taken at the last one.
- **A dark commit** is any default-branch merge between W1 and the base commit that changes only
  `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*/` (decision D52). Each carries part B's identity check with its own
  first parent, retained in this bundle; its switch-on outputs are retained and read, and part C, taken at the base
  commit, judges them.
- Each run names its tested commit; hosted runs are dispatched at any time after their commits exist.

A part whose base was taken at any other commit, or whose commits fail those conditions, is void.

## Part A - test262 parity per path, in UTC, with negative tests (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e - every path the runner's --all-script-host-verifiable selection admits with --include-negative, in both variants the canonical configuration runs, original and terser
configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, by .github/workflows/test262-evidence.yml as the base commit carries it and unchanged at the after commit, dispatched with the tested commit's forty-digit identifier, suite_ref at the revision above, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone empty and include_negative true: eight shards merged by scripts/compliance/merge_test262_shards.py, --minifier terser at the version its lock file pins, no memory limit, negative-metadata tests included and their expected errors verified, a --self-check before each shard, every shard running the same shell, and every shard in UTC, shown by its host-zone record; both runs' host-zone records read alike; the base run at the base commit and the after run at the after commit, on the same RID; each artifact imported unchanged, with the zip's digest
comparison: per path and variant, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, the classification file retained beside its report
timeouts: a row that timed out in either run is the one the workflow ran again alone, with one worker; the script takes the re-run through --base-rerun or --after-rerun, and both are retained
admitted difference classes:
  C1 correction - a row failing at the base that passes after, because the shared frontend follows the pinned text where this engine did not: each row one of corrections K1 to K10 explains; listed with its path, its variant and its correction, and in the changelog (decision SJL-D-16); a C1 row that none explains is recorded as a finding against the correspondence and is a regression
  C3 implementation limit (JSD-0080 decision D27) - a row passing at the base and failing after because its source nests past the bound the frontend applies for this engine, refused with a SyntaxError for nesting; listed with its path, its variant and the depth the frontend reports; expected empty
  T timing (decision D64) - a negative row failing at the base that passes after because the SyntaxError of a class T row of corrections.md, which this engine raised late or not at all, is raised before evaluation; listed with its path, its variant and its row
no C5: no data, pin or policy moves
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after outside C3
MET if and only if both runs are complete (every expected shard reported, one suite revision, one shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, the script finds them comparable, and every changed verdict is in C1, C3 or T and is listed.
NOT MET otherwise; and if the two runs' host-zone records differ, the part is not judged until the base is taken again at the same commit in the after run's zone record.
```

## Part B - the bridge's identity with the switch off (W1, and each dark commit)

```rule
evidence class: identity (not a measurement)
population: every input and variant of conformance/parse/corpus.manifest
configuration: bundle sjl-12-001's tool docs/evidence/sjl-12-001/tools/parse-corpus, byte-unchanged and built as this file's header says, in Release against each commit's projects, on one thread with a 256 MB stack, no LANG and TZ=UTC: entry points P and K, and the --dump mode (before and after the compiler's for-loop pass), at W1's first parent and at W1, the switch at its default; the same at each dark commit and its first parent; and every test project of Broiler.JS.slnx at W1 by its CI run; at each dark commit and its first parent, the side-by-side tool's entry points B and K with the switch on are also run, retained and read, not judged here (part C judges the switch-on outputs at the base commit)
comparison: per input, variant and entry point, the outcome, the exception type and the message; per input, the dumps' hashes
admitted difference classes: none
MET if and only if, at W1 and at every dark commit, both runs are complete and identical in every field, and W1's CI run of every test project passed.
NOT MET otherwise.
```

## Part C - the side-by-side run (SJL-14.2)

```rule
evidence class: conformance parity (not a measurement)
population: every input and variant of the parse corpus; every test of Broiler.JS's test projects; the whole pinned suite as part A selects it
configuration: at the base commit (W2's first parent, the last state before the cut-over, every dark commit included), one build in Release: the side-by-side tool docs/evidence/sjl-14-001/tools/side-by-side at entry points P (the old parser), B (the bridge) and K (the whole compile, the switch off and on), with the canonical dump of bundle sjl-12-001's tool applied to P's and B's trees; every test project with the switch on through its internal options member; scripts/compliance/run_test262.py locally on the programme's machine through the shell built at that commit with its hidden switch off and on, in UTC, with negative tests, one worker per core; the same runs at W1, retained and read, not judged
comparison: per input and variant, P against B and K-off against K-on: outcome, exception type, message and dump hash; per test, its result off and on; per test262 path and variant, its verdict off and on
admitted difference classes: part A's C1 (by K1 to K10), C3 and T; E, by this file's header; and M - a refusal with the same exception type at the same entry point whose message differs, each listed in the bundle's generated message table with both texts (decision D46); and S - a dump difference only in a node's span, where P's span disagrees with the node's source text and B's agrees, listed with both spans checked against the text
every other difference is unclassified
MET if and only if every run at the base commit is complete, and every difference is in C1, C3, E, T, M or S and is listed.
NOT MET otherwise.
```

**How it is read.** This is SJL-14.2's done-when, "the side-by-side run is retained with every difference
classified", judged where it last matters: at the base commit, after every dark merge. The run at W1 is retained so
that a library fix found there can be traced to its dark merge. An unclassified difference is a bridge or library
defect, fixed as a dark merge and run again before W2 merges.

## Part D - the parse corpus across the cut-over

```rule
evidence class: conformance parity (not a measurement)
population: part B's
configuration: bundle sjl-12-001's tool, byte-unchanged and built as this file's header says, at the base commit and at the after commit, entry points P (at the after commit, the adapter Parser package's FastParser facade, which keeps every member the tool names and throws today's exception types; decision D50) and K, and the --dump mode
completeness: every input and variant at both entry points at both commits; the manifest's digest equal
controls: a copy of the base report with one outcome flipped and one message changed, which the comparison must report as two differences; retained
comparison: scripts/compliance/compare_parse_corpus.py, per input, variant and entry point: outcome, exception type and message; per input, the dump hashes
admitted difference classes: part C's C1, C3, E, T, M and S
every other difference is a regression
MET if and only if both runs are complete, the control is reported, and every difference is in C1, C3, E, T, M or S and is listed.
NOT MET otherwise.
```

**How it is read.** This is SJL-14.1's "old-tree-against-new-tree comparison over the parse corpus". The dump reads
non-public fields of every `Broiler.*` type, so a field the bridge carries differently shows; class S is the only tree
difference admitted outside the corrections.

## Part E - the portable compiler and its Native AOT sample (clause 4; layer L10)

```rule
evidence class: publish and run (not a measurement)
population: Broiler.JS/Broiler.JavaScript.Portable.Tests, which holds Broiler.JavaScript.Portable.Compiler's tests (it has no test project of its own), including the tests the after commit adds for the early errors the portable compiler now reports; the sample Broiler.JS/samples/Broiler.JavaScript.NativeAotSample
configuration: dotnet test of that project in Release at the after commit; scripts/performance/collect_phase5.py, run locally on linux-x64 at the base commit and at the after commit - the portable compiler run on fibonacci.js with Program.cs's arguments, its output compared byte for byte with the checked-in FibonacciProgram.g.cs, the sample published with Native AOT and run; and a closure listing of Broiler.JavaScript.Portable.Compiler at the after commit (its transitive project and package references, and the assemblies its publish output holds)
MET if and only if every test passes at the after commit; at the after commit the regenerated program is identical to the checked-in copy, the sample publishes with Native AOT, runs and answers as the collector requires, with no dynamic code; and the closure holds no Broiler.JavaScript.Runtime and no Broiler.JavaScript.Storage.
NOT MET otherwise.
```

## Part F - the packages (clause 5's first half; layer L11)

```rule
evidence class: packages (not a comparison)
population: every package eng/pack.ps1 packs at the after commit, Broiler.ECMAScript.Frontend among them
configuration: eng/pack.ps1 and eng/verify-feed.ps1 at the after commit into a local feed; scripts/packaging/test_pristine_consumer.py against that feed; a metadata reader, committed to the bundle, listing the [Obsolete] attribute, its diagnostic identifier and its message on every public type of Broiler.JavaScript.Ast and Broiler.JavaScript.Parser
controls: the pristine consumer restored with Broiler.ECMAScript.Frontend removed from the feed, which must fail with the missing-package error
MET if and only if packing and feed verification pass, the pristine consumer passes, the control fails as stated, and every public type dispositions.md marks obsolete carries the mark with the removal version decision D50 names.
NOT MET otherwise.
```

## Part G - the downstream consumers (clause 5's second half; layer L8)

```rule
evidence class: consumer parity (not a measurement)
population: Broiler.JSeal's test suites and Broiler.HtmlBridge's, at the commits the bundle names, read before the base commit exists
configuration: two candidate feeds, packed by eng/pack.ps1 at the base commit and at the after commit; Broiler.JSeal's eng/test-package-consumer.ps1 with -CandidateFeed and -CandidateVersion for each, its pins unchanged; Broiler.HtmlBridge in a scratch copy whose package source and Broiler.JavaScript versions are overridden for each candidate, never committed; every test's result retained
comparison: per test, its result against the base candidate and against the after candidate
admitted difference classes: none
MET if and only if both suites ran against both candidates, and every test that passes against the base candidate passes against the after candidate (decision D49).
NOT MET otherwise; and the part is not judged while either suite has not been run against both candidates.
```

**How it is read.** Clause 5, as revised under update rule 5 before any evidence (decision D49), asks that the
downstream candidate-feed runs lose no test to the cut-over. A test that fails against both candidates is recorded with its cause; the
JSeal module adapter's reflection on members SJL-12.5 removed is such a cause, and a finding of SJL-12.5's.

## Part H - the public API (layer L12)

```rule
evidence class: API baseline (not a comparison of runs)
population: every public type and member of every package eng/pack.ps1 packs
configuration: the API lister docs/evidence/sjl-0-001/api/api-lister.csproj at the base commit and at the after commit
comparison: per member, present at the base, at the after, or at both with a changed signature or attribute
MET if and only if every member removed, changed or newly obsolete has a row of docs/public-api.md at the after commit with its SJL-D-25 disposition - (a) forwarder, (b) obsolete with the removal version, or (c) removal with a recorded consumer check - and dispositions.md agrees with it.
NOT MET otherwise.
```

## Part I - Broiler.JS's parse measurement (clause 6; template T5)

```rule
evidence class: measurement
population: the workloads of docs/evidence/sjl-14-001/inputs/workloads as committed with this rule, the real corpora of Broiler.JS/benchmarks' CompilePhaseMetrics; quantity: the time from source text to the tree the compiler reads, measured on the calling thread (at the base: FastParser.ParseProgram; after: the hop to the parse thread, the frontend's parse, the bridge and the return; decision D47)
arms: B the base commit's build; A the after commit's; A-again, the A/A lane
configuration: the harness Broiler.JS/docs/roadmap/Measurement.md names, on the lane the bundle names with its effective configuration; interleaved arms; repetitions and warm-up as Measurement.md fixes them; each lane's condition, that every workload parses to the outcome the base gives, checked before and after
quantity, noise floor and comparison: Q the median per workload and arm; N = |Q(A) - Q(A-again)|; D = Q(A) - Q(B) against N
lane: the part is judged only on a controlled lane as Measurement.md requires; on a lane that is not controlled the part is void - neither MET nor NOT MET - and its runs are retained and recorded as void
MET if and only if the lane is controlled, every condition passes, and for every workload D <= N.
NOT MET if and only if the lane is controlled and a condition fails or some workload has D > N.
```

**What part I decides.** Clause 6 ("Broiler.JS's parse measurement meets its rule.") is read from this part as the
milestone file states the clause when the part is read: as written, the clause is met only by a MET on a controlled
lane, and a void part leaves it unmet; what the clause becomes while no controlled lane exists is the owner's own
choice (decision D48), which this file does not make. No speed claim.

## Part J - fuzzing (layer L9)

```rule
evidence class: robustness (not a measurement)
population: the seeds docs/evidence/sjl-14-001/inputs/fuzz-seeds.txt names, each with its goal, its count of mutants and the time bound per compile
configuration: a seeded mutation session, committed to the bundle, compiling each mutant through CoreScript.Compile at the after commit in Release
reading: each mutant is accepted, or refused with a SyntaxError; a finding is any other exception type, a lost process or a compile past the bound
controls: a scratch copy in which the bridge throws on one node kind the seeds produce; the session must report a finding; reverted
MET if and only if no finding is reported and the control is reported.
NOT MET otherwise.
```

## Part K - the deletion search (clause 2)

```rule
evidence class: code fact (not a comparison)
population: SJL-14.5's deletion list as clause 2 reads it after its revision of 2026-11-03 (decision D50), member by member as dispositions.md lists it: FastParser's partial files, FastScanner's and FastTokenStream's scanning code, FastKeywordMap's table, CharExtensions' scanner members, the parser's half of FastScope, RegExpValidator, DecodeBracedUnicodeEscapes and the parenthesis-balance patch (each deleted, or relocated behind the provider as decision D51 allows and dispositions.md records), the division fallback, the two token re-scans and the direct-eval prefix decision D41 deletes at the cut-over (SyntaxValidation.ContainsLegacyOctalLiteral, DirectEvalSupport.ContainsStrictReservedWordUsage, and DirectEvalSupport's "use strict" prefix with the two places that skip it), and the test-only switch
configuration: the deletion search script, committed to the bundle with the after commit's change, as bundle sjl-11-001's part E runs its E-1 search: at the after commit, over Broiler.JS/ and Broiler.ECMAScript/ outside docs/, each listed member's declarations and references, every hit listed with its file and line
controls: a scratch copy of the after commit in which one deleted member is declared again; the search must report it; reverted, and the revert checked
MET if and only if no listed implementation is found, every hit is a facade member dispositions.md lists as an obsolete forwarder over the bridge or a relocation decision D51 allows, and the control is reported.
NOT MET otherwise.
```

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory. It quotes this one whole
and says it was written afterwards, and its decision is then weaker evidence.

## What this file does not decide

- **Clause 1's code fact** ("parse with the shared frontend"), read from the tree at the after commit, which the bundle
  retains; clause 2 is part K's.
- **The library**, which the home's bundle `sjl-13-001` judges.
- **Broiler.VM's clauses.**
- **A speed claim of any kind.**
