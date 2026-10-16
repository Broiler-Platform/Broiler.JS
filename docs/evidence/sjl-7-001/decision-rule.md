# Bundle SJL-7-001's decision rule: the Unicode area's test262 parity per path, with negative tests, and Broiler.JS's measurement

**Written:** 2026-10-16, at milestone SJL-7 (work package SJL-7.1) of the shared JavaScript libraries
programme, with the area verdict this repository's `docs/roadmap/SharedLibraries.md` records, before any
code of the Unicode area exists in any repository. Committed in the same change:
- the `include_negative` input of `.github/workflows/test262-evidence.yml`, which part A dispatches with;
- the workloads of part B, `UnicodeBenchmarks` in
  `Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks/UnicodeBenchmarks.cs`, with the benchmark
  `Program.cs`'s `--unicode-answers`.

**Milestone judged:** SJL-7, `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-7-unicode.md`: its
Broiler.JS clause 7, and the measurement its work package SJL-7.6 asks of each engine, which answers the fifth
reason of Broiler.VM's JSD-0031 §4(b) for this engine. **Owner:** the Broiler.JS maintainer. **Reviewer:**
none. Every role this rule names is held by one person (the programme records this as Broiler.VM's exception
EX-30), and this rule does not claim that anyone independent has read it.

**What this file is.** Two predeclared decisions, one per part, in the templates T2 and T5 of the
programme's validation document, `Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4.
**Each part decides alone**, and neither reads the other's runs. Part B is the only measurement, and the
bundle retains its runs in a sub-bundle of their own, `sjl-7-001/measurement/`, with its own README and
manifest, because [`Measurement.md`](../../roadmap/Measurement.md) asks every result bundle to declare
exactly one evidence class. The owner, Broiler.Unicode, commits the library-level rules (its test files, its
byte-for-byte generation and its reference arms) in its own bundle of the same name, and Broiler.VM commits
its own rule for its own engine; neither is read here. **It holds no figure.**

**The correspondence this rule classifies by** is Broiler.VM's `docs/evidence/sjl-7-001/correspondence.md`,
read from code in both engines and in the owner at the revisions it names, with every behavioural cell its
rows rest on run in this engine's shell. Its rows are named here by their letters: P (property lookup),
I and W (identifiers and white space), F (case folding), C (case mapping), N (normalization), S
(properties of strings) and G (segmentation).

**Why negative tests are included.** Two of this engine's corrections are about what a parser refuses or
admits: the property names of `\p{…}` (rows P1 to P4) and identifiers that must subtract Pattern_Syntax
(I1, I3). test262 tests both largely through negative-metadata tests, which expect a `SyntaxError` at parse
time, and the canonical configuration leaves those out. Every earlier bundle ran without them, so **no
earlier run can stand for this rule's base**, and both runs include them.

**The base is taken afresh, at the commit that carries this rule**, by the workflow. A run names its tested
commit in its own `run.txt`, so a base is the base whenever it is dispatched; a base taken at any other
commit is void.

