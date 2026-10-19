# The shared conformance vectors

**The corpus of the shared JavaScript libraries programme.** A vector pins one operation's outcome for
one input, with where that outcome comes from. The corpus runs against both engines, Broiler.JS here
and Broiler.VM's JavaScript profile there, and later against each shared library itself. It tells
each engine where it is wrong today, before any code moves, so that a cut-over's corrections can be
told apart from its regressions.

It is **not test262**: it complements test262 where an operation's whole input space matters, and it
never replaces test262 as the oracle. **Agreement between the two engines on a vector is not evidence
that the vector is right**; only its provenance is.

Created 2026-10-12 at milestone SJL-1 of the programme. The milestone, the area design and the
decision rules live in the Broiler.VM repository: `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-1-shared-conformance-vectors.md`
and `Broiler.VM/docs/javascript-shared-libraries/areas/conformance-vectors.md`; this repository's
rule for its own engine is [`docs/evidence/sjl-1-001/decision-rule.md`](../../docs/evidence/sjl-1-001/decision-rule.md).

## Layout

| Path | What it is |
|---|---|
| [`SCHEMA.md`](SCHEMA.md) | The format, the classes, the provenance kinds, and the one rendering rule both engine-level runners follow |
| [`REVISIONS.md`](REVISIONS.md) | The corpus revision, and every dated classification change |
| [`prelude.js`](prelude.js) | What a runner evaluates in each fresh realm before a vector's script |
| [`sources/`](sources/) | The pins of the specification edition and the Unicode data the provenance names |
| `numbers/` | The number area: `Number::toString` at every radix, `toFixed`, `toExponential`, `toPrecision`, `StringToNumber`, `parseFloat`, `parseInt`, numeric literal values, and array-index and canonical numeric keys |
| `numerics/` | The numerics area: the integer conversions, Float16, the Math kernels each engine implements itself, the rounding modes (through Temporal), and BigInt arithmetic, text, `asIntN`, `asUintN` and its conversion to and comparison with Number |
| `text/` | The text area: TrimString, well-formedness, CodePointAt and AdvanceStringIndex; StringPad and the relative-index normalisation; replacement templates; the URI codecs with `escape` and `unescape`; the base64 and hex codecs; and JSON's grammar, its quoting, its gap, its line ending and `JSON.rawJSON` *(added 2026-10-14, at corpus revision 10)* |
| `date/` | The Date area: MakeTime, MakeDay, TimeClip, MakeFullYear and the setters' range and order; field extraction; the Date Time String Format and each engine's dialect; the renderings; local time through relations that hold in every time zone; and the ISO calendar's civil-day helpers (through Temporal); and, from revision 16, strings outside the format on which the engines' readings differ *(added 2026-10-15, at corpus revision 14; the last clause at revision 16, the same day)* |
| `unicode/` | The Unicode area: `normalize` over Unicode's NormalizationTest.txt and the correspondence's cells; `toUpperCase`, `toLowerCase` and the language-sensitive `toLocaleUpperCase` and `toLocaleLowerCase`, and case folding through RegExp's `i` flag; `\p{…}` acceptance and membership under `u` and `v`, properties of strings included; identifier acceptance through escaped identifiers, and white space; and `Intl.Segmenter` over Unicode's three break tests *(added 2026-10-16, at corpus revision 18)* |
| `time-zones/` | The time-zone area: offsets, next and previous transitions, the instants of a wall-clock time and each disambiguation, start of day and the hours in a day, through Temporal, over the data home's pinned tz database under decision SJL-D-20, at instants on both sides of every policy difference and correction of the area's verdict (the release, backzone, extrapolation after 2037, CLDR primaries, `Factory`, lookup by ASCII case only, offsets with ASCII digits); the identifiers and primaries, through Temporal and ECMA-402; and a sample of ordinary transitions, gaps and overlaps *(added 2026-10-17, at corpus revision 21)* |
| `temporal/` | The Temporal area: the ISO 8601 and RFC 9557 grammar through every goal's `from()`, duration, calendar, time zone, offset and `relativeTo` strings, with every accept and refuse case of the area design's table and of its rows G1 to G24; duration rounding in every mode and every increment of every unit, with and without `relativeTo`, nudging, bubbling and the shifted window, totals and comparison; the records' limits, and balancing, regulation and addition at them; `until` and `since` in every type and calendar; and the calendars: identifiers, month codes, eras, month-day reference dates, the formula calendars at their edges and the table calendars at their tables' edges where Table 1 publishes them *(added 2026-10-18, at corpus revision 24)* |
| [`controls/`](controls/README.md) | Malformed files the validator must refuse. **Not part of the corpus.** |

## Running it

