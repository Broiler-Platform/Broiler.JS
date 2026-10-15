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
contain two pieces of third-party code and one set of public-domain formulas, named below, and no data:

- `Broiler.ECMAScript.Algorithms` holds the contract types and the number-conversion, numerics, text
  and Date areas written for the shared libraries programme, Apache-2.0, and no code derived from
  Yantra JS. Its third-party components are **Ryu** and **fdlibm**, below, and it implements **Howard
  Hinnant's civil-day formulas**, also below.
- **Ryu** ([github.com/ulfjack/ryu](https://github.com/ulfjack/ryu)), Copyright 2018 Ulf Adams, used
  under the Apache License, Version 2.0 (the project is offered under that licence or the Boost
  Software License 1.0, at the user's choice). `Broiler.ECMAScript.Algorithms/Numbers/ShortestDecimal.cs`
  ports its double-to-shortest-decimal routine (`d2s.c`) to C#. Its two multiplier tables are not
  copied from it: they were computed for this library, and the library's tests
  (`ShortestDecimalTableTests`) recompute every entry exactly. The Apache License text is in
  [`LICENSE`](LICENSE).
- **fdlibm** 5.3, as FreeBSD's `msun` keeps it (`lib/msun/src/s_expm1.c` and `s_log1p.c`), Copyright
  (C) 1993 by Sun Microsystems, Inc., under the notice it carries (SPDX `SunPro`):
  `Broiler.ECMAScript.Algorithms/Numbers/Fdlibm.cs` ports `expm1` and `log1p` to C#, and keeps the
  notice in its source. The notice reads:

  > Copyright (C) 1993 by Sun Microsystems, Inc. All rights reserved.
  >
  > Developed at SunPro, a Sun Microsystems, Inc. business.
  > Permission to use, copy, modify, and distribute this
  > software is freely granted, provided that this notice
  > is preserved.
- **Howard Hinnant's civil-day formulas**, `days_from_civil` and `civil_from_days`
  ([howardhinnant.github.io/date_algorithms.html](https://howardhinnant.github.io/date_algorithms.html)),
  which their author "donated to the public domain": `Broiler.ECMAScript.Algorithms/Time/IsoCalendarMath.cs`
  implements them in `EpochDays` and `FromEpochDays` (milestone SJL-6.3), and says so in its source. No
  licence terms apply; the credit is given here.
- The number area's other routines that are not written from the specification are ported from
  Broiler.VM, the programme's other engine, under the same licence and by the same contributors
  (`FixedDigits.cs`, from `JsRealm.Number`; the BigInt literal ceiling, from `JsParser.TryBigIntValue`;
  and, at milestone SJL-4.3, the BigInt kernel `BigIntMath.cs` and `BigIntText.cs`, from `JsBigInt`);
  so is the Date area's reading of strings in Broiler.VM's dialect, at milestone SJL-6.3
  (`EcmaDateParser.cs`, from `DateParseText` and `DateParseTextual`). Each records its origin in its
  assurance annotation or its source.
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
  method forwards to the shared library. The project also holds, under `ReferenceArms/Numerics/`, the
  reference arms of bundle `docs/evidence/sjl-4-001`: test-only copies of both engines' numerics code
  at their SJL-3 merges, held and retired the same way. Arm J is this engine's code, as above; arm V is
  Broiler.VM's, under the same licence and by the same contributors. Arm J's `SumPrecise` says it
  ports the reference algorithm of the TC39 `Math.sumPrecise` proposal
  ([github.com/tc39/proposal-math-sum](https://github.com/tc39/proposal-math-sum)), Shewchuk's exact
  addition as CPython's `math.fsum` uses it; since milestone SJL-4.5 that copy is the only one in this
  repository (see the engine's section below), and the shared library's `PreciseSum` is written from
  the clause, not from that code. Neither arm copies fdlibm.

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
no other text changed. Revised again 2026-10-13, at milestone SJL-4.3, when the numerics area moved in:
the section's second sentence read "They contain one piece of third-party code, named below, and no
data:"; the first bullet read "holds the contract types and the number-conversion area written for the
shared libraries programme" and ended "Its one third-party component is **Ryu**, below."; the bullet on
the code ported from Broiler.VM ended at "from `JsParser.TryBigIntValue`)."; and the bullet on fdlibm
was added. Revised again the same day, later in milestone SJL-4.3: the bullet on
`Broiler.ECMAScript.Algorithms.Tests` ended at "the engine's own method forwards to the shared
library.", and its last three sentences, on bundle `sjl-4-001`'s arms, were added. Revised again
2026-10-13, at milestone SJL-4.5: the bullet's last sentence read "Neither copies fdlibm or any other
third-party code.", which overlooked arm J's `SumPrecise`. Revised again 2026-10-15, at milestone
SJL-6.3, when the Date area moved in: the section's second sentence read "They contain two pieces of
third-party code, named below, and no data:"; the first bullet read "holds the contract types, the
number-conversion area and the numerics area written for the shared libraries programme" and ended
"Its third-party components are **Ryu** and **fdlibm**, below.", which also overlooked the text area of
milestone SJL-5.3; the bullet on Hinnant's formulas was added; and the bullet on the code ported from
Broiler.VM ended at "from `JsBigInt`). Each records its origin in its assurance annotation."

## Sources the engine's own code cites

- `JSMath.Clz32` (`Broiler.JavaScript.BuiltIns`) links Jurassic's `MathObject.cs`
  ([github.com/paulbartrum/jurassic](https://github.com/paulbartrum/jurassic), MIT License, Copyright
  (c) 2015 Paul Bartrum) as a reference. Its body is `ToUint32` and a leading-zero count, and carries
  no Jurassic code. `UriHelper` (`Broiler.JavaScript.Runtime`) linked Jurassic's `GlobalObject.cs` the
  same way, and whether its code derived from it was never recorded. It was deleted at milestone
  SJL-5.5, not resolved: the URI functions call the shared library's `UriCodec`, written from the
  specification and from Broiler.VM's implementation. The test-only arm J of the text area's reference
  arms keeps a copy until SJL-16. *(Revised 2026-10-15, at milestone SJL-5.5: the sentence read
  "`UriHelper` (`Broiler.JavaScript.Runtime`) links Jurassic's `GlobalObject.cs` the same way; whether
  its code derives from it is not recorded here yet.")*
- `JSMath.SumPrecise` said, until milestone SJL-4.5, that it ported the reference algorithm of the
  TC39 `Math.sumPrecise` proposal named above. That code is deleted from the engine: the sum is the
  shared library's `PreciseSum`, written from the clause. The test-only arm J keeps a copy until
  SJL-16.

*Added 2026-10-13, at milestone SJL-4.5, as the shared libraries plan said it would be: the plan's
numerics record found that the notices named neither citation.*

## Data the engine's own code is generated from

- `JSIntlDefaultNumberingSystems` (`Broiler.JavaScript.BuiltIns`, `Intl/JSIntlDefaultNumberingSystems.g.cs`),
  each locale's default numbering system, is generated by `scripts/intl/generate_default_numbering_systems.py`
  from the **Unicode Common Locale Data Repository (CLDR)**, release 47.0.0, as the `cldr-json` project's
  `cldr-numbers-full` package publishes it (npm, integrity
  `sha512-5OXUfjiAdXYuVD4XlaQe++WKBVmI1bkKxO0uWJMpbokzgcJ30vhFzcX5WsnZMtoZuhEpOt5KubCJaxaHPj5XTQ==`, which
  the generator checks). It is CLDR data, under the Unicode License v3 below.

*Added 2026-10-15, at milestone SJL-5.6, when the table was added.*

```text
UNICODE LICENSE V3

COPYRIGHT AND PERMISSION NOTICE

Copyright © 2004-2025 Unicode, Inc.

NOTICE TO USER: Carefully read the following legal agreement. BY
DOWNLOADING, INSTALLING, COPYING OR OTHERWISE USING DATA FILES, AND/OR
SOFTWARE, YOU UNEQUIVOCALLY ACCEPT, AND AGREE TO BE BOUND BY, ALL OF THE
TERMS AND CONDITIONS OF THIS AGREEMENT. IF YOU DO NOT AGREE, DO NOT
DOWNLOAD, INSTALL, COPY, DISTRIBUTE OR USE THE DATA FILES OR SOFTWARE.

Permission is hereby granted, free of charge, to any person obtaining a
copy of data files and any associated documentation (the "Data Files") or
software and any associated documentation (the "Software") to deal in the
Data Files or Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, and/or sell
copies of the Data Files or Software, and to permit persons to whom the
Data Files or Software are furnished to do so, provided that either (a)
this copyright and permission notice appear with all copies of the Data
Files or Software, or (b) this copyright and permission notice appear in
associated Documentation.

THE DATA FILES AND SOFTWARE ARE PROVIDED "AS IS", WITHOUT WARRANTY OF ANY
KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF
THIRD PARTY RIGHTS.

IN NO EVENT SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN THIS NOTICE
BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL DAMAGES,
OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS,
WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION,
ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THE DATA
FILES OR SOFTWARE.

Except as contained in this notice, the name of a copyright holder shall
not be used in advertising or otherwise to promote the sale, use or other
dealings in these Data Files or Software without prior written
authorization of the copyright holder.

SPDX-License-Identifier: Unicode-3.0
```
