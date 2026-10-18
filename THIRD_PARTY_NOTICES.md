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
contain three pieces of third-party code and one set of public-domain formulas, named below, and no data:

- `Broiler.ECMAScript.Algorithms` holds the contract types, the number-conversion, numerics, text,
  Date and time-zone and calendar areas, and the Temporal core's first part written for the shared
  libraries programme, Apache-2.0, and no code derived from Yantra JS. Its third-party components are
  **Ryu**, **fdlibm** and **ICU4X's lunisolar approximation**, below, and it implements **Howard
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
- **ICU4X's lunisolar approximation**, from the `icu_calendar` crate 2.3.0
  (`src/cal/east_asian_traditional/simple.rs`, [github.com/unicode-org/icu4x](https://github.com/unicode-org/icu4x)),
  Copyright The ICU4X Project Developers, under the Unicode License v3 (SPDX `Unicode-3.0`), whose text is
  below: `Broiler.ECMAScript.Algorithms/Time/CalendarYears.cs` ports its mean-solar-term and mean-new-moon
  reckoning of a `chinese` or `dangi` year outside the published tables (milestone SJL-8.3), as Broiler.VM's
  `JsCalendars` ported it, and says so in its source. The years inside the tables are data the engine hands
  in, never the library's (below).
- The number area's other routines that are not written from the specification are ported from
  Broiler.VM, the programme's other engine, under the same licence and by the same contributors
  (`FixedDigits.cs`, from `JsRealm.Number`; the BigInt literal ceiling, from `JsParser.TryBigIntValue`;
  and, at milestone SJL-4.3, the BigInt kernel `BigIntMath.cs` and `BigIntText.cs`, from `JsBigInt`);
  so is the Date area's reading of strings in Broiler.VM's dialect, at milestone SJL-6.3
  (`EcmaDateParser.cs`, from `DateParseText` and `DateParseTextual`); and so is the time-zone and calendar
  area, at milestone SJL-8.3 (`TimeZoneDatabase.cs`, `OffsetTimeZones.cs`, `ZonedTime.cs` and
  `CalendarYears.cs`, from `JsTimeZones`, `JsTemporal.Zones`, `JsRealm.DateTimeFormat`'s zone resolution and
  `JsCalendars`), whose Persian and tabular Hijri years are Calendrical Calculations' arithmetic; and so is
  the Temporal core's first part, its records and limits, balancing, grammar and serialization, at milestone
  SJL-10.3 (`Temporal/TemporalRecords.cs`, `TemporalLimits.cs`, `IsoArithmetic.cs`, `DurationCore.cs`,
  `TemporalGrammar.cs`, `TemporalFormat.cs` and `TemporalOptionRules.cs`, from `JsTemporalCore`,
  `JsTemporalParser`, `JsTemporal.Conversions` and the engine-free arithmetic of `JsTemporal.Calendar`,
  `JsTemporal.Durations` and `JsTemporal.Options`). Each records its origin in its assurance annotation or
  its source.
- The Temporal calendars, at milestone SJL-10.3 (`Temporal/Calendars/`), are ported from Broiler.VM under the
  same licence and by the same contributors: the calendar systems from `JsCalendars`, and the calendar operations
  from the core of `JsTemporal.Calendar` and `JsTemporal.CalendarNonIso`. The Coptic, Ethiopian, Hebrew, Persian and
  tabular Hijri calendars are Calendrical Calculations' arithmetic, as Broiler.VM's are; the month-day reference
  years of `chinese` and `dangi` are the Intl era and month code proposal's Table 6. Each unit records its origin in
  its assurance annotation.
- `Broiler.ECMAScript.Generation` holds the time data home's generator (milestone SJL-8.2): the tz pin
  reader, the tz compiler and the time zone table generator, the calendar pin reader and the calendar
  table generator, ported from Broiler.VM's architecture tests under the same licence and by the same
  contributors, each recording its origin in its assurance annotation. The compiler reproduces what the
  IANA tz project's `zic` writes, whose code is in the public domain; it is written from Broiler.VM's
  compiler and the tz source format, not from `zic`'s code. **The package carries no data**: it reads the
  pinned archives its caller hands it, and the tables it writes are files of this repository, under the
  time data section below, never package content (decision SJL-D-6 (c)).
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
  the clause, not from that code. Neither arm copies fdlibm. The project also holds, under
  `ReferenceArms/Text/` and `ReferenceArms/Date/`, the reference arms of bundles `docs/evidence/sjl-5-001` and
  `docs/evidence/sjl-6-001`: test-only copies of both engines' text and Date code before their cut-overs, held
  and retired the same way; arm J is this engine's code and arm V Broiler.VM's, as above, and the Date arms'
  civil copies implement Hinnant's formulas, above. Its time-zone and calendar tests (milestone SJL-8.3)
  read the data home's generated tables from disk, under the time data section below, and hold no copy of
  them. Its Temporal calendar tests (milestone SJL-10.3) hold, under `Temporal/Correspondence/`, byte-for-byte
  copies of ten of Broiler.VM's answer files of correspondence `sjl-10-001`, under the same licence and by the same
  contributors, and read the retained dataset `conformance/datasets/time/temporal-calendars.js` and its answers in
  place.

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
milestone SJL-5.3; the bullet on Hinnant's formulas was added; the bullet on the code ported from
Broiler.VM ended at "from `JsBigInt`). Each records its origin in its assurance annotation."; and the bullet
on `Broiler.ECMAScript.Algorithms.Tests` ended at "Neither arm copies fdlibm.", which overlooked bundle
`sjl-5-001`'s text arms, and the sentence on them and on bundle `sjl-6-001`'s Date arms was added. Revised
again 2026-10-17, at milestone SJL-8.2: the bullet on `Broiler.ECMAScript.Generation` was added, and no
other text changed. Revised again 2026-10-17, at milestone SJL-8.3, when the time-zone and
calendar area moved in: the section's second sentence read "They contain two pieces of third-party code and
one set of public-domain formulas, named below, and no data:"; the first bullet read "holds the contract
types and the number-conversion, numerics, text and Date areas written for the shared libraries programme"
and "Its third-party components are **Ryu** and **fdlibm**, below, and it implements **Howard Hinnant's
civil-day formulas**, also below."; the bullet on ICU4X's approximation was added; the bullet on the code
ported from Broiler.VM read "so is the Date area's reading of strings in Broiler.VM's dialect, at milestone
SJL-6.3 (`EcmaDateParser.cs`, from `DateParseText` and `DateParseTextual`). Each records its origin in its
assurance annotation or its source."; and the bullet on `Broiler.ECMAScript.Algorithms.Tests` ended at "the
Date arms' civil copies implement Hinnant's formulas, above.". Revised again 2026-10-18, at milestone SJL-10.3,
when the Temporal core moved in, in two parts. At part A, its records and limits, balancing, grammar and
serialization: the first bullet read "holds the contract types and the number-conversion, numerics, text, Date
and time-zone and calendar areas written for the shared libraries programme"; and the bullet on the code ported
from Broiler.VM read "..., whose Persian and tabular Hijri years are Calendrical Calculations' arithmetic. Each
records its origin in its assurance annotation or its source.". At part B, its calendars: the bullet on them was
added after the bullet on the code ported from Broiler.VM, and the bullet on
`Broiler.ECMAScript.Algorithms.Tests` ended at "and hold no copy of them.".

## The time data home (`Broiler.ECMAScript/data/time/`)

This repository is the home of both engines' time zone and calendar data (decision SJL-D-1 (b-iii);
Broiler.VM's JSD-0076). [`Broiler.ECMAScript/data/time/`](Broiler.ECMAScript/data/time/README.md) holds
three pinned inputs, each a byte-for-byte copy of Broiler.VM's at its commit `9508bd5`, and the tables
`Broiler.ECMAScript.Generation` writes from them into `data/time/generated/`. **No package carries the
inputs or the tables** (decision SJL-D-6 (c)): each engine embeds the tables itself, and Broiler.JS does so
from milestone SJL-8.6, when this section's tables also join the engine's data above.

- **The IANA Time Zone Database**, release 2026e, its data tarball `tzdata2026e.tar.gz` archived unmodified
  in [`data/time/tzdb/`](Broiler.ECMAScript/data/time/tzdb/README.md), where `tzdb.pin` records its length,
  SHA-256 and SHA-512 and the length and SHA-256 of the twelve members the generator reads. The release's
  `LICENSE` states: "Unless specified below, all files in the tz code and data (including this LICENSE
  file) are in the public domain." The files it then names are code, are not in the data tarball, and are
  not here. No licence text therefore has to travel with the tables derived from it, `time-zones.bin`,
  `time-zone-ids.txt` and `time-zone-regions.txt`; this entry records their source all the same.
- **The Unicode Common Locale Data Repository (CLDR)**, release 48.2.0: one file, `bcp47/timezone.json`
  from the npm package `cldr-bcp47`, archived unmodified in
  [`data/time/cldr/`](Broiler.ECMAScript/data/time/cldr/README.md) with the CLDR licence text,
  `cldr-LICENSE.txt`, and held by its length and SHA-256 in `cldr.pin`. It is © Unicode, Inc., under the
  Unicode License v3 below. The primary identifiers in `time-zone-ids.txt` are derived from it and remain
  subject to that licence.
- **ICU4X's `icu_calendar` 2.3.0**, as crates.io serves it, archived unmodified in
  [`data/time/calendars/`](Broiler.ECMAScript/data/time/calendars/README.md) and held by `calendars.pin`. It
  is under the Unicode License v3 below; the crate's own copy of the licence is its `LICENSE`, "Copyright ©
  2020-2024 Unicode, Inc.". The Chinese, Korean, Qing-era and Umm al-Qura years in `calendars.bin` are read
  from its year tables and **remain subject to the Unicode License v3**.
- **`calendrical_calculations` 0.2.4**, its dependency, archived beside it, under the Apache License 2.0,
  the licence of this repository's own code, whose text is in [`LICENSE`](LICENSE). Its files state that
  they implement algorithms from *Calendrical Calculations* by Reingold and Dershowitz (4th edition, 2018),
  which their authors released as Lisp code under the Apache License 2.0; the crate carries no `NOTICE`
  file. The Persian calendar's corrections in `calendars.bin` are read from its `persian.rs`.

*Added 2026-10-17, at milestone SJL-8.2, when the data home's inputs and generator were added. Broiler.VM's
`THIRD_PARTY_NOTICES.md` carries the same three inputs' entries for its own copies.*

## The reference datasets (`conformance/datasets/time/`)

[`conformance/datasets/time/`](conformance/datasets/time/README.md) holds byte-for-byte copies of three programs
of Broiler.VM, each with the answer a reference implementation gave for it and Broiler.VM's divergences from that
answer: the first two as Broiler.VM's commit `9508bd5` holds them, the third as its `14072b0` does, all held by
their SHA-256 in `datasets.pin`. Bundle `sjl-8-001`'s rule, part C, runs the first two in this engine's shell, and
bundle `sjl-10-001`'s rule, part C, `temporal.js` and the third. **They are test material only: no project
builds, packs or embeds them.**

- **The programs**, `timezones.js`, `temporal.js` and `temporal-calendars.js`, and **the divergences files** are
  Broiler.VM's, by the same contributors as this repository, under the Apache License 2.0, whose text is in
  [`LICENSE`](LICENSE).
- **`timezones.icu-77.1.txt`** is the output of `timezones.js` run on Node 22.22.0, whose ICU is 77.1, with
  CLDR 47.0 and tzdb 2025b. ICU and CLDR are © Unicode, Inc., under the Unicode License v3 below. The file holds
  what ICU printed for the program, one line per name: time zone identifiers and UTC offsets in the GMT format,
  and the identifiers it lists. It reproduces none of ICU's code and no CLDR file. Broiler.VM's notices record
  the polyfill's answers, the next item, and not this file; this entry records both, so that a reader finds
  every reference the copies were produced by in one file.
- **`temporal.polyfill-e8cc03fc.txt`** is the output of `temporal.js` run under the Temporal proposal's
  reference polyfill, `polyfill/lib` of [tc39/proposal-temporal](https://github.com/tc39/proposal-temporal) at
  `e8cc03fc970a65a3359e8870e3b35e687ac94e55`, which is licensed under the ISC licence, on Node 22.22.0. The
  polyfill was installed outside Broiler.VM's repository for the run and is not in this one. The file holds the
  answers the polyfill gave, one per line; it reproduces none of the polyfill's code or text, so no licence
  text travels with it. Broiler.VM's notices say the same of its copy.
- **`temporal-calendars.polyfill-e8cc03fc.txt`** is the output of `temporal-calendars.js` run under the same
  polyfill at the same revision on Node 22.22.0. The polyfill computes every calendar but ISO 8601 by formatting
  dates with Node's `Intl.DateTimeFormat`, whose ICU4C is 77.1, so the calendar fields it holds are what ICU
  computed for those dates. It holds the answers, one per line, and reproduces none of the polyfill's or ICU's
  code or text. Broiler.VM's notices say the same of its copy.

*Added 2026-10-17, at milestone SJL-8.4, when the copies were added; the third program and its answer were added
on 2026-10-18, at milestone SJL-10.2.*

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
- `JSDate.Parse` (`Broiler.JavaScript.BuiltIns`) said, until milestone SJL-6.5, that it was Jint's
  `Parse` ([github.com/sebastienros/jint](https://github.com/sebastienros/jint), BSD 2-Clause License,
  Copyright (c) Sebastien Ros) with its `DateTimeStyles` changed, and the dead `JSDate.ToDateTime` named
  a step "missing from Jurassic" (above). Both are deleted: `Date.parse` calls the shared library's
  `EcmaDateParser`, written from the specification and from Broiler.VM's implementation, and this
  engine's own fallback between its two dialects. That fallback, `DateParser` (`Broiler.JavaScript.Runtime`),
  keeps the format lists the deleted method read, other than its lists of the Date Time String Format;
  whether they derive from Jint was never recorded. The test-only arm J of the Date area's reference arms
  keeps a copy of the deleted code until SJL-16.

- `CharExtensions` (`Broiler.JavaScript.Parser`) held, until milestone SJL-7.5, two tables of the code points
  Unicode 17.0.0 adds to ID_Start and ID_Continue, which its comment said were taken from test262's
  generated identifier tests, `test/language/identifiers/start-unicode-17.0.0.js` and
  `part-unicode-17.0.0.js` ([github.com/tc39/test262](https://github.com/tc39/test262), under test262's BSD
  license; those files are Copyright 2025 Mathias Bynens, generated by caniunicode). Both tables are deleted
  with the classification they patched: identifiers are Broiler.Unicode's `EcmaScriptLexical` over the
  generated tables below.

*Added 2026-10-13, at milestone SJL-4.5, as the shared libraries plan said it would be: the plan's
numerics record found that the notices named neither citation. Revised 2026-10-16, at milestone SJL-6.5:
the bullet on `JSDate.Parse` was added; the notices had named neither of the Date code's citations. Revised
again 2026-10-16, at milestone SJL-7.5: the bullet on `CharExtensions` was added; the notices had not named
its tables' source.*

## Data the engine's own code is generated from

- `JSIntlDefaultNumberingSystems` (`Broiler.JavaScript.BuiltIns`, `Intl/JSIntlDefaultNumberingSystems.g.cs`),
  each locale's default numbering system, is generated by `scripts/intl/generate_default_numbering_systems.py`
  from the **Unicode Common Locale Data Repository (CLDR)**, release 47.0.0, as the `cldr-json` project's
  `cldr-numbers-full` package publishes it (npm, integrity
  `sha512-5OXUfjiAdXYuVD4XlaQe++WKBVmI1bkKxO0uWJMpbokzgcJ30vhFzcX5WsnZMtoZuhEpOt5KubCJaxaHPj5XTQ==`, which
  the generator checks). It is CLDR data, under the Unicode License v3 below.

*Added 2026-10-15, at milestone SJL-5.6, when the table was added.*

- `JSUnicodeTableData` (`Broiler.JavaScript.Runtime`, `Unicode/JSUnicodeTableData.g.cs`), the Unicode
  tables the engine's identifiers, white space, `\p{…}` names, case mapping, normalization, properties of
  strings and `Intl.Segmenter` read, is generated by Broiler.Unicode's generator, the test-only package
  `Broiler.Unicode.Generation`, from the archive in [`data/unicode/pins/`](data/unicode/pins/README.md):
  the **Unicode Character Database** 17.0.0 files, its auxiliary break data and the **emoji** 17.0 sequence
  files, unmodified, each held by its length and SHA-256 in `unicode.pin`. They are © Unicode, Inc., under
  the Unicode License v3 below; the archive keeps the licence text it was published with as
  `data/unicode/pins/unicode-LICENSE.txt`, the same licence, whose copyright line reads "Copyright ©
  1991-2026 Unicode, Inc.". The tables are Unicode data under that licence; the code that reads them,
  `Broiler.Unicode.Algorithms` and `Broiler.Unicode.Segmentation`, is Broiler's under the Apache License 2.0,
  and carries no table (decision SJL-D-6 (c)).
- The same tables encode the property names, values and aliases ECMAScript admits, which the generator reads
  from three tables of **ECMA-262**, archived unmodified in
  [`data/unicode/pins/ecma262/`](data/unicode/pins/ecma262), below.
- Until milestone SJL-7.5 the engine's case mapping, its `\p{…}` sets and its properties of strings were read
  from the Unicode data the `Broiler.Unicode.Properties` and `Broiler.Unicode.Emoji.StringProperties`
  packages carry, under the same licence, with the runtime's own Unicode data and the host's ICU. The engine
  no longer calls either package; both still arrive with `Broiler.Regex`, which depends on them, until
  milestone SJL-11, and carry their own notices.

*Added 2026-10-16, at milestone SJL-7.5, when the Unicode area moved to Broiler.Unicode's packages.*

- The time zone and calendar tables `Broiler.JavaScript.BuiltIns` embeds (its resources
  `Broiler.JavaScript.BuiltIns.TimeData.*`) are the time data home's `data/time/generated/` files, linked into
  the assembly unmodified. Their sources and licences are the home's, above: the IANA Time Zone Database's
  zones and links, in the public domain; CLDR's primary identifiers, under the Unicode License v3; ICU4X's
  Chinese, Korean, Qing-era and Umm al-Qura years, under the Unicode License v3; and
  `calendrical_calculations`' Persian corrections, under the Apache License 2.0.
- Until milestone SJL-8.6 the engine embedded instead `tzdata.bin`, a DEFLATE pack of a build host's compiled
  zoneinfo (tzdb 2025b, in the public domain), and read its Chinese, Korean and Persian years from the .NET
  runtime's calendars and the CLDR package's `CldrChineseCalendar` (an implementation of *Calendrical
  Calculations*' astronomy) and its Umm al-Qura years from that package's `CldrUmmAlQura` (ICU4C's table). It
  no longer embeds `tzdata.bin` or calls either type. `Broiler.Unicode.Cldr.LocaleData` stays a dependency
  for its locale data, with its own notices.

*Added 2026-10-17, at milestone SJL-8.6, when the time-zone and calendar area moved to the data home's
tables and `Broiler.ECMAScript.Algorithms`.*

### ECMA-262's property tables

[`data/unicode/pins/ecma262/`](data/unicode/pins/ecma262) holds three tables of the ECMAScript Language
Specification, ECMA-262, 17th edition (ES2026), unmodified, from the `tc39/ecma262` repository at
`0248456c`: `table-binary-unicode-properties.html`, `table-nonbinary-unicode-properties.html` and
`table-binary-unicode-properties-of-strings.html`. They are © Ecma International. That repository licenses
its natural-language text under the **Alternative copyright notice of the Ecma text copyright policy**,
retained in full beside them at
[`ECMA-alternative-copyright-notice.txt`](data/unicode/pins/ecma262/ECMA-alternative-copyright-notice.txt).
The generator reads them for the names alone, which `JSUnicodeTableData` encodes; no prose of the
specification is copied. The notice of changes the Ecma notice's condition (iii) asks for is:

Broiler.JS includes material derived from the ECMAScript Language Specification, ECMA-262, 17th edition
(ES2026), https://ecma-international.org/publications-and-standards/standards/ecma-262/. Copyright (c) Ecma
International.

Condition (i) asks for the notice's full text where users of the derivative work can see it. This file
travels in every package, so the notice is reproduced here:

```text
ALTERNATIVE COPYRIGHT NOTICE AND COPYRIGHT LICENSE

(c) Ecma International

By obtaining and/or copying this work, you (the licensee) agree that you have read, understood,
and will comply with the following terms and conditions.

Permission under Ecma's copyright to copy, modify, prepare derivative works of, and distribute
this work, with or without modification, for any purpose and without fee or royalty is hereby
granted, provided that you include the following on ALL copies of the work or portions thereof,
including modifications:

(i)   The full text of this COPYRIGHT NOTICE AND COPYRIGHT LICENSE in a location viewable to
      users of the redistributed or derivative work.

(ii)  Any pre-existing intellectual property disclaimers, notices, or terms and conditions. If
      none exist, the Ecma alternative copyright notice should be included.

(iii) Notice of any changes or modifications, through a copyright statement on the document such
      as "This document includes material copied from or derived from [title and URL of the Ecma
      document]. Copyright (c) Ecma International."

DISCLAIMERS

THIS WORK IS PROVIDED "AS IS," AND COPYRIGHT HOLDERS MAKE NO REPRESENTATIONS OR WARRANTIES,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO, WARRANTIES OF MERCHANTABILITY OR FITNESS FOR
ANY PARTICULAR PURPOSE OR THAT THE USE OF THE DOCUMENT WILL NOT INFRINGE ANY THIRD PARTY PATENTS,
COPYRIGHTS, TRADEMARKS OR OTHER RIGHTS.

COPYRIGHT HOLDERS WILL NOT BE LIABLE FOR ANY DIRECT, INDIRECT, SPECIAL OR CONSEQUENTIAL DAMAGES
ARISING OUT OF ANY USE OF THE DOCUMENT.

The name and trademarks of copyright holders may NOT be used in advertising or publicity
pertaining to the work without specific, written prior permission. Title to copyright in this work
will at all times remain with copyright holders.
```

### The Unicode License v3

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