```bash
# The reader, the validator and the library-level runner are the shared Conformance library's tests:
dotnet test Broiler.ECMAScript/Broiler.ECMAScript.Conformance.Tests -c Release
# Broiler.JS's engine-level runner:
dotnet test Broiler.JS/Broiler.JavaScript.Vectors.Tests -c Release
# With a report of every vector's verdict, as a bundle retains it (a path relative to the
# repository, or absolute):
BROILER_VECTORS_REPORT=vectors-report.tsv dotnet test Broiler.JS/Broiler.JavaScript.Vectors.Tests -c Release
# The corpus test alone, in four evaluating processes, as CI's engine vectors job runs it; the detailed
# console logger shows the line with N, the elapsed time and the slowest vector's time:
BROILER_VECTORS_WORKERS=4 dotnet test Broiler.JS/Broiler.JavaScript.Vectors.Tests -c Release --filter Category=EngineCorpus --logger "console;verbosity=detailed"
# Regenerating the number and numerics areas, and the reference computations' own tests:
python3 scripts/conformance/generate_number_vectors.py --proplist <UCD 17.0.0 PropList.txt> --out conformance/vectors/numbers
python3 scripts/conformance/generate_numerics_vectors.py --out conformance/vectors/numerics
python3 scripts/conformance/generate_text_vectors.py --out conformance/vectors/text
python3 scripts/conformance/generate_date_vectors.py --out conformance/vectors/date
# The Unicode area reads the pinned UCD 17.0.0, which the corpus does not archive: --ucd is the directory
# sources/unicode.pin's paths are under, normally Broiler.Unicode's data/unicode/pins/. -I, as for any downloaded data.
python3 -I scripts/conformance/generate_unicode_vectors.py --ucd <Broiler.Unicode>/data/unicode/pins --out conformance/vectors/unicode
# The time-zone area compiles the data home's pinned tz database with the host's zic (tzref.py); its tests are skipped
# where zic is absent.
python3 -I scripts/conformance/generate_time_zone_vectors.py --out conformance/vectors/time-zones
# The Temporal area runs the proposal's own polyfill at e8cc03fc under Node 22.22.0, set up as the generator's header
# says (its lib/ and node_modules/ in scripts/conformance/temporalref/, or a directory given by --polyfill); its tests
# that regenerate the area read BROILER_TEMPORAL_POLYFILL and BROILER_NODE and are skipped where the reference is absent.
python3 -I scripts/conformance/generate_temporal_vectors.py --out conformance/vectors/temporal --node <Node 22.22.0>
python3 -m unittest discover -s scripts/conformance/tests
# The Unicode area's tests that read the pinned data look for it in BROILER_UNICODE_PINS, or in a Broiler.Unicode
# checkout beside this repository, and are skipped where it is absent.
BROILER_UNICODE_PINS=<Broiler.Unicode>/data/unicode/pins python3 -I -m unittest discover -s scripts/conformance/tests
```

The reader, the validator and both runners' engine-neutral parts are the test-only
`Broiler.ECMAScript.Conformance` library in the shared folder,
[`Broiler.ECMAScript/`](../../Broiler.ECMAScript/README.md). It takes each file's bytes from its
caller and reads nothing from disk, so the same code runs in the shared tests, in Broiler.JS's
engine-level runner and in the Native AOT probe. It is not packed at this version. The corpus does
not move.

**`BROILER_VECTORS_WORKERS`** sets how many evaluating processes Broiler.JS's corpus test uses: a
whole number above 1, or one process when it is unset or anything else. With N processes the corpus
is cut into blocks of 512 vectors, and worker w takes blocks w, w + N, w + 2N and so on, in order, with
an engine and a process of its own; the results are merged in corpus order. A vector that finds no
completion within the runner's 5 s in that pass is run again alone in a new process, and that result is
kept, so a vector that never completes still aborts. The report is therefore byte for byte the one a
single process writes, unless a vector finds no completion in the single process's run: there one
process keeps the abort, where N processes keep what the vector gives alone, and the same goes for a
vector that only sometimes completes. Each run writes a line with N, its elapsed time, its slowest
vector's time and the vectors run again alone, to the test's output and to standard error, which
`dotnet test` shows from normal verbosity up; CI's engine vectors job raises a warning when any vector
was run again. **Retained evidence runs use one process**: such a run is started with the variable
unset (`env -u BROILER_VECTORS_WORKERS`), and a collector removes it from the environment it gives
`dotnet test`, as it removes `LANG`. A run retained with more would need a dated note in the
configuration clause of bundle sjl-1-001's decision rule. *(Added 2026-10-19, when CI's engine vectors
job began to run the corpus in four processes.)*