## Part A - test262 parity per path, with negative tests (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to and Broiler.VM's src/tests/conformance/pins/test262.pin pins - every path the runner's --all-script-host-verifiable selection admits with --include-negative, that is the script-host-verifiable paths and the negative-metadata paths blocked by nothing but their metadata, in both variants the canonical configuration runs, original and terser
configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, by .github/workflows/test262-evidence.yml as this file's commit carries it, dispatched with the tested commit's forty-digit identifier, suite_ref at the revision above, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone empty and include_negative true: eight shards merged by scripts/compliance/merge_test262_shards.py, --minifier terser at the version its lock file pins, no memory limit, negative-metadata tests included and their expected errors verified in every shard and in the re-run, a --self-check before each shard, every shard running the same shell, shown by the hashes of its binaries, and every shard in the runner's own zone, UTC, shown by its host-zone record; the runner's ICU is the host's, recorded in the after run's bundle section with the runner image; the base run is taken at the commit that carries this file, and the after run at the commit that lands SJL-7.5's cut-over, named in bundle sjl-7-001, on the same RID; each run's artifact test262-evidence is imported unchanged, with the digest of the zip as downloaded
comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report; the script refuses two runs whose includeNegative differs, so a run taken without negative tests is never compared with one taken with them
timeouts: a row that timed out in either run is the one the workflow ran again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
admitted difference classes:
  C1 correction - a row failing at the base passes after, because the owner's implementation follows the specification where this engine did not: the correspondence rows P1 to P4 (property names), I1 and I3 (Pattern_Syntax in identifiers and group names), W1 (U+0085 in the scanner and in RegExp.escape), C1 and C4 to C8 (case mapping), N1 (lone surrogates in normalize and localeCompare), G1 and G2 (sentence and word boundaries); each one listed with its path, its variant and the row of the correspondence or the specification clause that names the correction, and listed in the changelog (decision SJL-D-16)
  C3 implementation-defined - the verdict changes on a surface the specification leaves to the implementation: a segment's isWordLike (row G4), which becomes the owner's word status; a locale's tailoring of segmentation, which neither engine applies (G6); each one listed with its row
  C5 data policy by decision - the verdict changes because pinned data at Unicode 17.0.0 replaces the host's, under decision SJL-D-6 (c) as Broiler.VM's JSD-0074 applies it: the rows I2 (identifier classes), C3 (default case mappings), N2 and N3 (normalization at Unicode 16 and 17, and under invariant globalization) and G3 (grapheme clusters); each one listed with its row and the decision
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, which no class admits, and a row whose verdict moves between failed, timed out and skipped without a class; no class admits the rows F2 and W5, which are the .NET translator's and SJL-11's
MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision, one shell, every shard in the zone, includeNegative true in both), the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1, C3 or C5 and is listed.
NOT MET otherwise.
```

**How it is read.** As bundle `sjl-6-001`'s rule reads its part A: the bundle retains each run, the
comparison's JSON report, the classification file it read, and a table naming each C1, C3 and C5 row with
its reason. The script decides no class; a classification of a row that passed at the base is reported as
refused. Totals do not decide the part. The two engines' test262 totals are not compared with each other
anywhere.

**Where the corrections are expected**, as a reading and not a limit on the classes: P1 to P4 under
`test/built-ins/RegExp/property-escapes/` and `test/built-ins/RegExp/unicodeSets/`; I1 under
`test/language/identifiers/` and I3 under `test/built-ins/RegExp/named-groups/` and
`test/language/literals/regexp/`; W1 under `test/language/white-space/` and
`test/built-ins/RegExp/escape/`; C1 and C4 to C8 under `test/built-ins/String/prototype/toLowerCase/`,
`toUpperCase/` and `test/intl402/String/prototype/`; N1 under `test/built-ins/String/prototype/normalize/`
and `test/intl402/String/prototype/localeCompare/`; G1 to G4 under `test/intl402/Segmenter/`. A row
elsewhere is classified by the same rows, or is a regression.

## Part B - Broiler.JS's measurement (template T5)

```rule
evidence class: measurement; in Measurement.md's classes, an acceptance comparison when the lane is a controlled lane as its section "Controlled lanes and effective configuration" requires, and smoke otherwise; a smoke run is void for this part and is not judged
population: the six workloads of UnicodeBenchmarks (Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks/UnicodeBenchmarks.cs) as committed with this rule - Normalize (String.prototype.normalize in its four forms), Casing (toUpperCase, toLowerCase, and toLocaleUpperCase and toLocaleLowerCase with a Turkic and a Lithuanian locale) and Identifiers (the compiler's identifier scanning outside ASCII), each with its control, NormalizeControl, CasingControl and IdentifiersControl; the benchmark project built in Release under JIT with its own runtime settings; one machine and one RID, both named in the sub-bundle's manifest
arms: B the base, a worktree at this rule's commit; A the commit that lands SJL-7.5's cut-over, with UnicodeBenchmarks.cs and the benchmark Program.cs as this rule's commit has them; A-again the A arm measured a second time under another lane name, the A/A lane; each arm's benchmark and engine assemblies named by hash in the manifest, which is written and made read-only before any lane runs
configuration: in every repetition, B, A and A-again each run once, in an order that rotates by repetition, so the arms are balanced and process-isolated as Measurement.md's acceptance comparison asks; one run is one launch of the benchmark executable with --filter on this class only, BROILER_BENCHMARK_PROFILE=baseline and a build timeout ample for the lane, which runs all six workloads, each in BenchmarkDotNet's own child process, under the baseline job BenchmarkConfiguration.cs fixes at this rule's commit (its launch, warm-up and iteration counts); the host's ICU loaded, not invariant globalization, since at arm B this engine's locale casing throws without it; seven repetitions per arm, every one retained
condition: before and after every run, the arm's --unicode-answers prints every workload's answer; each answer equals the same workload's answer at arm B's first check; a run whose arm's answers differ fails the condition
configuration check: the configuration object --unicode-answers prints (framework, RID, architecture, GC mode, tiering, PGO and ReadyToRun settings, vector acceleration, processor count, the invariant-globalization variable, the current culture and the two probes of the host's globalization data), and in each run's full JSON report BenchmarkDotNet's host environment record (runtime and SDK versions, architecture, processor, JIT) with each workload's hardware-intrinsics record, are identical across arms, lanes and repetitions
quantity: Q(workload, arm) = the median, over the arm's seven runs, of BenchmarkDotNet's reported median time per operation of that workload in that run
noise floor: N(workload) = |Q(workload, A) - Q(workload, A-again)|
comparison: D(workload) = Q(workload, A) - Q(workload, B)      (positive: the after arm slower), against N(workload), for Normalize, Casing and Identifiers; one engine's after arm against the same engine's base arm, never against another engine
attribution, retained beside D and deciding nothing: E(workload, arm) = Q(workload, arm) - Q(control, arm)
guardrails, reported for every workload and arm and deciding nothing: BenchmarkDotNet's allocated bytes per operation and its collection counts
admitted difference classes: none; a measurement admits no class, and a difference within the A/A floor is below resolution rather than admitted
MET if and only if all of:
  1. the lane is a controlled lane, recorded as Measurement.md requires;
  2. every run's condition passes, before and after, for every arm;
  3. every configuration check is identical across arms, lanes and repetitions;
  4. every run completed every workload, and BenchmarkDotNet reported no workload with issues;
  5. for Normalize, Casing and Identifiers, D <= N.
NOT MET otherwise.
readings:
  - a workload whose |D| <= N is below resolution, and meets clause 5;
  - clause 5 has no tolerance but the A/A lane: this rule states no percentage, so the after arm is held to the base within this lane's own noise on that workload;
  - a failed condition is a NOT MET, not a re-run; an interrupted collection may be run again from the start, with the stopped run retained beside it and named as stopped;
  - this part returns MET or NOT MET, not one of Measurement.md's candidate decisions, because it makes no performance claim; a NOT MET under clauses 1 to 4 is what Measurement.md calls invalid-run, and under clause 5 leaves JSD-0031's fifth reason standing for this engine, which SJL-7 answers before it closes.
```

**Why the answers count operations.** The cut-over corrects results this engine gets wrong today: a lone
surrogate that throws in `normalize` (N1), Unicode 16's decompositions (N2), casing that reads the host
culture (C8) or a list of locales (C7), and identifiers that admit Pattern_Syntax (I1). An answer built
from the results would change for a correction, and the condition would refuse the very change the
milestone exists to make, which is how SJL-3's measurement of Broiler.VM failed its condition. Each
workload's answer therefore counts the operations it completed: the same at the base and after, and still
failed by a lane that throws or stops early. The results themselves are judged by part A, by SJL-7.3's
vectors and by the owner's test files, not here.

**Why the host's ICU is part of the configuration.** Until the cut-over, this engine's normalization and
casing are the host's: `string.Normalize` and `CultureInfo`. Under invariant globalization its locale
casing throws, so arm B's answers could not be taken, and its normalization does nothing, so arm B would
price an empty operation. `--unicode-answers` prints the invariant-globalization variable and two probes of
the host's data, the Turkic upper case of `i` and the length of `e` U+0301 in NFC, and the configuration
check holds them identical, so that both arms run on one host's data.

**What SJL-3 taught this part.** Bundle `sjl-3-001`'s part B was void: this repository's lane was not a
controlled lane, and a smoke run is not judged. Nothing here changes that: if the lane SJL-7.6 has is not a
controlled lane, this part is void again, and Broiler.VM's JSD-0074 says what follows, that JSD-0031's fifth
reason stays open for this engine and the area is neither met nor refused on it.

**What part B does not decide.** A speed claim of any kind, in either direction. A comparison with
Broiler.VM, whose own measurement is judged by its own rule in its own repository. Anything about a RID or a
machine other than the one named.

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory, quoting
this one whole and saying it was written afterwards; its decision is then weaker evidence. The workloads
are not changed after they are committed.

## What this file does not decide

- **The owner's clauses** (1 to 4): its conditions, its API, its test files and its byte-for-byte
  generation. Broiler.Unicode's bundle `sjl-7-001` judges them, with the library-level reference arms.
- **Engine-level vectors.** SJL-7.3 adds them, and the changelog lists each correction they show.
- **Broiler.VM's test262 parity, fuel parity or measurement.** Each is judged by Broiler.VM's own rule.
- **The deletion list, the host Unicode APIs and the corrections list** (clauses 5, 6 and 8). Each is judged
  by its own mechanism: a search of the source at the after commit, and the changelog.
- **Case folding under `/i` without `u` and the translator's line terminators** (rows F2 and W5). They are
  the .NET translator's, which SJL-11 retires.
