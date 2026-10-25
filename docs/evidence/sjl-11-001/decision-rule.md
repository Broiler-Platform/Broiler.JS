# Bundle SJL-11-001's decision rule: test262 parity per path with negative tests, Broiler.JS's measurement, the adversarial corpus in this engine, culture independence, and the deletion list

**Written:** 2026-10-25, at milestone SJL-11 (work package SJL-11.1) of the shared JavaScript libraries programme,
with the area verdict this repository's `docs/roadmap/SharedLibraries.md` records ("Regular expressions (SJL-11.1)")
and Broiler.VM's JSD-0081,
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0081-regular-expressions-move-to-broiler-regex-under-its-owner-conditions.md`.
It was written before any change it judges exists: no adapter onto Broiler.Regex 0.2, no deletion and no benchmark
class. Committed in the same change: part B's workloads as JavaScript text and part C's programs, under
[`inputs/`](inputs/). **Milestone judged:** SJL-11,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-11-regexp.md`: its Broiler.JS clauses 4 ("The translator
and the validators are gone"), 5 ("test262 `RegExp` per path meets the area rule") and 6 ("Duplicate named groups
still work"), SJL-11.6's adversarial corpus in this engine, and this engine's measurement. **Owner:** the Broiler.JS
maintainer. **Reviewer:** none. Every role this rule names is held by one person (the programme records this as
Broiler.VM's exception EX-30), and this rule does not claim that anyone independent has read it.

*Revised 2026-10-25, the day it was written, after review, before any run and before this change reached the default
branch: part A's MET line admits a row passing at the base that fails after under either kind of class C3, where it
read "outside C3 (L)", so that kind (E), row M4's empty match, admits a move in either direction, as its definition
says. Nothing else changed.*

**What this file is.** Five predeclared decisions, one per part.
- **The templates.** Part A is template T2 of the programme's validation document
  (`Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4), part B T5. Part C is resource accounting
  over a declared corpus (JSD-0081 decision D37), part D an identity of vector reports across host cultures
  (decision D33), and part E a search and a metadata check (decision D34).
- **Each part decides alone**, and no part reads another's verdict.
- **Part B is the only measurement.** Its runs are retained in `sjl-11-001/measurement/`, because
  [`Measurement.md`](../../roadmap/Measurement.md) asks every result bundle to declare one evidence class. It is not a
  gate clause of SJL-11; JSD-0081 decision D19 says how its verdict is recorded.
- **Other rules.** Broiler.VM and Broiler.RegEx commit their own rules in their own bundles `sjl-11-001`. Neither is
  read here.
- **It holds no figure.** Where a count or an allowance matters, it names the file that fixes it.

**The correspondence this rule classifies by** is Broiler.VM's `docs/evidence/sjl-11-001/correspondence.md`, read
from code at this repository's `1f91b8f1`, Broiler.VM's `aaa39fb` and Broiler.Regex `0.1.0-preview.4` (`c1422f4d`),
which this engine restores for every `u` and `v` pattern, with every behavioural cell run. Its section "Revisions"
reads this repository's `792ff56e`, where SJL-12.5 moved the validators to `Broiler.JavaScript.Ast` with comments and
a forwarder as their only change. Its rows are named here by their letters and numbers: S (syntax and early errors),
M (matching), B (budget and limits), F (flags), T (text helpers) and U (data). A row's class that rests on a reading of
the specification's text stands until `regexpref` checks that reading; a reading it refutes is a finding against
JSD-0081, and the row is reclassified there (decision D25).

**This engine's corrections, named before any run** (JSD-0081 decision D23). Each is a row of the correspondence
that names a correction of Broiler.JS, or of Broiler.Regex `0.1.0-preview.4` as this engine routes it. Each is listed
by its row in `Broiler.JS/CHANGELOG.md` at SJL-11.5 (decision SJL-D-16 (a)). Where the correspondence names the
test262 files a row explains, they are named here; each fails in this repository's bundle `sjl-10-001` after run
(`test262/after/test262-results.json.gz`, at `00b65dd0`, whose validators, scanner, compiler literal and
`BuiltIns/RegExp/` are byte-identical to `1f91b8f1`'s).
- **The literal check**, closed by re-pointing the scanner's check to Broiler.Regex's validate entry (decision D22):
  - **S2b**, a reference spelled with an escape, refused by the literal path where the constructor accepts it;
  - **S7**, a quantified look-behind accepted without `u`:
    `test/language/literals/regexp/invalid-optional-lookbehind.js`, `invalid-optional-negative-lookbehind.js`,
    `invalid-range-lookbehind.js` and `invalid-range-negative-lookbehind.js`;
  - **S8**, the `v` flag's reserved syntax accepted:
    `test/built-ins/RegExp/prototype/unicodeSets/breaking-change-from-u-to-v-01.js` to
    `breaking-change-from-u-to-v-28.js` but `-03.js`, whose pattern `[[]` this engine refuses today, the others'
    patterns being `[(]` `[)]` `[{]` `[}]` `[/]` `[-]` `[|]` `[&&]` `[!!]` `[##]` `[$$]` `[%%]` `[**]` `[++]` `[,,]`
    `[..]` `[::]` `[;;]` `[<<]` `[==]` `[>>]` `[??]` `[@@]` ``[``]`` `[~~]` `[^^^]` `[_^^]`; and `[a|]` and `[a!!b]`,
    which no file names;
  - **S11**, a literal's errors reported only when it is evaluated, not early: the invalid modifiers and the `v`-mode
    classes the row lists, explaining `test/language/literals/regexp/early-err-modifiers-code-point-repeat-i-1.js`,
    `early-err-modifiers-code-point-repeat-i-2.js`, `early-err-modifiers-other-code-point-uppercase-I.js`,
    `early-err-modifiers-should-not-case-fold-i.js`, `early-err-modifiers-should-not-case-fold-m.js`,
    `early-err-modifiers-should-not-case-fold-s.js`, and `early-err-arithmetic-modifiers-` followed by
    `add-remove-i.js`, `add-remove-m.js`, `add-remove-multi-duplicate.js`, `add-remove-s.js`, `both-empty.js`,
    `code-point-repeat-i-1.js`, `code-point-repeat-i-2.js`, `no-colon-1.js`, `no-colon-2.js`, `no-colon-3.js`,
    `other-code-point-uppercase-I.js`, `reverse-add-remove-i.js`, `reverse-add-remove-m.js`,
    `reverse-add-remove-multi-duplicate.js`, `reverse-add-remove-s.js`, `reverse-code-point-repeat-i-1.js`,
    `reverse-code-point-repeat-i-2.js`, `reverse-other-code-point-uppercase-I.js`, `reverse-should-not-case-fold-i.js`,
    `reverse-should-not-case-fold-m.js`, `reverse-should-not-case-fold-s.js`, `should-not-case-fold-i.js`,
    `should-not-case-fold-m.js` and `should-not-case-fold-s.js`;
- **S2a**, a name spelled once with an escape and once without, in one alternative, accepted (with the library);
- **S10's class case**, `[\uD83D\u{DC38}]` under `u`, which joins the two escapes into one astral code point;
- **case-insensitive matching through .NET**: M2 (bundle `sjl-7-001`'s F2), M2a under `tr-TR` and under `lt-LT`, and
  M2b; and **U3**, by data decision (class C5 of part A, decision SJL-D-6 (c));
- **M3**, .NET's line terminators (bundle `sjl-7-001`'s W5), with `$` without `m`;
- **M4's matches of half a surrogate pair** from a `lastIndex` inside the pair. The step out of a pair that SJL-11.5
  adds moves only the matcher's start, never the reported `"index"` or substring (decision D23);
- **M6**, `String.prototype.search`'s direct call;
- **T1**, a literal's `source` rewritten where its constructor twin's is not;
- **with Broiler.Regex `0.1.0-preview.4`**: B1 (the shortened match and the no-match of a long greedy run), B2 (the
  wrong index), B3 (the flipped look-around), B5 (the stack guard's fallback, to .NET and to no match), B8 (the process
  abort of a flat sequence), S3b's process abort at the deepest nesting, and M7's look-behind back-reference inside a
  pair.

Not on it: `\0` before a non-ASCII digit, which is Broiler.Regex's alone and not observable here (row S4); B4,
uncharged alternation, which this engine cannot show because it passes no meter; and an invalid literal read as
division, which is SJL-14's (row S9). None of the rows above but S7, S8 and S11 names a test262 file.

**Why negative tests are included.** The corrections include what a parser refuses or admits, and test262 tests them
through negative-metadata tests: rows S7, S8 and S11 explain only negative tests. Bundles `sjl-7-001`, `sjl-8-001`,
`sjl-10-001` and `sjl-12-001` took their runs with them.

**The commits, named before they exist.**
- **The rule commit** carries this file. It changes only `docs/`.
- **The inputs commit** is the default-branch commit that adds `RegExpBenchmarks` and the benchmark `Program.cs`'s
  `--regexp-answers`, changing only `Broiler.JS/benchmarks/` and `docs/`. It descends from the rule commit and from
  SJL-12's corpus commit in this repository (JSD-0081 decision D17).
- **The base commit** is the default-branch commit that the merge landing SJL-11.5 has as its first parent. It
  descends from the inputs commit. Every base run is taken there, before the merge, while merges to
  `Broiler.JS/Broiler.JavaScript.*/` and `Broiler.ECMAScript/` are held (decision D16).
- **The after commit** is that merge. If its first parent is not the commit the base runs were taken at, the base
  runs are void and are taken again before any after run is seen.

**Why the base is not taken at the rule commit.** SJL-11.2 is long, and other milestones' product changes (SJL-12's,
SJL-9's, SJL-17's) would land between a base at the rule commit and the after run, and be read as SJL-11's.

## Part A - test262 parity per path, in UTC, with negative tests (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to and Broiler.VM's src/tests/conformance/pins/test262.pin pins - every path the runner's --all-script-host-verifiable selection admits with --include-negative, in both variants the canonical configuration runs, original and terser
configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, by .github/workflows/test262-evidence.yml as this file's commit carries it, dispatched with the tested commit's forty-digit identifier, suite_ref at the revision above, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone empty and include_negative true: eight shards merged by scripts/compliance/merge_test262_shards.py, --minifier terser at the version its lock file pins, no memory limit, negative-metadata tests included and their expected errors verified in every shard and in the re-run, a --self-check before each shard, every shard running the same shell, shown by the hashes of its binaries, and every shard in the runner's own zone, UTC, shown by its host-zone record; both runs' host-zone records read alike, line for line; the base run is taken at the base commit and the after run at the after commit, on the same RID; each run's artifact test262-evidence is imported unchanged, with the digest of the zip as downloaded
comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report; the script refuses two runs whose includeNegative differs
timeouts: a row that timed out in either run is the one the workflow ran again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
duplicate named groups (clause 6): every row of a file whose front matter's features list regexp-duplicate-named-groups passes at both commits
admitted difference classes:
  C1 correction - a row failing at the base that passes after, because the shared engine follows the specification where this engine did not, under a row of this file's list of this engine's corrections (rows S2a, S2b, S7, S8, S10's class case, S11, M2, M2a, M2b, M3, M4's half-pair matches, M6, T1, and with Broiler.Regex 0.1.0-preview.4 B1, B2, B3, B5, B8, S3b's process abort and M7's look-behind), the test262 files that list names for rows S7, S8 and S11 among them; each listed with its path, its variant and the row, and listed in Broiler.JS/CHANGELOG.md (decision SJL-D-16 (a)); a C1 row that no row of the list explains is recorded in the bundle as a finding against JSD-0081 and is a regression
  C3 implementation-defined, of two kinds: (L) a limit - a row passing at the base that after throws this engine's RangeError for Broiler.Regex's frame or trail ceiling, or its SyntaxError for the nesting limit (JSD-0081 decisions D7, D10 and D39); each listed with the limit it reached, shown by the row's pattern and subject run through Broiler.Regex at library level under the same policy, whose LimitExceeded status names that limit; the library run is retained; and (E) a row whose verdict moves only because this engine's answer to the empty match from a lastIndex inside a surrogate pair under u or v moves (row M4's empty match), which JSD-0081 decision D41 classes implementation-defined until the specification's text is corrected; listed with the row and the decision
  C5 data policy by decision - a row whose verdict changes because Unicode 17.0.0's data through the Unicode owner's provider replaces the runtime's in a construct the translator delegated to .NET (a character class or case-insensitive matching without u), under SJL-D-6 (c); each listed with its row (row U3)
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, and a row whose verdict moves between failed, timed out and skipped without a class
MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision, one shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, the script finds them comparable, every duplicate-named-groups row passes at both commits, no row passing at the base fails, times out or is skipped after outside C3, of either kind, and every changed verdict is in C1, C3 or C5 and is listed.
NOT MET otherwise; and if the two runs' host-zone records differ, because the runners' tzdata moved between them, the part is not judged until the base is taken again at the same commit in the after run's zone record.
```

**How it is read.** As bundle `sjl-7-001`'s part A. The bundle retains each run, the comparison's report, the
classification file, and a table naming each C1, C3 and C5 row with its reason. The script decides no class.
- **The files this file's list names are expected to move**, each from failing at the base to passing after. One
  that still fails after is no difference this part judges, and the bundle records it as a correction SJL-11.5 did not
  make, which clause 7 then reads.
- **A row of row S9**, an invalid literal read as division, is no C1 row: the division fallback stays, SJL-14's
  (decision D22), and a change of such a row's verdict is a finding against JSD-0081 and a regression.

## Part B - Broiler.JS's measurement (template T5)

```rule
evidence class: measurement; in Measurement.md's classes, an acceptance comparison when the lane is a controlled lane as its section "Controlled lanes and effective configuration" requires, and smoke otherwise; a smoke run is void for this part and is not judged
population: the six workloads of RegExpBenchmarks (Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks/RegExpBenchmarks.cs) as the inputs commit adds it, each running the JavaScript text of docs/evidence/sjl-11-001/inputs/workloads/ verbatim, as committed with this rule - Scan (scan.js), Captures (captures.js), Replace (replace.js), Split (split.js), Unicode (unicode.js) and Backtrack (backtrack.js) - each with its control, ScanControl to BacktrackControl, running the *.control.js file beside it; the benchmark project built in Release under JIT with its own runtime settings; one machine and one RID, both named in the sub-bundle's manifest
arms: B the base commit, with RegExpBenchmarks.cs and the benchmark Program.cs as the inputs commit has them; A the after commit, with the same two files; A-again the A arm measured a second time under another lane name, the A/A lane; each arm's benchmark and engine assemblies named by hash in the manifest, which is written and made read-only before any lane runs
configuration: in every repetition, B, A and A-again each run once, in an order that rotates by repetition; one run is one launch of the benchmark executable with --filter on this class only, BROILER_BENCHMARK_PROFILE=baseline and a build timeout ample for the lane, which runs all six workloads and their controls, each in BenchmarkDotNet's own child process, under the baseline job BenchmarkConfiguration.cs fixes at the inputs commit; the process's culture invariant; seven repetitions per arm, every one retained
condition: before and after every run, the arm's --regexp-answers prints every workload's answer, its program's completion value; each answer counts the operations that completed and equals the same workload's answer at arm B's first check; a run whose arm's answers differ fails the condition
configuration check: the configuration object --regexp-answers prints (framework, RID, architecture, GC mode, tiering, PGO and ReadyToRun settings, vector acceleration, processor count, the invariant-globalization variable and the current culture), and in each run's full JSON report BenchmarkDotNet's host environment record with each workload's hardware-intrinsics record, are identical across arms, lanes and repetitions
quantity: Q(workload, arm) = the median, over the arm's seven runs, of BenchmarkDotNet's reported median time per operation of that workload in that run
noise floor: N(workload) = |Q(workload, A) - Q(workload, A-again)|
comparison: D(workload) = Q(workload, A) - Q(workload, B)      (positive: the after arm slower), against N(workload); one engine's after arm against the same engine's base arm, never against another engine
attribution, retained beside D and deciding nothing: E(workload, arm) = Q(workload, arm) - Q(control, arm)
guardrails, reported for every workload and arm and deciding nothing: BenchmarkDotNet's allocated bytes per operation and its collection counts
admitted difference classes: none
MET if and only if all of:
  1. the lane is a controlled lane, recorded as Measurement.md requires;
  2. every run's condition passes, before and after, for every arm;
  3. every configuration check is identical across arms, lanes and repetitions;
  4. every run completed every workload, and BenchmarkDotNet reported no workload with issues;
  5. for every workload, D <= N.
NOT MET otherwise.
readings:
  - a workload whose |D| <= N is below resolution, and meets clause 5;
  - a failed condition is a NOT MET, not a re-run; an interrupted collection may be run again from the start, with the stopped run retained and named as stopped;
  - this part returns MET or NOT MET and makes no performance claim; JSD-0081 decision D19 records a NOT MET as the trade the area design names, "a slower but bounded matcher", and a void part leaves the question open for this engine.
```

**Why a new class.** `RegexProfileMetrics` and `RegexCallEnvelopeMetrics` time .NET `Regex` and the routing
directly, which the cut-over deletes, so they cannot run unchanged at both arms.
`Broiler.JS/OtherTests/JIntPerfTests/Scripts/dromaeo-object-regexp.js` is not run either: part B's population is the
six workloads above, whose answers this rule's condition can hold.

**Why the answers count operations.** The cut-over corrects results this engine gets wrong today, so an answer
built from the results would change for a correction, and the condition would refuse the very change the milestone
exists to make. Each workload's answer counts the calls it completed, fixed by the loop bounds written in its file,
and is still failed by a lane that throws or stops early. The workloads' README says which rows of the correspondence
they stay away from.

**The workloads are Broiler.VM's**, byte for byte, as their README records; each engine measures them against itself
only, under its own rule, and no figure of one is compared with the other's.

## Part C - the adversarial corpus in this engine (resource accounting; SJL-11.6)

```rule
evidence class: resource accounting (not a measurement)
population: every case of docs/evidence/sjl-11-001/inputs/adversarial/manifest.tsv as committed with this rule, each a program generated by Broiler.RegEx's eng/adversarial-programs.py --engine broiler-js from that repository's tests/Adversarial/corpus.tsv, both named in the manifest's header by commit and SHA-256; the classes W, G, L, N, F, M, V and O, at the sizes the generator chose so that the specification's answer completes inside the manifest's wall-clock limit; the classes K and A are not in it, because this engine passes no meter (JSD-0081 decision D10) and their unbounded cases are judged at library level by Broiler.RegEx's rule; each case with its declared outcome after: completed with the specification's answer as the corpus declares it, a reading of the specification's text that regexpref checks (decision D25), a RangeError, or a SyntaxError
configuration: the shell BroilerJS built in Release; each case one process with --script-host, its program read as this rule's commit stores it and checked against the manifest's SHA-256 before the run, with the wall-clock limit the manifest's header states, the process's culture invariant; at the base commit and at the after commit; for each process its exit, its outcome (completed with its answer, a thrown error with its constructor, an abnormal end, or past the wall-clock limit) and its output retained
controls: a patch to a scratch copy of the after commit, never merged, in which the adapter answers no match where Broiler.Regex's match reports LimitExceeded naming frames or trail; every M case must be reported as differing from its declared outcome; retained; reverted and the revert checked
comparison: per case, the outcome after against the declared outcome, and against the outcome at the base
admitted difference classes:
  C1 correction - a case whose outcome at the base is a wrong answer, a no match where the specification matches, a shorter match, a flipped look-around, or an abnormal end, and whose outcome after is its declared one; each listed with the correspondence row
  C3 implementation limit - a case that completed at the base and after throws the RangeError or SyntaxError its declaration names, for a limit of decisions D7 and D10; listed
no other class
MET if and only if, after, every case gives its declared outcome, no process ends abnormally and none passes the wall-clock limit; every case whose outcome differs from the base's is in C1 or C3 and is listed; and the control is reported as stated.
NOT MET otherwise.
```

**How it is read.** This is validation section 7's control for SJL-11, "a pattern whose budget runs out, which must
report exhaustion and not no match", for the classes this engine runs, and SJL-11.6's "adversarial resource corpus"
in this engine, as decision D10 reads it.
- **The M cases** are the only cases that reach a ceiling, through the adapter's mapping of `LimitExceeded` to a
  catchable `RangeError`, so they are the cases the control can reach. At the base this engine has no ceiling, and an
  M case that completes there and throws after is class C3.
- **The N cases past the limit** complete at the base where this engine accepts their depth, and end the process
  where Broiler.Regex `0.1.0-preview.4`'s parser recurses past the stack (row S3b); after, they throw the
  `SyntaxError` of the nesting limit, under C3 or under C1 with row S3b.
- **The G cases** write their run as the class `[a]`, which the corpus's third presumption runs as one instruction
  (decision D37), so they complete inside the ceilings. A G case that throws after has broken that presumption and
  differs from its declared outcome.
- **A case declared to complete** is held to its declared answer, so a correction this engine needs but does not
  make is a NOT MET here, whatever part A shows.

## Part D - culture independence of the regular-expression vectors

```rule
evidence class: determinism across host cultures (not a measurement)
population: every vector of conformance/vectors/regexp/ at the corpus revision bundle sjl-11-001 names (the revision that classified both engines' first runs)
configuration: the engine-level vector runner (Broiler.JS/Broiler.JavaScript.Vectors.Tests) built in Release, run three times at the after commit - with the invariant culture, with tr-TR and with lt-LT as the current culture and current UI culture - and the same three runs at the base commit, retained and read, not judged
comparison: the three after reports, byte for byte; and each vector's result after against its class's expectation for broiler-js
admitted difference classes: none
MET if and only if the three after reports are byte-identical and every vector's result after is the outcome its class expects (normative: out; impl-defined: per-engine.broiler-js, row M4's empty match among them by JSD-0081 decision D41; a divergence.broiler-js field that the cut-over answers reports that it no longer holds).
NOT MET otherwise.
```

**How it is read.** Case-insensitive matching through .NET `RegexOptions.IgnoreCase` without `CultureInvariant`
(`Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.cs:698-701` at `1f91b8f1`) reads the current culture; the
base runs show it (row M2a, under `tr-TR` and under `lt-LT`), and the after runs must not.

## Part E - the deletion list (clause 4)

```rule
evidence class: deletion (not a comparison of runs)
population: the after commit's sources under Broiler.JS/ and its Release assemblies Broiler.JavaScript.Parser, Broiler.JavaScript.Ast, Broiler.JavaScript.BuiltIns and Broiler.JavaScript.Runtime
checks:
  E-1 search: git grep -w -n, at the after commit, over Broiler.JS/ but CHANGELOG.md, for each name of JSD-0081's Broiler.JS deletion list (its X6 sections and decisions D20 to D22) that a word search can find - the types RegExpValidator, RegexTiering and RegexTieringDiagnostics; the members ClampLargeQuantifiers, HexValue, GapScan, TryRouteToBroiler, FromNet, CreateRegex, ParseFlags, BuildFlagsString, TryAppendEscape, EscapePatternSource and DecodeBracedUnicodeEscapes; RegExpGroupNames' FirstError and FirstDuplicate and JSUnicodePropertyNames' FirstRefused, the regular-expression prechecks, wherever SJL-12.5 moved them - and the file Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.UnicodeSets.cs absent; no hit, except RegExpUnicodeValidator, the forwarder to Broiler.Regex's validate entry the list keeps under SJL-D-25 (a) (decision D21), and a hit the bundle lists with the declaration that shows it is another type's member of the same name
  E-2 metadata: no TypeRef or MemberRef into System.Text.RegularExpressions from the Parser and Ast assemblies, from any type whose namespace is Broiler.JavaScript.BuiltIns.RegExp, from JSStringPrototype's search, match, matchAll, replace, replaceAll and split members, or from LegacyRegExpState; this holds the members of the list a word search cannot find - the translator's members between ClampLargeQuantifiers and HexValue that are not object-model code, Escape, the overflow fallback and the .NET value field
  E-3 the scanner: FastScanner's regular-expression scan calls Broiler.Regex's validate entry, and DecodeBracedUnicodeEscapes and the parenthesis-balance patch are absent; the division fallback stays (decision D22)
  E-4 the direct call: JSStringPrototype's Search member, read at the after commit, reaches a regular expression only through RegExpCreate and an Invoke of @@search, with no call of a JSRegExp member that matches (row M6, the last item of decision D20's list); the bundle cites the member's lines
controls: one deleted member re-added to a scratch copy of the after commit, never merged; E-1 must report it; reverted and the revert checked
MET if and only if E-1 to E-4 find nothing outside their exceptions and the control is reported.
NOT MET otherwise.
```

**How it is read.** Clause 4 says "The translator and the validators are gone". `RegExpUnicodeValidator` stays as a
forwarder, its body a call of the validate entry, under decision SJL-D-25 (a), and the bundle names its body. The
.NET `Regex` uses outside ECMAScript matching (`JSIntl.cs`, `DateParser.cs`, `FastCompiler.DeferredBody.cs`) are
outside E-2's population; the bundle lists them as findings for their owners (decision D34).

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory, quoting this one whole
and saying it was written afterwards; its decision is then weaker evidence. The workloads and the adversarial programs
are not changed after they are committed; a change of Broiler.RegEx's corpus is such a revision, and the manifest then
names another SHA-256.

## What this file does not decide

- **The component clauses** (1 to 3), the library-level vectors and the reference arms: Broiler.RegEx's bundle.
- **Broiler.VM's clauses.**
- **Clause 7, every correction listed:** read from `Broiler.JS/CHANGELOG.md` against parts A, C and D's C1 rows, the
  vectors' classification and this file's list of this engine's corrections.
- **The public-API records:** `docs/public-api.md` and SJL-D-25's dispositions, with the consumer check's queries and
  date (decision D21), read from the tree at SJL-11.5.
- **.NET `Regex` outside ECMAScript matching** (`JSIntl.cs`, `DateParser.cs`, `FastCompiler.DeferredBody.cs`): listed
  as findings for their owners; not SJL-11's.
- **An invalid literal read as division:** SJL-14's, listed (JSD-0081 decision D22).
- **The downstream suites** and **a speed claim of any kind.**
