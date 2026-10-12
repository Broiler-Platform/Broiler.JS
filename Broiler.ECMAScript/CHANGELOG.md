# Changelog — the shared JavaScript libraries

Every release of the `Broiler.ECMAScript.*` packages has an entry here, newest first. The packages
follow semantic versioning under [convention 16](docs/conventions.md):

- a public API removal or a binary-breaking change is a major version;
- a new API is a minor version;
- a correction toward the specification is a minor version, with its own entry under
  **Observable changes**;
- a data-version change is a minor version, with its own entry under **Data**.

Each entry has the headings **Added**, **Changed**, **Observable changes** and **Data**, and says
"none" under a heading with nothing to report. A release without an entry here is not published.

## Unreleased

### Added

- `Broiler.ECMAScript.Algorithms`: the contract types of the conventions, `WorkCharge`,
  `ResultStatus`, `Failure`, `LengthLimit` and `DepthLimit`.
- `Broiler.ECMAScript.Algorithms`: the number-conversion area, namespace
  `Broiler.ECMAScript.Numbers` (milestone SJL-3.3; adapter guide
  [`docs/adapter-guide-numbers.md`](Broiler.ECMAScript.Algorithms/docs/adapter-guide-numbers.md)):
  - `NumberText`: `Number::toString` at radix 10 (`ToString`, `TryFormat`) and at every radix from 2
    to 36 (`ToRadixString`), and the digits of `toFixed`, `toExponential` and `toPrecision`;
  - `NumberParsing`: `StringToNumber`, `ParseFloat` and `ParseInt`, correctly rounded at every
    radix, and the `StrWhiteSpaceChar` set (`IsStrWhiteSpaceChar`, `TrimStrWhiteSpace`);
  - `NumericLiterals.Evaluate`: the value of a numeric literal in every form, BigInt literals under
    the caller's width limit, and every refusal as a `Failure` with a message identifier;
  - `NumericKeys`: `TryParseArrayIndex` and `IsCanonicalNumericString`.

  Every number vector of corpus revision 3 passes at library level. Ryu (Apache-2.0) is ported for
  the shortest radix-10 digits; see `THIRD_PARTY_NOTICES.md`.

### Changed

None.

### Observable changes

None: the area is new in this stream. What each engine observes at its cut-over is listed in that
engine's own records (Broiler.JS's changelog; Broiler.VM's JSC entries).

### Data

- Unicode 17.0.0 (`NumberParsing.UnicodeVersion`): the `Space_Separator` members of the
  `StrWhiteSpaceChar` set, generated from the pinned `PropList.txt` by
  `scripts/conformance/generate_white_space_table.py` (convention 13, revised 2026-10-12).

## 0.0.1-probe.1 — not published

The probe pre-release of milestone SJL-2.13: `Broiler.ECMAScript.Algorithms` holding the contract
types listed above under Unreleased. It is packed for Broiler.VM's admission probe and reaches
Broiler.VM through that repository's committed feed. **It is never pushed to nuget.org**: no
Broiler.VM release depends on it, and its version is below every version this stream will publish.
