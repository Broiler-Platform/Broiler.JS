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
# Regenerating the number area, and the reference computation's own tests:
python3 scripts/conformance/generate_number_vectors.py --proplist <UCD 17.0.0 PropList.txt> --out conformance/vectors/numbers
python3 -m unittest discover -s scripts/conformance/tests
```

The reader, the validator and both runners' engine-neutral parts are the test-only
`Broiler.ECMAScript.Conformance` library in the shared folder,
[`Broiler.ECMAScript/`](../../Broiler.ECMAScript/README.md). It takes each file's bytes from its
caller and reads nothing from disk, so the same code runs in the shared tests, in Broiler.JS's
engine-level runner and in the Native AOT probe. It is not packed at this version. The corpus does
not move.

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
- **Inputs.** The number area's inputs are the populations milestone SJL-1.4 names: special values,
  the exponent range, the subnormal boundaries, seeded random doubles (SplitMix64, with the seeds in
  the generator), the argument edges and exact ties of each method, and the inputs Broiler.VM's
  differential probes and Broiler.JS's round-trip tests exercise. Those probes' and tests' answers are
  never used.
- **test262.** No vector of revision 1 is derived from a test262 test. A vector that is will cite the
  test with `test262:<path>`, and the corpus will then carry test262's licence notice.
