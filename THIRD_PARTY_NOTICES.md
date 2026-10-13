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

The packages built from [`Broiler.ECMAScript/`](Broiler.ECMAScript/README.md) carry this file. They
contain one piece of third-party code, named below, and no data:

- `Broiler.ECMAScript.Algorithms` holds the contract types and the number-conversion area written
  for the shared libraries programme, Apache-2.0, and no code derived from Yantra JS. Its one
  third-party component is **Ryu**, below.
- **Ryu** ([github.com/ulfjack/ryu](https://github.com/ulfjack/ryu)), Copyright 2018 Ulf Adams, used
  under the Apache License, Version 2.0 (the project is offered under that licence or the Boost
  Software License 1.0, at the user's choice). `Broiler.ECMAScript.Algorithms/Numbers/ShortestDecimal.cs`
  ports its double-to-shortest-decimal routine (`d2s.c`) to C#. Its two multiplier tables are not
  copied from it: they were computed for this library, and the library's tests
  (`ShortestDecimalTableTests`) recompute every entry exactly. The Apache License text is in
  [`LICENSE`](LICENSE).
- The number area's other routines that are not written from the specification are ported from
  Broiler.VM, the programme's other engine, under the same licence and by the same contributors
  (`FixedDigits.cs`, from `JsRealm.Number`; the BigInt literal ceiling, from `JsParser.TryBigIntValue`).
  Each records its origin in its assurance annotation.
- `Broiler.ECMAScript.Conformance` (not packed) holds the conformance corpus's reader, validator and
  runners, written for the programme. The corpus it reads, `conformance/vectors/`, takes its expected
  values from ECMA-262 and from an exact reference computation in this repository, not from code.
- `Broiler.ECMAScript.Algorithms.Tests` (not packed) holds the reference arms of bundle
  `docs/evidence/sjl-3-001`: test-only copies of both engines' number code as it was before their
  cut-overs, under `ReferenceArms/`, never referenced outside that project and retired at milestone
  SJL-16. Arm J is this engine's code, derived in part from Yantra JS as above. Its
  `JSNumberPrototype.DecimalToArbitrarySystem` is a **Stack Overflow answer** its comment links
  (question 923771), Copyright 2012 Pavel Vladov, under the Creative Commons Attribution-ShareAlike 3.0
  licence. Since milestone SJL-3.5 that copy is the only one in this repository: the engine's own
  method forwards to the shared library.

Code that later moves into a shared library records its origin unit by unit, and code whose licence
is share-alike or unclear is rewritten from the specification rather than moved (the programme's
governance, section 9). Each such move adds its notice here. Data travels with its own notices
through the engine that embeds it, never in a shared package (decision SJL-D-6 (c)).

*Added 2026-10-12, at milestone SJL-2.13. Revised 2026-10-12, at milestone SJL-3.3, when the number
area moved in: the section's first sentences read "The packages built from `Broiler.ECMAScript/`
carry this file. At this version they contain no third-party code and no data:", and its first bullet
read "`Broiler.ECMAScript.Algorithms` holds the contract types written for the shared libraries
programme, Apache-2.0, and no code derived from Yantra JS or from any other project."* Revised again
2026-10-12, at milestone SJL-3.5: the bullet on `Broiler.ECMAScript.Algorithms.Tests` was added, and
no other text changed.
