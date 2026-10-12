# Third-party notices

Broiler.JS is derived in part from [Yantra JS](https://github.com/yantrajs/yantra),
an Apache License 2.0 JavaScript engine for .NET. Broiler.JS retains Yantra JS
contribution history and attribution; the Apache License text is included in
[`LICENSE`](LICENSE).

Broiler.JS has diverged substantially and is maintained independently. The Yantra JS
authors have not reviewed, sponsored, or endorsed Broiler.JS. The upstream project name
is used only to provide accurate provenance.

Other third-party dependencies retain their own copyright and license terms as identified
in source files, package metadata, or accompanying notices.

## The shared JavaScript libraries (`Broiler.ECMAScript.*`)

The packages built from [`Broiler.ECMAScript/`](Broiler.ECMAScript/README.md) carry this file. At this
version they contain no third-party code and no data:

- `Broiler.ECMAScript.Algorithms` holds the contract types written for the shared libraries
  programme, Apache-2.0, and no code derived from Yantra JS or from any other project.
- `Broiler.ECMAScript.Conformance` (not packed) holds the conformance corpus's reader, validator and
  runners, written for the programme. The corpus it reads, `conformance/vectors/`, takes its expected
  values from ECMA-262 and from an exact reference computation in this repository, not from code.

Code that later moves into a shared library records its origin unit by unit, and code whose licence
is share-alike or unclear is rewritten from the specification rather than moved (the programme's
governance, section 9). Each such move adds its notice here. Data travels with its own notices
through the engine that embeds it, never in a shared package (decision SJL-D-6 (c)).

*Added 2026-10-12, at milestone SJL-2.13.*
