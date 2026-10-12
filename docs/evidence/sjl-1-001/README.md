# Bundle sjl-1-001: Broiler.JS against the shared conformance vectors

**Milestone:** SJL-1 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-1-shared-conformance-vectors.md`: its
home clauses 1 to 3 and the Broiler.JS half of clause 6. **Rule:** [`decision-rule.md`](decision-rule.md),
committed at SJL-0 before any of what it judges existed. **Collected:** 2026-10-12. **Owner:** the
Broiler.JS maintainer. **Reviewer:** none; every role is one person, and nothing here was read by
a second one.

## Decision

**MET**, read against the rule as written. One judgement went into that reading, and it is stated
under "What a reader should know" rather than left inside the verdict: seven vectors' results are
`abort:` (the engine did not finish: six hangs, and one host `ArgumentException` that escaped it),
and this README counts an abort as the engine's result, which a divergence field may classify, not as
a harness fault, which nothing may. A reader who counts those seven as harness faults reads **NOT
MET**.

| Rule clause | What the bundle holds |
|---|---|
| Validator accepted the named revision | [`tests.log`](tests.log): `The_corpus_validates` passed on revision 3; `Every_vector_has_a_source_independent_of_both_engines` passed |
| Validator refused its malformed negative-control files | [`tests.log`](tests.log): `A_negative_control_is_refused_for_its_stated_reason`, twelve cases, each refused for the one reason its file name states and no other |
| Validator refused a vector with engine-observed provenance only | [`tests.log`](tests.log): the `engine-observed.vectors` control, `A_vector_whose_only_provenance_is_engine_observed_is_refused`, and a real vector changed to `prov: observed:broiler-vm` |
| Every vector of the population has a result | [`run-revision-3.tsv`](run-revision-3.tsv): 10,132 rows, one per vector of revision 3 |
| Every difference is in class (d) and listed | 700 differences, each on a vector classed `divergence` with a `divergence.broiler-js` field that names its reason and the plan that answers it; listed in [`classification.tsv`](classification.tsv) and, verdict by verdict, in the run |
| Every match in class (i) is listed | 122 `impl-defined` vectors, all matching their `per-engine.broiler-js` expectation; listed by identifier and class in the run |
| No expected value is engine-observed only | Every vector's provenance names the specification and `ref:numref@1`, the exact reference computation; [`reference-tests.log`](reference-tests.log) is its own test run, which checks it against CPython |
| The negative control was reported unclassified | [`negative-control.tsv`](negative-control.tsv): in a copy of the corpus, `numbers/to-fixed/0001` was changed to expect `"00"`; the runner, given the copy, reported it as the one `difference` of that file and classified nothing |

## The runs

| Run | Corpus | Match | Classified divergence | Difference |
|---|---|---|---|---|
| First, [`run-revision-1.tsv.gz`](run-revision-1.tsv.gz) | Revision 1, commit `966140a` | 9,432 | 0 | 700 |
| Judged, [`run-revision-3.tsv`](run-revision-3.tsv) | Revision 3, commit `593f2c5` | 9,432 | 700 | 0 |

Every rendered outcome of the judged run is byte-identical to the first run's; only the corpus
changed. Revision 2 classified these 700; revision 3 added Broiler.VM's classifications and changed
nothing this engine is compared with.

**What the 700 are**, all engine defects, all classified by `scripts/conformance/number-divergences.txt`:

| Reason | Vectors | Answered by |
|---|---|---|
| `Number::toString` at a radix other than 10: more digits than the shortest | 233 | SJL-3 |
| The same: digits that do not denote the value (the fraction expanded in double arithmetic) | 124 | SJL-3 |
| The same: wrong at 2^63 or more (the integer part through a 64-bit integer) | 238 | SJL-3 |
| Hexadecimal, octal and binary integers beyond 2^53, as literals and strings, not rounded to nearest, ties to even | 62 | SJL-3 |
| Decimal significands beyond 309 digits: `StringToNumber` and `parseFloat` hang (6) or give NaN (12) | 18 | SJL-3 |
| Numeric-literal early errors not raised: `08` and `09` in strict code, `07.5`, `1e`, `01n`, `08n`, and `.5n` as a TypeError | 13 | SJL-14 |
| `parseInt` trims U+0085 | 7 | SJL-3 |
| Argument edges: `toString` with a radix of 0, `null`, `-0` or NaN, and `toPrecision(100.5)` | 5 | SJL-3 |

## What a reader should know

- **The `abort:` form was written during this milestone, after the first hang was seen.** The first
  attempt at a full run stalled on `numbers/parse-float/0777`: `parseFloat` of a 366-digit
  significand starts from `+Infinity` and steps one unit in the last place at a time. The corpus's
  SCHEMA.md section 7 then gained `abort:` (the engine did not complete, by a host exception or the
  runner's time limit), which a divergence field may classify, and `fault:` (the runner itself
  failed), which nothing may. Both were written before the first complete run, and no run produced a
  `fault:`. The rule's "runner refusal or harness fault" is read as the second. The six hangs are the
  engine's and are classified `long-significand-hang`; the seventh abort, `toString(NaN)` throwing a
  .NET `ArgumentException` out of the engine, is classified `radix-argument`.
- **Why a child process.** `ControlledExecution` was tried first and could not abort the hung
  conversion; its canceller spun for as long as it was left. The runner therefore evaluates vectors
  in a child process of the test assembly, one fresh `JSContext` per vector as before, and ends the
  process at 5 s; the next vector starts a new one. A timing pass before the limit existed found no
  vector slower than 0.4 s among the 7,570 it reached before the first hang.
- **The library-level runner** ran against the empty library, and every vector's outcome was
  `status: not-implemented` (`Against_the_empty_library_every_vector_is_not_implemented`). The rule
  decides nothing about it.
- **The corpus is generated.** Its expected values come from `scripts/conformance/numref.py`, an
  independent computation in exact arithmetic, checked against CPython's float conversion and
  shortest `repr`; the generator's test fails when the committed corpus differs from what it writes.
  No expected value was changed by any classification.
- **Nothing here compares the two engines.** Broiler.VM's run is in Broiler.VM's own bundle, judged
  by its own rule.

## Files

| File | What it is |
|---|---|
| [`decision-rule.md`](decision-rule.md) | The rule, committed at SJL-0 |
| [`identity.txt`](identity.txt) | The commits, the corpus digest, the toolchain and the machine |
| [`tests.log`](tests.log) | The test project's run on revision 3: validator, controls, library-level runner, renderer, the engine-level run and its negative control |
| [`run-revision-3.tsv`](run-revision-3.tsv) | The judged run: per vector, its identifier, class, expected outcome, rendered outcome and verdict |
| [`run-revision-1.tsv.gz`](run-revision-1.tsv.gz) | The first run, before any classification |
| [`classification.tsv`](classification.tsv) | Every difference of the first run, its verdict on revision 3 and its reason |
| [`negative-control.tsv`](negative-control.tsv) | The runner's report on the mutated copy's file, its difference line kept |
| [`reference-tests.log`](reference-tests.log) | The reference computation's tests, the generator's included |
