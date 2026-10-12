# Corpus revisions

The corpus has one revision number, raised by every change that adds, retires or reclassifies a
vector. A vector's `since` field names the revision that added it. **A classification change is a
dated entry here**, in the revision that makes it, and never an edit to an earlier entry.

| Revision | Date | Change |
|---|---|---|
| 1 | 2026-10-12 | The number area (milestone SJL-1.4): Number::toString at every radix with its argument edges, `toFixed`, `toExponential` and `toPrecision` with their argument edges and exact ties, `StringToNumber`, `parseFloat`, `parseInt`, the values of numeric literals, and array-index and canonical numeric keys. Every expected value is computed by `scripts/conformance/numref.py` (`ref:numref@1`) from ECMA-262 at `0248456c`. No vector is classed `divergence`. |
| 2 | 2026-10-12 | Classifies every difference of Broiler.JS's first engine-level run, on revision 1: 700 vectors become `divergence` with a `divergence.broiler-js` field. 595 are `Number::toString` at a radix other than 10: 233 longer than the shortest digits, 124 whose digits do not denote the value, 238 wrong at a magnitude of 2^63 or more. 105 others: 62 hexadecimal, octal and binary integers beyond 2^53 not rounded to nearest, ties to even, as literals and strings; 18 decimal significands beyond 309 digits that hang or give NaN; 7 `parseInt` inputs that trim U+0085; 5 argument edges of `toString` and `toPrecision`; 13 numeric-literal early errors not raised. The reasons, with the plans that answer them (Broiler.JS SharedLibraries items SJL-3 and SJL-14), are in `scripts/conformance/number-divergences.txt`. No expected value changed and no vector was added or removed. |