*(Revised 2026-10-12, at milestone SJL-2.10, when the shared folder was created. It read: "The
reader and validator are engine-neutral and live, for now, in the Broiler.JS test project beside the
runner. They move into the test-only `Broiler.ECMAScript.Conformance` package when milestone SJL-2
creates the shared folder; the corpus does not move." The first two commands above read "# The
validator and Broiler.JS's engine-level runner are tests of one project:" and "dotnet test
Broiler.JS/Broiler.JavaScript.Vectors.Tests -c Release".)*

## How a vector is added, reviewed and retired

1. **A vector is added with its provenance**, before the shared code it judges lands: every area
   milestone adds its vectors first. Its outcome comes from the specification at the pinned edition,
   from a test262 test at the pinned revision, from a pinned data file, or from an independent
   computation in exact arithmetic under `scripts/conformance/`. **No expected value is ever taken
   from either engine**, and the validator refuses a vector whose provenance is only
   `observed:` an engine.
2. **A generated area is regenerated, not edited.** The number area is written by
   `scripts/conformance/generate_number_vectors.py`; its test fails when the committed files differ
   from what the generator writes. Its classifications are the generator's input too:
   `scripts/conformance/number-divergences.txt` lists each divergence by vector, engine and reason.
3. **A change raises the corpus revision** and adds a dated row to [`REVISIONS.md`](REVISIONS.md).
   New vectors carry the new revision in `since`. Identifiers are never reused.
4. **The first run of each engine classifies every difference**, as milestone SJL-1.5 asks:
   - an **engine defect** becomes a `divergence.<engine>` field, with the reason and a link to the
     plan that answers it;
   - a **vector defect** is fixed in the next revision, and the run is repeated;
   - an **implementation-defined** difference moves into the vector as `impl-defined`, with a
     `per-engine` expectation taken from that engine's plan.
5. **A vector an engine fails is never deleted or reclassified to make it pass.** A classification
   changes only by a dated entry in a later revision. A `divergence` field is retired the same way,
   when the engine's cut-over corrects it and lists the correction in its changelog.
6. **Review.** Each revision is a reviewed change to this repository. One person holds every role
   the programme names today, so no revision has been read by a second reviewer, and none claims it.

## Provenance, and the sources' terms

- **The specification.** Expected values cite clauses of ECMA-262 at the revision
  [`sources/ecma262.pin`](sources/ecma262.pin) names, the edition Broiler.VM's JavaScript profile
  archives. The corpus quotes no specification text; it names clauses by their identifiers.
- **Unicode.** The number area's whitespace set takes its Space_Separator members from UCD 17.0.0's
  `PropList.txt` ([`sources/unicode.pin`](sources/unicode.pin)). The corpus copies no Unicode data
  file; it states which code points the set holds.
  **The Unicode area** (revision 18) reads the UCD 17.0.0 files, the two emoji 17.0 sequence files and
  ECMA-262's three property tables that the same pin names, by the lengths and digests of Broiler.Unicode's pin, and
  copies none of them: its expected values are Unicode's own test files where they cover an input, and otherwise
  `scripts/conformance/unicoderef.py`'s, a marked copy of Broiler.Unicode's reference computation, written from the
  standards and tested against those files.
- **Inputs.** The number area's inputs are the populations milestone SJL-1.4 names: special values,
  the exponent range, the subnormal boundaries, seeded random doubles (SplitMix64, with the seeds in
  the generator), the argument edges and exact ties of each method, and the inputs Broiler.VM's
  differential probes and Broiler.JS's round-trip tests exercise. Those probes' and tests' answers are
  never used.
- **The numerics area** (revision 6) follows the same rule: its inputs are milestone SJL-4.2's populations, with seeded random values (SplitMix64, the seeds in the generator), and every expected value is `scripts/conformance/numericsref.py`'s, in exact arithmetic. Its rounding vectors run through Temporal, which the pinned ECMA-262 edition does not contain, so their provenance is that computation alone; the generator names the proposal's operations it follows.
- **Time zones.** The time-zone area (revision 21) reads the data home's pinned tz database and CLDR file, `Broiler.ECMAScript/data/time/tzdb/` and `cldr/`, by their pins, and copies neither: every expected value is `scripts/conformance/tzref.py`'s (`ref:tzref@1`), which compiles the pinned release with the host's `zic`, reads the compiled files with its own reader and applies decision SJL-D-20's policy, and shares no code with the data home's generator or the algorithms library it judges.
- **Temporal.** The Temporal area (revision 24) cites the Temporal proposal's specification text and the Intl era and
  month code proposal's at the revisions both engines follow, by `proposal:` entries and the pins
  [`sources/temporal.pin`](sources/temporal.pin) and [`sources/intl-era-monthcode.pin`](sources/intl-era-monthcode.pin),
  and archives and quotes neither. Every expected value is the answer of the proposal's own polyfill at
  `e8cc03fc` (`ref:temporalref@1`, decision D10 of bundle `sjl-10-001`), run under Node 22.22.0 by
  `scripts/conformance/temporalref.mjs` with its npm dependencies at the versions `scripts/conformance/temporalref/package-lock.json`
  pins; the corpus holds the answers, not the polyfill. Where the pinned text and the polyfill disagree the text rules
  (decision D7): the vector takes the text's answer and records the polyfill's as `overruled:temporalref@1=<outcome>`.
  Where the polyfill reckons a calendar through ICU4C or JavaScript's Date and so is not the specification's, the input
  is not vectored; the generator's header lists each such kind.
- **test262.** No vector of revision 1 is derived from a test262 test. A vector that is will cite the
  test with `test262:<path>`, and the corpus will then carry test262's licence notice.
