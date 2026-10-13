# Evidence bundle SJL-4-001 (Broiler.JS) - the numerics area

**Milestone:** SJL-4 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-4-numerics.md`: its home clauses 1 to 3 and
its Broiler.JS clauses 4 to 7. **Rules:** [`decision-rule.md`](decision-rule.md) (test262 per path) and
[`reference-arms.md`](reference-arms.md) (the library-level comparison), both committed at SJL-4.1
before any code they judge. **Area verdict:** [`SharedLibraries.md`](../../roadmap/SharedLibraries.md),
"Area verdicts", with Broiler.VM's JSD-0067 and JSD-0068. **Owner:** the Broiler.JS maintainer; one
person holds every role (the programme's EX-30, recorded in Broiler.VM). **Reviewer:** none, and nothing
here has been read by anyone but its author.

**State:** collection in progress. Nothing here is a verdict until its section says so. **Taken:** the
engine-level vectors of SJL-4.2 (section 1). The rest is added as SJL-4.3 to SJL-4.6 take it. Broiler.VM
keeps its own bundle of the same name for its own half, judged by its own rule; neither bundle reads the
other's runs.

## 1. Engine-level vectors (SJL-4.2)

The shared corpus gains the numerics area at revision 6 and classifies each engine's first run on it at
revision 7 ([`REVISIONS.md`](../../../conformance/vectors/REVISIONS.md)).

| File | What it is |
|---|---|
| [`engine/run-revision-6.tsv.gz`](engine/run-revision-6.tsv.gz), [`engine/run-revision-6.log`](engine/run-revision-6.log) | This engine's first run on the area, at revision 6 (`e3892d6`), through `Broiler.JS/Broiler.JavaScript.Vectors.Tests` in Release: 22,634 vectors, 22,576 match, 4 classified divergences (the number area's), 54 unclassified differences. The test fails, as a first run on unclassified vectors does |
| [`engine/run-revision-7.tsv.gz`](engine/run-revision-7.tsv.gz), [`engine/run-revision-7.log`](engine/run-revision-7.log) | The run at revision 7 (`b725bb0`), which classifies the 54: 22,576 match, 58 classified divergences, 0 unclassified differences, and the test passes |
| [`engine/retired-divergences.tsv`](engine/retired-divergences.tsv) | The 391 `divergence.broiler-vm` fields of the number area that revision 6 retires, under their reasons: Broiler.VM's SJL-3.4 cut-over corrects them (its JSD-0066 section 4 and its bundle `sjl-3-001`) |

The 54 differences, under the reasons of
[`scripts/conformance/numerics-divergences.txt`](../../../scripts/conformance/numerics-divergences.txt):
18 `asUintN` results at widths of 2^32 or more, 9 cube roots, 5 BigInt powers past a 32-bit exponent,
5 BigInt property keys beyond 2^32 - 1 or below 0, 5 cases of `1 ** NaN` and of 1 to an infinity,
5 hypotenuses, 5 `expm1` and `log1p` results, and 2 hexadecimal StringToBigInt texts of 262,144 digits
that do not complete within the runner's five seconds. Every one is answered by SJL-4.5. Four
differences between the engines that the area verdict did not classify are Broiler.VM's JSD-0068.
