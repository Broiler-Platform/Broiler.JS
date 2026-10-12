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
  `ResultStatus`, `Failure`, `LengthLimit` and `DepthLimit`, and no algorithm.

### Changed

None.

### Observable changes

None.

### Data

None.

## 0.0.1-probe.1 — not published

The probe pre-release of milestone SJL-2.13: `Broiler.ECMAScript.Algorithms` holding the contract
types listed above under Unreleased. It is packed for Broiler.VM's admission probe and reaches
Broiler.VM through that repository's committed feed. **It is never pushed to nuget.org**: no
Broiler.VM release depends on it, and its version is below every version this stream will publish.
