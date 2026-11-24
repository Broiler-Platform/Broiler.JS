# Bundle SJL-14-001's decision rule, revised again after the after runs were seen: part K admits the words of a message string listed with its message table, and a member of a .NET library type shown by that type and its assembly, as the owner's answer 16 of 2026-11-24 names, and nothing wider

**Written:** 2026-11-24, at milestone SJL-14, after W2 merged as the after commit
`83a9bd82a5d258d3001468095ebf43f9efc3e9a7` (its first parent the base commit
`69a8a13bc446a1714d3c8be9be3cfd742aebee6e`), on the records branch `sjl-14-after` at
`0a44a3151c2ef9b205fc1e9c34d62f4ed95d63ba`, with the default branch's head read as the after commit. It is written
**after every run, reading aid and reading that branch holds up to that commit was seen**: part K's after run
(`after/local/delsearch/`) and its first reading, NOT MET as the rule is written (the bundle README's section "W2's
after commit, recorded on 2026-11-24"); part K's reading aids [`after/part-k/hits.tsv`](after/part-k/hits.tsv),
[`after/part-k/scope.txt`](after/part-k/scope.txt) and [`after/part-k/listing.tsv`](after/part-k/listing.tsv); the
revision [`decision-rule-revised-2026-11-24.md`](decision-rule-revised-2026-11-24.md) and part K's second reading
under it, NOT MET for the hits that are words of the bridge's message strings, with the README's dated note on the
hit `Combine` (section "W2's after commit, records part 2, recorded on 2026-11-24", and
[`after/second-reading/reading.txt`](after/second-reading/reading.txt)); and the listing aid's extension,
[`after/part-k/listing-2.tsv`](after/part-k/listing-2.tsv) by [`list_k2.py`](after/part-k/list_k2.py), committed by
that commit before this file was written. **Owner:** the Broiler.JS maintainer. **Reviewer:** none; every role is one
person (the programme's EX-30), and this revision does not claim that anyone independent has read it.

**Whose decision.** This revision is filed by the owner's answer 16 of 2026-11-24, an answer of the owner's own, not
one taken under the owner's direction of 2026-10-18, quoted verbatim.

Answer 16. Asked (2026-11-24): "SJL-14 part K still reads NOT MET after today's revision. Seven of its hits are words
inside the bridge's error messages, not code: "Function", "Class", "Import" and "Decorators" in
BridgeMessages.cs lines 90, 100, 113 and 120–123 (for example "Class constructor may not be an accessor, a generator
or an async method"). Answer 15 admitted only changelog, comment and record lines and other types' members, so these
hits are not covered. A second point would go in the same revision: one hit, `Combine`, belongs to .NET's
System.HashCode, so the listing names that .NET type instead of a file and line. How should part K be settled?"
Chosen: "Revise for msg text (Recommended)" — "One more dated revision, also weaker evidence because the runs were
already seen. It admits words inside string literals, each listed with the message table that holds it, and lets a
member of a .NET library type be shown by naming that type. Part K is then read again, likely MET. It goes on the
same records branch before the PR." Not taken: "Leave K NOT MET" — "Change no rule. Part K stays NOT MET on record. I
open the records PR as the branch stands; nothing is accepted either way (EX-30)."

This file is that dated revision. It changes no rule of Broiler.VM's or of any other bundle.

**The rule revised** is [`decision-rule.md`](decision-rule.md) as blob `e0e954631f798d38ca67d0b808c53c3d3c38f44e`, the
file the base commit and the after commit hold, as revised by
[`decision-rule-revised-2026-11-23.md`](decision-rule-revised-2026-11-23.md), blob
`a120407e907b425905ef2f02061008d4b50f3122` (SHA-256 `1f9c7e539716b9d498c45592ca057ecb65e6543dcd613dfc09d2d4a6ba6b921e`),
and by [`decision-rule-revised-2026-11-24.md`](decision-rule-revised-2026-11-24.md), blob
`13aa6dadcbfe34057be15e6b69b3027ee032c53c` (SHA-256 `d706070cda19ee77e80e21667625c831614008c1ee84e608b017b6159bb05936`).
Both revisions stand, and this file changes the revision of 2026-11-24 only where its items below say. The rule is
quoted whole at the end of this file, as both revisions quote it (each line prefixed with "> ", a blank line a bare
">"), and no earlier file is edited, as the rule's section "Revisions" requires. **Its name** is the rule's file name
with `-revised-`, the date and `-k`, the part it revises, since a revision of this date is already filed; the name
decides nothing.

## Why it is written

Part K's MET line, as the revision of 2026-11-24 has it (its item 7, "now reads"), asks that:

```rule
every hit is a facade member dispositions.md lists as an obsolete forwarder over the bridge, a relocation decision D51 allows, a line of a changelog, a code comment or a record (a Markdown file, a JSON manifest or a PublicAPI.txt list), or a hit the bundle lists with the declaration that shows it is another type's member of the same name; each such line and hit listed with the declaration that shows it
```

and that revision's "What it does not admit" reads, in its entry on part K's message strings: "The hits that
[`after/part-k/listing.tsv`](after/part-k/listing.tsv) lists as words of message strings of the bridge's
`BridgeMessages.Text` (its lines 90, 100, 113 and 120 to 123) are not admitted: a message string is not a line of a
changelog, a code comment or a record, and the word names no member of another type."

The bundle README's section "W2's after commit, records part 2, recorded on 2026-11-24" reads, under "Part K, read
again": "**Part K: read NOT MET on the retained runs**, under the revision of 2026-11-24, for those message-string
hits"; and its dated note beside that reading says of the hit `Combine`, whose listed declaration is no line of the
repository but the type `System.HashCode`: "It is read as admitted with the .NET type named in place of a file and
line, which the revision's "listed with the declaration that shows it" does not say in so many words; this point goes
to the owner beside the message strings, so that a further revision can settle both."

## What was checked before it was written

[`after/part-k/listing-2.tsv`](after/part-k/listing-2.tsv), blob `e3ee792da9c76afe70c9c14abffb4eed35607ca4` (SHA-256
`89f60c40f0f274b5a059635de546482ca08ceecaa77b6416e8ce22183ea02334`), by [`list_k2.py`](after/part-k/list_k2.py), lists
each hit that `listing.tsv` gives as words of a message string with the message table that holds its literal (the
member `BridgeMessages.Text` and its declaration, its file and line at the after commit, and the message identifier of
the arm), and each hit that `listing.tsv` gives as a member of a type with no declaration in the repository with that
type and its assembly. It holds a row only when every check its header names held, each read from the after commit's
git objects, and the last from .NET's reference pack:
- **That the literals are message texts only.** Each such hit line is one arm of the switch that `Text` is, a message
  identifier mapped to a text, and the hit's word stands in the text's literal, never in the identifier the switch
  looks up; `Text` is private, and its value is used once, as the template the message is rendered from;
  `BridgeMessages.cs` names no reflection or lookup API; outside that file, in part K's scope, the type is named only
  in documentation comments and in calls of its `Render`; and each literal's text is found elsewhere in that scope only
  on a line that asserts a rendered message. So no such literal is a member name used by reflection, or a key looked
  up by its text.
- **That the library hit is the .NET type's member.** The hit is a call of `HashCode.Combine` in
  `Broiler.JS/Broiler.JavaScript.Parser/FastParserOptions.cs`, which imports the namespace `System`; no file of the tree
  declares a type `HashCode` or an alias of it; the project targets the framework of the reference pack read, whose
  reference assembly `System.Runtime` documents the type `System.HashCode` and a `Combine` taking as many arguments as
  the call passes.

## What changed

1. **Part K's MET line.** The words of the revision of 2026-11-24 quoted above, "every hit is a facade member
   dispositions.md lists as an obsolete forwarder over the bridge, a relocation decision D51 allows, a line of a
   changelog, a code comment or a record (a Markdown file, a JSON manifest or a PublicAPI.txt list), or a hit the
   bundle lists with the declaration that shows it is another type's member of the same name; each such line and hit
   listed with the declaration that shows it", now read "every hit is a facade member dispositions.md lists as an
   obsolete forwarder over the bridge, a relocation decision D51 allows, a line of a changelog, a code comment or a
   record (a Markdown file, a JSON manifest or a PublicAPI.txt list), words inside a string literal that is a message
   text, or a hit the bundle lists with the declaration that shows it is another type's member of the same name; each
   such line and hit listed with the declaration that shows it, where words inside a string literal are listed with
   the message table that holds the literal (the member and its declaration, its file and line at the after commit)
   and the string is no member name used by reflection or lookup, and a member of a type outside the repository, of a
   .NET library, is shown by naming that type and its assembly in place of a file and line". "no listed
   implementation is found" and "the control is reported" stand, and so does the NOT MET line.
2. **The entry on part K's message strings.** The revision of 2026-11-24's sentence quoted above, "The hits that
   [`after/part-k/listing.tsv`](after/part-k/listing.tsv) lists as words of message strings of the bridge's
   `BridgeMessages.Text` (its lines 90, 100, 113 and 120 to 123) are not admitted", stays true of that file's words;
   read with this file, those hits are admitted when they are listed as item 1 asks, and are read as item 1 reads them.
   Its reasons ("a message string is not a line of a changelog, a code comment or a record, and the word names no
   member of another type") stand: item 1 adds a kind, and moves no hit into those kinds.
3. **Nothing else changes**: no other line of the rule, or of the revisions of 2026-11-23 and 2026-11-24.

## What it does not admit

- **Words inside a string literal that is not a message text**: a literal that names a member for reflection (as
  the argument of a `GetMethod` or the like), a key looked up by its text, or any literal the listing does not list with
  the message table that holds it.
- **A member of a type outside the repository that is not shown** by naming that type and its assembly, and a member
  of a type outside the repository that is not a .NET library's.
- **Any other kind of hit**, which is read as the rule and its revisions read it.

## How it reads

- **Kinds, each listed.** Item 1 adds one kind of hit, the words of a message string, each listed with its message
  table, and one way of showing a declaration, a .NET library type and its assembly for a member that has no
  declaration in the repository. The listing is [`after/part-k/listing.tsv`](after/part-k/listing.tsv) with its
  extension [`after/part-k/listing-2.tsv`](after/part-k/listing-2.tsv), named above.
- **The runs are not taken again**: part K is read again on the search already retained, and its first and second
  readings stay on record in the README.

## What it decides, and what it does not

- **It decides** that part K's MET line admits the words of a string literal that is a message text, listed with the
  message table that holds it, and lets a member of a .NET library type be shown by naming that type and its
  assembly in place of a file and line.
- **It does not change** any other line of the rule or of the revisions of 2026-11-23 and 2026-11-24. It widens no
  tolerance and no class the rule names, and adds no row to the corrections.
- **It is weaker evidence** in the sense of the rule's section "Revisions": "A rule revised after any run it judges has
  been seen is a new dated file in this directory. It quotes this one whole and says it was written afterwards, and its
  decision is then weaker evidence." It was written after part K's after run, its first and second readings and the
  reading aids named at the top of this file had been seen.
- **It takes no reading** of any part and declares nothing met; the bundle's README reads part K again under it.
- **The alternative not taken** is the owner's, quoted above: "Leave K NOT MET".

## The rule it revises, quoted whole

> # Bundle SJL-14-001's decision rule: Broiler.JS on the shared syntax library, judged by test262 per path, by the bridge's identity with the switch off, by the side-by-side run, by the parse corpus across the cut-over, by the portable compiler and its Native AOT sample, by the packages, by the downstream consumers, by the public API, by its parse measurement, by fuzzing and by the deletion search
>
> **Written:** 2026-11-03, at milestone SJL-14 (work package SJL-14.1) of the shared JavaScript libraries programme,
> with this repository's `docs/roadmap/SharedLibraries.md` section "Broiler.JS on the shared syntax library (SJL-14)" and
> Broiler.VM's JSD-0083, before any change it judges exists: no bridge, no switch, no adapter package and
> no deletion. Committed in the same change: [`corrections.md`](corrections.md), [`dispositions.md`](dispositions.md)
> and [`inputs/`](inputs/README.md) (part I's workloads, part J's seeds). **Milestone judged:** SJL-14,
> `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-14-broiler-js-syntax.md`: its Broiler.JS clauses 1 to 5,
> clauses 2 and 5 as revised on 2026-11-03 before any evidence (decisions D50 and D49), and clause 6 ("Broiler.JS's
> parse measurement meets its rule.") as the milestone file states it when part I is read: what clause 6 becomes while
> no controlled lane exists is the owner's own choice (decision D48), and this file does not make it. **Owner:** the Broiler.JS maintainer. **Reviewer:** none. Every role this rule names is
> held by one person (the programme records this as Broiler.VM's exception EX-30), and this rule does not claim that
> anyone independent has read it.
>
> **What this file is.** Eleven predeclared decisions, one per part. Part A is template T2 and part I template T5. Part
> B is an identity, part C a side-by-side comparison, part D the parse corpus across the cut-over with bundle
> `sjl-12-001`'s tool, part E layer L10, part F layer L11, part G layer L8, part H layer L12, part J layer L9 and part K
> the deletion search. Each part decides alone. It holds no figure. The bundle has a `manifest.json` (decision D56).
>
> **Bundle `sjl-12-001`'s tool** (parts B and D) is used byte-unchanged. From the after commit it builds with the
> obsolete marks' diagnostic identifier suppressed by the `NoWarn` of the repository root's `Directory.Build.props`, which
> it inherits (decision D50), and the `FastParser` facade it calls throws today's exception types, `FastParseException`
> for a refusal. A build of the tool that fails at any commit makes the part that needs it void, not MET.
>
> **The corrections this rule classifies by** are [`corrections.md`](corrections.md)'s K1 to K10, each with its rows of
> Broiler.VM's `docs/evidence/sjl-13-001/correspondence.md`, or of this bundle's inherited-context probes
> ([`inherited-context/`](inherited-context/README.md), rows IC1 to IC7 of K10), and its test262 paths, listed in `Broiler.JS/CHANGELOG.md` by
> name at the cut-over (decision D45). **JSD-0080 decision D27's listed refusals** are not corrections: they are an
> implementation limit, class C3, enumerated by differencing the corpus outcomes on the 256 MiB thread the SJL-12
> baselines used (decision D47).
>
> **The stage and timing classes** (decisions D45 and D64), named before any run with their rows in
> [`corrections.md`](corrections.md):
> - **E, early-error stage** - an input accepted at entry point P (the parser alone) at the side the base stands for,
>   and refused at P or B on the other side with a SyntaxError whose shared code is a `syntax` row of the stage table,
>   K refusing on both sides, where either the row's catalogue `js_stage` names only compiler sites
>   (`SyntaxValidation`, `FastCompiler.EarlyErrors`, `Visit*`, `DirectEvalSupport`; the stage table's
>   `js_compiler_only` `yes`, [`corrections/class-e.tsv`](corrections/class-e.tsv)), or the row's `js_compiler_only` is
>   `partly` ([`corrections/partly.tsv`](corrections/partly.tsv)) or `no`, and bundle `sjl-13-001`'s classification
>   expects the input refused in configuration J with that row among the rows whose codes may come first (decision D45
>   as revised on 2026-11-03, finding S14-F1 in [`corrections.md`](corrections.md)); a row the stage table marks
>   `not-checked` is never class E; listed with its row. The library checks at the parse what this engine checked only
>   in its compiler.
> - **T, timing** - for those of the E rows whose check ran lazily or after other code ran (a `Visit*` check at a
>   function's first call), an input or test262 row whose SyntaxError this engine raised later than the specification's
>   early-error point, or not at all when the function was never called, and raises before evaluation after; listed with
>   its row.
>
> **The product paths** (decision D42): `Broiler.JS/` except `Broiler.JS/benchmarks/` and `Broiler.JS/CHANGELOG.md` -
> which includes `Broiler.JS/Broiler.JavaScript/`, the engine's main project, whose folder has no suffix -
> `Broiler.ECMAScript/` outside `Broiler.ECMAScript/docs/`, and the root's `Directory.Packages.props`,
> `Directory.Build.props`, `Directory.Build.targets`, `global.json` and `NuGet.config`.
>
> **The commits, named before they exist.**
> - **The rule commit** carries this file.
> - **The bridge commit (W1)** is the default-branch merge that lands SJL-14.2's bridge with the switch off by default,
>   one merge. **Its base** is its first parent, which descends from the rule commit; W1 is the only first-parent commit
>   that carries any part of the bridge or the switch.
> - **The after commit (W2)** is the default-branch merge that lands SJL-14.3 to SJL-14.5, one merge. **The base commit**
>   is its first parent, which descends from W1 and from the rule commit; W2 is the only first-parent commit that
>   carries any part of SJL-14.3 to SJL-14.5 (the switch's removal, the adapter packages, the portable compiler's and the
>   module analysis's move, the deletions).
> - If either window had to land in more than one merge, each is named in this bundle, its base is the first one's first
>   parent, and its after runs are taken at the last one.
> - **A dark commit** is any default-branch merge between W1 and the base commit that changes only
>   `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*/` (decision D52). Each carries part B's identity check with its own
>   first parent, retained in this bundle; its switch-on outputs are retained and read, and part C, taken at the base
>   commit, judges them.
> - Each run names its tested commit; hosted runs are dispatched at any time after their commits exist.
>
> A part whose base was taken at any other commit, or whose commits fail those conditions, is void.
>
> ## Part A - test262 parity per path, in UTC, with negative tests (template T2)
>
> ```rule
> evidence class: conformance parity (not a measurement)
> population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e - every path the runner's --all-script-host-verifiable selection admits with --include-negative, in both variants the canonical configuration runs, original and terser
> configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, by .github/workflows/test262-evidence.yml as the base commit carries it and unchanged at the after commit, dispatched with the tested commit's forty-digit identifier, suite_ref at the revision above, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone empty and include_negative true: eight shards merged by scripts/compliance/merge_test262_shards.py, --minifier terser at the version its lock file pins, no memory limit, negative-metadata tests included and their expected errors verified, a --self-check before each shard, every shard running the same shell, and every shard in UTC, shown by its host-zone record; both runs' host-zone records read alike; the base run at the base commit and the after run at the after commit, on the same RID; each artifact imported unchanged, with the zip's digest
> comparison: per path and variant, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, the classification file retained beside its report
> timeouts: a row that timed out in either run is the one the workflow ran again alone, with one worker; the script takes the re-run through --base-rerun or --after-rerun, and both are retained
> admitted difference classes:
>   C1 correction - a row failing at the base that passes after, because the shared frontend follows the pinned text where this engine did not: each row one of corrections K1 to K10 explains; listed with its path, its variant and its correction, and in the changelog (decision SJL-D-16); a C1 row that none explains is recorded as a finding against the correspondence and is a regression
>   C3 implementation limit (JSD-0080 decision D27) - a row passing at the base and failing after because its source nests past the bound the frontend applies for this engine, refused with a SyntaxError for nesting; listed with its path, its variant and the depth the frontend reports; expected empty
>   T timing (decision D64) - a negative row failing at the base that passes after because the SyntaxError of a class T row of corrections.md, which this engine raised late or not at all, is raised before evaluation; listed with its path, its variant and its row
> no C5: no data, pin or policy moves
> every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after outside C3
> MET if and only if both runs are complete (every expected shard reported, one suite revision, one shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, the script finds them comparable, and every changed verdict is in C1, C3 or T and is listed.
> NOT MET otherwise; and if the two runs' host-zone records differ, the part is not judged until the base is taken again at the same commit in the after run's zone record.
> ```
>
> ## Part B - the bridge's identity with the switch off (W1, and each dark commit)
>
> ```rule
> evidence class: identity (not a measurement)
> population: every input and variant of conformance/parse/corpus.manifest
> configuration: bundle sjl-12-001's tool docs/evidence/sjl-12-001/tools/parse-corpus, byte-unchanged and built as this file's header says, in Release against each commit's projects, on one thread with a 256 MB stack, no LANG and TZ=UTC: entry points P and K, and the --dump mode (before and after the compiler's for-loop pass), at W1's first parent and at W1, the switch at its default; the same at each dark commit and its first parent; and every test project of Broiler.JS.slnx at W1 by its CI run; at each dark commit and its first parent, the side-by-side tool's entry points B and K with the switch on are also run, retained and read, not judged here (part C judges the switch-on outputs at the base commit)
> comparison: per input, variant and entry point, the outcome, the exception type and the message; per input, the dumps' hashes
> admitted difference classes: none
> MET if and only if, at W1 and at every dark commit, both runs are complete and identical in every field, and W1's CI run of every test project passed.
> NOT MET otherwise.
> ```
>
> ## Part C - the side-by-side run (SJL-14.2)
>
> ```rule
> evidence class: conformance parity (not a measurement)
> population: every input and variant of the parse corpus; every test of Broiler.JS's test projects; the whole pinned suite as part A selects it
> configuration: at the base commit (W2's first parent, the last state before the cut-over, every dark commit included), one build in Release: the side-by-side tool docs/evidence/sjl-14-001/tools/side-by-side at entry points P (the old parser), B (the bridge) and K (the whole compile, the switch off and on), with the canonical dump of bundle sjl-12-001's tool applied to P's and B's trees; every test project with the switch on through its internal options member; scripts/compliance/run_test262.py locally on the programme's machine through the shell built at that commit with its hidden switch off and on, in UTC, with negative tests, one worker per core; the same runs at W1, retained and read, not judged
> comparison: per input and variant, P against B and K-off against K-on: outcome, exception type, message and dump hash; per test, its result off and on; per test262 path and variant, its verdict off and on
> admitted difference classes: part A's C1 (by K1 to K10), C3 and T; E, by this file's header; and M - a refusal with the same exception type at the same entry point whose message differs, each listed in the bundle's generated message table with both texts (decision D46); and S - a dump difference only in a node's span, where P's span disagrees with the node's source text and B's agrees, listed with both spans checked against the text
> every other difference is unclassified
> MET if and only if every run at the base commit is complete, and every difference is in C1, C3, E, T, M or S and is listed.
> NOT MET otherwise.
> ```
>
> **How it is read.** This is SJL-14.2's done-when, "the side-by-side run is retained with every difference
> classified", judged where it last matters: at the base commit, after every dark merge. The run at W1 is retained so
> that a library fix found there can be traced to its dark merge. An unclassified difference is a bridge or library
> defect, fixed as a dark merge and run again before W2 merges.
>
> ## Part D - the parse corpus across the cut-over
>
> ```rule
> evidence class: conformance parity (not a measurement)
> population: part B's
> configuration: bundle sjl-12-001's tool, byte-unchanged and built as this file's header says, at the base commit and at the after commit, entry points P (at the after commit, the adapter Parser package's FastParser facade, which keeps every member the tool names and throws today's exception types; decision D50) and K, and the --dump mode
> completeness: every input and variant at both entry points at both commits; the manifest's digest equal
> controls: a copy of the base report with one outcome flipped and one message changed, which the comparison must report as two differences; retained
> comparison: scripts/compliance/compare_parse_corpus.py, per input, variant and entry point: outcome, exception type and message; per input, the dump hashes
> admitted difference classes: part C's C1, C3, E, T, M and S
> every other difference is a regression
> MET if and only if both runs are complete, the control is reported, and every difference is in C1, C3, E, T, M or S and is listed.
> NOT MET otherwise.
> ```
>
> **How it is read.** This is SJL-14.1's "old-tree-against-new-tree comparison over the parse corpus". The dump reads
> non-public fields of every `Broiler.*` type, so a field the bridge carries differently shows; class S is the only tree
> difference admitted outside the corrections.
>
> ## Part E - the portable compiler and its Native AOT sample (clause 4; layer L10)
>
> ```rule
> evidence class: publish and run (not a measurement)
> population: Broiler.JS/Broiler.JavaScript.Portable.Tests, which holds Broiler.JavaScript.Portable.Compiler's tests (it has no test project of its own), including the tests the after commit adds for the early errors the portable compiler now reports; the sample Broiler.JS/samples/Broiler.JavaScript.NativeAotSample
> configuration: dotnet test of that project in Release at the after commit; scripts/performance/collect_phase5.py, run locally on linux-x64 at the base commit and at the after commit - the portable compiler run on fibonacci.js with Program.cs's arguments, its output compared byte for byte with the checked-in FibonacciProgram.g.cs, the sample published with Native AOT and run; and a closure listing of Broiler.JavaScript.Portable.Compiler at the after commit (its transitive project and package references, and the assemblies its publish output holds)
> MET if and only if every test passes at the after commit; at the after commit the regenerated program is identical to the checked-in copy, the sample publishes with Native AOT, runs and answers as the collector requires, with no dynamic code; and the closure holds no Broiler.JavaScript.Runtime and no Broiler.JavaScript.Storage.
> NOT MET otherwise.
> ```
>
> ## Part F - the packages (clause 5's first half; layer L11)
>
> ```rule
> evidence class: packages (not a comparison)
> population: every package eng/pack.ps1 packs at the after commit, Broiler.ECMAScript.Frontend among them
> configuration: eng/pack.ps1 and eng/verify-feed.ps1 at the after commit into a local feed; scripts/packaging/test_pristine_consumer.py against that feed; a metadata reader, committed to the bundle, listing the [Obsolete] attribute, its diagnostic identifier and its message on every public type of Broiler.JavaScript.Ast and Broiler.JavaScript.Parser
> controls: the pristine consumer restored with Broiler.ECMAScript.Frontend removed from the feed, which must fail with the missing-package error
> MET if and only if packing and feed verification pass, the pristine consumer passes, the control fails as stated, and every public type dispositions.md marks obsolete carries the mark with the removal version decision D50 names.
> NOT MET otherwise.
> ```
>
> ## Part G - the downstream consumers (clause 5's second half; layer L8)
>
> ```rule
> evidence class: consumer parity (not a measurement)
> population: Broiler.JSeal's test suites and Broiler.HtmlBridge's, at the commits the bundle names, read before the base commit exists
> configuration: two candidate feeds, packed by eng/pack.ps1 at the base commit and at the after commit; Broiler.JSeal's eng/test-package-consumer.ps1 with -CandidateFeed and -CandidateVersion for each, its pins unchanged; Broiler.HtmlBridge in a scratch copy whose package source and Broiler.JavaScript versions are overridden for each candidate, never committed; every test's result retained
> comparison: per test, its result against the base candidate and against the after candidate
> admitted difference classes: none
> MET if and only if both suites ran against both candidates, and every test that passes against the base candidate passes against the after candidate (decision D49).
> NOT MET otherwise; and the part is not judged while either suite has not been run against both candidates.
> ```
>
> **How it is read.** Clause 5, as revised under update rule 5 before any evidence (decision D49), asks that the
> downstream candidate-feed runs lose no test to the cut-over. A test that fails against both candidates is recorded with its cause; the
> JSeal module adapter's reflection on members SJL-12.5 removed is such a cause, and a finding of SJL-12.5's.
>
> ## Part H - the public API (layer L12)
>
> ```rule
> evidence class: API baseline (not a comparison of runs)
> population: every public type and member of every package eng/pack.ps1 packs
> configuration: the API lister docs/evidence/sjl-0-001/api/api-lister.csproj at the base commit and at the after commit
> comparison: per member, present at the base, at the after, or at both with a changed signature or attribute
> MET if and only if every member removed, changed or newly obsolete has a row of docs/public-api.md at the after commit with its SJL-D-25 disposition - (a) forwarder, (b) obsolete with the removal version, or (c) removal with a recorded consumer check - and dispositions.md agrees with it.
> NOT MET otherwise.
> ```
>
> ## Part I - Broiler.JS's parse measurement (clause 6; template T5)
>
> ```rule
> evidence class: measurement
> population: the workloads of docs/evidence/sjl-14-001/inputs/workloads as committed with this rule, the real corpora of Broiler.JS/benchmarks' CompilePhaseMetrics; quantity: the time from source text to the tree the compiler reads, measured on the calling thread (at the base: FastParser.ParseProgram; after: the hop to the parse thread, the frontend's parse, the bridge and the return; decision D47)
> arms: B the base commit's build; A the after commit's; A-again, the A/A lane
> configuration: the harness Broiler.JS/docs/roadmap/Measurement.md names, on the lane the bundle names with its effective configuration; interleaved arms; repetitions and warm-up as Measurement.md fixes them; each lane's condition, that every workload parses to the outcome the base gives, checked before and after
> quantity, noise floor and comparison: Q the median per workload and arm; N = |Q(A) - Q(A-again)|; D = Q(A) - Q(B) against N
> lane: the part is judged only on a controlled lane as Measurement.md requires; on a lane that is not controlled the part is void - neither MET nor NOT MET - and its runs are retained and recorded as void
> MET if and only if the lane is controlled, every condition passes, and for every workload D <= N.
> NOT MET if and only if the lane is controlled and a condition fails or some workload has D > N.
> ```
>
> **What part I decides.** Clause 6 ("Broiler.JS's parse measurement meets its rule.") is read from this part as the
> milestone file states the clause when the part is read: as written, the clause is met only by a MET on a controlled
> lane, and a void part leaves it unmet; what the clause becomes while no controlled lane exists is the owner's own
> choice (decision D48), which this file does not make. No speed claim.
>
> ## Part J - fuzzing (layer L9)
>
> ```rule
> evidence class: robustness (not a measurement)
> population: the seeds docs/evidence/sjl-14-001/inputs/fuzz-seeds.txt names, each with its goal, its count of mutants and the time bound per compile
> configuration: a seeded mutation session, committed to the bundle, compiling each mutant through CoreScript.Compile at the after commit in Release
> reading: each mutant is accepted, or refused with a SyntaxError; a finding is any other exception type, a lost process or a compile past the bound
> controls: a scratch copy in which the bridge throws on one node kind the seeds produce; the session must report a finding; reverted
> MET if and only if no finding is reported and the control is reported.
> NOT MET otherwise.
> ```
>
> ## Part K - the deletion search (clause 2)
>
> ```rule
> evidence class: code fact (not a comparison)
> population: SJL-14.5's deletion list as clause 2 reads it after its revision of 2026-11-03 (decision D50), member by member as dispositions.md lists it: FastParser's partial files, FastScanner's and FastTokenStream's scanning code, FastKeywordMap's table, CharExtensions' scanner members, the parser's half of FastScope, RegExpValidator, DecodeBracedUnicodeEscapes and the parenthesis-balance patch (each deleted, or relocated behind the provider as decision D51 allows and dispositions.md records), the division fallback, the two token re-scans and the direct-eval prefix decision D41 deletes at the cut-over (SyntaxValidation.ContainsLegacyOctalLiteral, DirectEvalSupport.ContainsStrictReservedWordUsage, and DirectEvalSupport's "use strict" prefix with the two places that skip it), and the test-only switch
> configuration: the deletion search script, committed to the bundle with the after commit's change, as bundle sjl-11-001's part E runs its E-1 search: at the after commit, over Broiler.JS/ and Broiler.ECMAScript/ outside docs/, each listed member's declarations and references, every hit listed with its file and line
> controls: a scratch copy of the after commit in which one deleted member is declared again; the search must report it; reverted, and the revert checked
> MET if and only if no listed implementation is found, every hit is a facade member dispositions.md lists as an obsolete forwarder over the bridge or a relocation decision D51 allows, and the control is reported.
> NOT MET otherwise.
> ```
>
> ## Revisions
>
> A rule revised after any run it judges has been seen is a new dated file in this directory. It quotes this one whole
> and says it was written afterwards, and its decision is then weaker evidence.
>
> ## What this file does not decide
>
> - **Clause 1's code fact** ("parse with the shared frontend"), read from the tree at the after commit, which the bundle
>   retains; clause 2 is part K's.
> - **The library**, which the home's bundle `sjl-13-001` judges.
> - **Broiler.VM's clauses.**
> - **A speed claim of any kind.**
