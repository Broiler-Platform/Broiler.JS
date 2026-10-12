# Corpus revisions

The corpus has one revision number, raised by every change that adds, retires or reclassifies a
vector. A vector's `since` field names the revision that added it. **A classification change is a
dated entry here**, in the revision that makes it, and never an edit to an earlier entry.

| Revision | Date | Change |
|---|---|---|
| 1 | 2026-10-12 | The number area (milestone SJL-1.4): Number::toString at every radix with its argument edges, `toFixed`, `toExponential` and `toPrecision` with their argument edges and exact ties, `StringToNumber`, `parseFloat`, `parseInt`, the values of numeric literals, and array-index and canonical numeric keys. Every expected value is computed by `scripts/conformance/numref.py` (`ref:numref@1`) from ECMA-262 at `0248456c`. No vector is classed `divergence`. |
