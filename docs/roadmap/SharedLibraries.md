# Shared JavaScript libraries — Broiler.JS's half of the programme

Give each JavaScript language algorithm, data set and source front end that Broiler.JS and
Broiler.VM's JavaScript profile both implement **one maintained implementation**, in libraries that
name neither engine, hosted in this repository where the programme places them, and consumed by both
engines. This engine stays an independently usable, public reference engine built on the same
libraries, and keeps its own value model, object model, execution, IL back end, hosting and resource
policy.

> The evidence half is [`SharedLibraries.status.md`](SharedLibraries.status.md). **The programme
> itself is owned in the Broiler.VM repository**: its roadmap, ledger, decision register, area
> designs and milestone files are under `Broiler.VM/docs/javascript-shared-libraries/`, and its
> milestone files hold every exit gate in full. This plan carries Broiler.JS's part of that
> programme, in this repository's plan/status convention, and closes nothing on its own: each item
> below closes against the Broiler.JS clauses of its milestone, met by evidence collected here.
>
> Added 2026-10-12, at the programme's milestone SJL-0.

## Current state

The decisions are taken and their records filed. The shared conformance corpus exists, with this
engine's runners over it (SJL-1), and so does the shared folder, `Broiler.ECMAScript/`, with its
conventions, its rules, its version stream and its Native AOT probe (SJL-2). Two areas have moved into
it, and both engines consume them: number conversion (SJL-3) and the numerics (SJL-4, whose cut-overs
are done and whose evidence remains). See [`SharedLibraries.status.md`](SharedLibraries.status.md).

*(Corrected 2026-10-12, at milestone SJL-2. It read: "The decisions are taken, the records are being
filed, and no shared code exists. Milestone SJL-0 is in progress in both repositories; every other
milestone is not started.")* *(Corrected again 2026-10-14, at milestone SJL-4.5; the correction was
due from SJL-3's cut-overs. The second sentence ended: "(SJL-2); no algorithm has moved into it, and
no engine consumes a shared package.")*

## What this repository hosts

Decision **SJL-D-1 (b)**, taken on 2026-10-11, places the libraries by component:

| Library | Package | Home |
|---|---|---|
| Language algorithms: number conversion, numerics, text, Date, the Temporal core, time-zone algorithms | `Broiler.ECMAScript.Algorithms` | **This repository** |
| Intl algorithms over a data provider, collation included (milestone SJL-17) | `Broiler.ECMAScript.Intl` | **This repository** |
| The frontend: tokenizer, parser, canonical syntax tree, positions, diagnostics, static semantics | `Broiler.ECMAScript.Frontend` | **This repository** |
| The shared conformance vectors | `Broiler.ECMAScript.Conformance` (test-only) | **This repository** |
| Locale data: Broiler.VM's CLDR generator and its pins (SJL-D-1 (b-ii)) | no package (SJL-D-6 (c)); `Broiler.ECMAScript.IntlData` reserved | **This repository** |
| Time data: the time-zone and calendar generators and their pins (SJL-D-1 (b-iii)) | no package (SJL-D-6 (c)); `Broiler.ECMAScript.TimeData` reserved | **This repository** |
| Unicode character data and algorithms | the Broiler.Unicode packages | Broiler.Unicode, under SJL-7.1's owner conditions; otherwise this repository |
| Regular expressions | `Broiler.Regex` | Broiler.RegEx, fixed to Broiler.VM's requirements (SJL-D-14 (a)) |

The shared projects live in their own folder, solution filter and version stream, apart from the
`Broiler.JavaScript.*` engine packages (milestone SJL-2).

## The decisions this plan carries

Every decision was taken on 2026-10-11 by the owner, who holds every role the programme names. This
plan is the Broiler.JS record of the part each one asks of this repository:

| Decision | What it asks of Broiler.JS |
|---|---|
| **SJL-D-1 (b)** homes | Host the libraries and generators in the table above. A sub-area of Unicode that Broiler.Unicode does not meet SJL-7.1's owner conditions for comes here, with the reason recorded |
| **SJL-D-2 (a)** package identity | The neutral prefix `Broiler.ECMAScript`, one package per boundary: `.Algorithms`, `.Intl`, `.Frontend`, `.Conformance`; `.TimeData` and `.IntlData` reserved. No reservation of the prefix on nuget.org is required *(revised 2026-10-12, when the owner withdrew it; read "The prefix is reserved on nuget.org before anything under it is published (SJL-2.13).")*. Broiler.JSeal's package gate admits it unchanged |
| **SJL-D-3** versioning | A separate version stream for the shared packages, with its own solution filter, pack invocation and tag prefix; semantic versioning, an API baseline, package validation against the last published version, and a changelog with "Observable changes" and "Data" headings. A specification correction and a data-version change are each a minor version. The engines declare minimum versions in their nuspecs and pin exact versions in their lock files. `docs/publishing.md` records it at SJL-2.13 |
| **SJL-D-4 (c)** how Broiler.VM obtains the packages | While an area is in development, its preview packages reach Broiler.VM through Broiler.VM's committed, hash-pinned feed, not through nuget.org (SJL-3's home clause 3); anything a Broiler.VM package release depends on is published to nuget.org first, so the home publishes before such a release |
| **SJL-D-5 (a)** assurance | Every relevant unit of a shared project carries a Broiler Code Assurance annotation, the artefacts are generated, and CI gates on them from the project's first commit; an area moves into a shared library only once that gate is active. Broiler.VM's release reads the review state of the revision it ships |
| **SJL-D-6 (c)** data | This repository holds the locale- and time-data generators and their pins; each engine embeds the tables they generate, in an assembly of its own; shared code reads tables through a provider. Every Broiler.JS behaviour that changes because pinned data replaces host data is a correction under SJL-D-16 |
| **SJL-D-9** fork policy | Fixes to a shared area flow through the shared library, in both directions. Engine code does not flow between the engines, and neither engine is the other's upstream. Evidence does not transfer. A shared library is never patched inside an engine: an engine pins back, or carries a dated adapter workaround naming the upstream defect and the release that removes it |
| **SJL-D-14 (a), (i)** regular expressions | Broiler.JS moves onto the fixed `Broiler.Regex` at SJL-11, and its .NET translator retires only after Broiler.Regex implements duplicate named groups |
| **SJL-D-16 (a)** observable corrections | A difference where a shared library follows the specification and this engine did not is a correction, listed by name in the changelog and admitted by the area's rule; a per-difference decision only for a difference with a known dependent consumer |
| **SJL-D-17** non-standard extensions | `Decimal` and `m` literals, `Number.prototype.clz`, `String.prototype.contains`, the .NET format-string extension to `toLocaleString`, and the SpiderMonkey-style date heuristics with the culture-dependent legacy `Date.parse` **stay in Broiler.JS**. Where one needs a shared primitive, a parameter of a shared operation provides it, such as a lexer flag that admits the `m` suffix |
| **SJL-D-18** no unification | Whole-runtime unification is **not planned**: `JSValue` and Broiler.VM's `JsValue` stay as they are, and so do objects, frames, dispatch, back ends, budgets and hosting. SJL-16 confirms it, or reopens it only on new evidence |
| **SJL-D-23 (a)** the pilot | The programme continues past the SJL-3 pilot, a choice taken before the pilot ran. SJL-3's criteria are its exit clauses and stop nothing else |

Decisions applied later are recorded by the milestone that applies them: SJL-D-10's metering
contract in the shared conventions (SJL-2.10) and the pilot record (SJL-3.1); SJL-D-25's public-type
dispositions in [`docs/public-api.md`](../public-api.md) at SJL-3.5 and each area; SJL-D-15's BigInt
width at SJL-4; SJL-D-19's providers at SJL-6; SJL-D-20 and SJL-D-21 at SJL-8; SJL-D-22 and SJL-D-24
at SJL-9; SJL-D-11 and SJL-D-12 at SJL-12, with SJL-D-12's `Ast` and `Parser` disposition at SJL-14;
SJL-D-7, SJL-D-8 and SJL-D-13 are Broiler.VM's alone, and ask nothing of this repository.

## Area verdicts

Each area milestone's first work package applies the programme's extraction gate (conditions X1 to X7
of `Broiler.VM/docs/javascript-shared-libraries/roadmap.md` section 5) and files the verdict twice: as a
JSD in Broiler.VM's JavaScript profile series, and here.

**Number conversion (SJL-3.1), filed 2026-10-12: accepted.** Broiler.VM's record is
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0065-number-conversion-moves-to-the-shared-algorithms-library.md`
(JSD-0065), which states each condition. This repository's part of it:

- **The correspondence was re-derived** at this repository's `b7a2a3f`. Every file the area design
  (`Broiler.VM/docs/javascript-shared-libraries/areas/numbers.md`) names is byte-identical there to
  `44a214dc`, the revision the design read. Every difference SJL-1's run found is classified in
  `scripts/conformance/number-divergences.txt`; the ones the shared operations correct are listed in
  `CHANGELOG.md` at SJL-3.5, under decision SJL-D-16 (a), and the literal-scanning ones stay with
  SJL-14.
- **The deletion list gains two members** the design did not name: `Storage/KeyStrings`'
  `TryGetArrayIndex` and `TryGetCanonicalNumericIndex`, which classify every interned key with a second
  copy of the array-index test and a canonical-numeric test that reads .NET's `"R"` format. They become
  calls to the shared key operations, so `Broiler.JavaScript.Storage` references the library beside
  `Parser`, `Runtime` and `BuiltIns`. Without that, the engine would keep an intra-engine duplicate of
  the area, which SJL-3's exit clause 14 refuses.
- **Decision SJL-D-10 (a)** is applied in the library: each parsing operation takes a nullable
  `WorkCharge` and charges the input's length in UTF-16 code units, in blocks, before reading it.
  This engine has no budget, and its adapters pass none.
- **The non-standard extensions stay here** (SJL-D-17): the `m` literal suffix, `Number.prototype.clz`
  and `toLocaleString`'s .NET format strings. None is a shared operation.

**Numerics (SJL-4.1), filed 2026-10-13: all four sub-areas accepted.** Broiler.VM's record is
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0067-numerics-move-to-the-shared-algorithms-library.md`
(JSD-0067), which states each condition for integer conversions and Float16, the Math kernels, exact
rounding and the BigInt kernel, each on its own; its input, the correspondence from code with every
member by file and line, is `Broiler.VM/docs/evidence/sjl-4-001/correspondence.md`. This repository's
part of it:

- **The correspondence was read from code**, at this repository's `4899f9c`, because the area has no
  vectors yet; SJL-4.2's vectors confirm it. It found far more copies of the area here than the design
  named: about thirteen more `ToIndex`, `ToLength` and `ToIntegerOrInfinity` helpers, Atomics' own
  narrowing and BigInt arithmetic, `JSDate.CoercedIntValue`, a second `Math.Pow` emitter, about eleven
  copies of the rounding modes across Temporal and Intl, and BigInt text, comparison and key paths
  outside `JSBigInt`'s named members. Each joins the deletion list, and SJL-4.5 removes or forwards it.
- **The corrections**, each listed in `CHANGELOG.md` at SJL-4.5 under decision SJL-D-16 (a): `1 ** NaN`
  on every path and the ±1-to-an-infinity cases on the compiled fast paths; Atomics' operands beyond
  2^53; `%TypedArray%.prototype.fill`'s range; the typed-array constructor's conversion order;
  `%TypedArray%.from`'s length; a Date setter's argument; `String.prototype.endsWith`'s undefined
  position; the scientific and compact notations' rounding mode; `NudgeToCalendarUnit`'s window;
  `Duration.prototype.total`'s double rounding; the culture-dependent BigInt text; `Power`'s 32-bit
  exponent and a shift's .NET exceptions; `asUintN` at very large widths; the compiled `==` between a
  BigInt and a Number literal; BigInt property keys beyond 2^32 - 1 and below 0; and `ToBigInt`'s hint
  and Number refusal in the typed arrays. The Intl mathematical value and the Temporal consumers'
  quantities are wrong here too, and stay with SJL-9 and SJL-10, which the milestone leaves them to.
- **Decision SJL-D-15 (a) is recorded here**, as the decision names this record: this engine passes
  the BigInt kernel the unbounded width policy and a no-op meter, and maps the kernel's over-width
  status to `RangeError` at the representation's limit, never to a .NET exception. The kernel keeps
  Broiler.VM's charging units (JSD-0067, decision 3), which costs nothing here, where no meter is
  passed.
- **Decision SJL-D-25 applies to the public members the area empties**: `JSNumericOperators.ToInt32`
  and `ToUint32` stay as forwarders, and the public, saturating `IntegerValue` stays as a member that
  forwards to the library's `ToIntegerOrInfinity` and saturates as it does today; every specification
  path that used it calls the library instead. `docs/public-api.md` records each at SJL-4.5.
- **Not shared, and staying here:** `Math.fround`, `clz32`, `imul`, `max`, `min` and `sign`, whose
  result the platform already gives; `Math.random`'s single generator; `Math.sumPrecise`'s feature
  flag; typed arrays' host byte order. `JSMath.Clz32`'s comment cites Jurassic's `MathObject.cs`, and
  `SumPrecise` ports the `Math.sumPrecise` proposal's reference algorithm; the third-party notices name
  neither, and SJL-4.5 corrects that when the kernel moves.

**Numerics vectors (SJL-4.2), 2026-10-13.** Corpus revision 6 adds the area's vectors
(`conformance/vectors/numerics/`). Where the specification leaves a result to the implementation, a
vector's `per-engine.broiler-js` expectation is this plan's, and this is what the plan commits this
engine to:

- **A Float16 NaN keeps its payload.** This engine does not normalise: a NaN is written quiet, with
  its sign and the ten most significant bits of the double's payload, which is what its `(Half)`
  conversion does today and what it passes the shared `Float16.Encode` as its NaN policy at SJL-4.5.
- **The approximated Math results are the library's**, as JSD-0067 decision 5 states them: `cbrt`
  correctly rounded; `expm1` and `log1p` within one unit in the last place; `hypot` within one unit in
  the last place until the library states its bound; and `**` and `Math.pow`'s general case,
  `System.Math.Pow`'s, pinned only where every approximation gives one result.
- **A BigInt past any representation is a `RangeError`**, as decision SJL-D-15 (a) has it, never a .NET
  exception or a wrong value.

**The first runs (SJL-4.2), 2026-10-13.** Each engine's first engine-level run on revision 6 is
classified at revision 7, and both engines then classify every vector. SJL-4.5 answers every one of
this engine's differences: as corrections listed in `CHANGELOG.md`, BigInt powers past a 32-bit
exponent, `asUintN` at very large widths, BigInt property keys beyond 2^32 - 1, `1 ** NaN` and the
cases of 1 to an infinity, `expm1` and `log1p` beyond any approximation, and a hexadecimal
`StringToBigInt` whose time grows with the square of its digits; and, by the library's bounds, `cbrt`,
`hypot` and the rest of `expm1` and `log1p`. Four differences between
the engines were not in the area verdict's classification, which fires the verdict's first falsifier;
Broiler.VM's `JSD-0068` (`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0068-the-numerics-vectors-find-what-x2-did-not-classify.md`)
classifies them and keeps the verdict. For this engine it corrects one reading above, that the cases of
1 to an infinity are wrong "on the compiled fast paths": `1 ** Infinity` written as literals is 1 here
too, and SJL-4.5 corrects every path. It also refutes the verdict's reading that the library adopts this
engine's `expm1`, which is not accurate across the range: the library implements its own.

**The implementation (SJL-4.3), 2026-10-13.** The numerics area is in `Broiler.ECMAScript.Algorithms`
(`2b0a16b`), with its adapter guide, and the shared stream's next version is `0.1.0-preview.2`, the owner
keeping the `0.1.0` prefix while the stream is a preview *(corrected 2026-10-14; read "and the shared
stream moves to `0.2.0`")*. Corpus revision 8 gives 72
vectors the operation and inputs that decide them, so that the library-level runner can read them, and
every numerics vector the library answers passes at library level. The reference-arm comparison is met
under [`reference-arms.md`](../evidence/sjl-4-001/reference-arms.md) as revised before any run
(bundle `sjl-4-001` section 2). Its first run found one defect of the library, which was corrected
before the run that met the rule: an exact value at a rounding window's upper end was rounded down
under "zero". For this engine, the comparison's corrections are what SJL-4.5 answers:

- Atomics' operands are saturated to 64 bits before they wrap;
- the ToIntegerOrInfinity, ToLength and ToIndex helpers give −0, saturate at their types' bounds, or
  refuse indices the clause admits;
- BigInt powers with a huge exponent throw a .NET overflow, and `asUintN` wraps or divides by zero at
  very large widths;
- a rational's nearest double is not rounded once, and an integer at the overflow threshold gives the
  largest double instead of the infinity;
- BigInt text follows the process's culture;
- a BigInt compared with a literal Number is compared through a double;
- `expm1` and `log1p` are more than a unit in the last place out near zero.

The comparison admits this engine's `cbrt`, `hypot` and the rest of `expm1` and `log1p` as
approximations, which the library's bounds replace.

**The cut-over (SJL-4.5), 2026-10-13 to 2026-10-14.** This engine's numerics are the shared library's: the integer
conversions and Float16, the Math kernels on every path the compiler has for `**`, the exact rounding
of Temporal and Intl, and the BigInt kernel (`0aa8369`, `73f1cd4`, `c565360`, `21a72c2` and
`e4094ec`). `CHANGELOG.md` lists every correction under decision SJL-D-16 (a), each with the vector or
test that names it, and corpus revision 9 (`f2c3597`) retires every divergence field of the area,
this engine's and those of Broiler.VM, whose cut-over (SJL-4.4) corrected its own. This repository's
part of the record:

- **Decision SJL-D-25 is applied, and no public member is removed**, so its consumer check is not
  needed: `JSNumericOperators.ToInt32` and `ToUint32` and `JSValue.IntValue` and `UIntValue` are
  forwarders, `IntegerValue` stays as this plan said, `JSMath.Hypot(double, double)` and
  `JSBigInt.ToNumber` are obsolete forwarders, and `BigIntegerExtensions.FromDouble`, which had no
  caller and which the cut-over did not empty, stays unchanged. `docs/public-api.md` records each.
- **Decision SJL-D-15 (a) found a defect of the library.** The representation's limit that the
  unbounded policy passes, `BigIntPolicy.RepresentationLimit`, was wider than a .NET `BigInteger`
  holds, so a result between the two widths raised a .NET exception, which the decision refuses. The
  limit is now below .NET's, with room for what an operation computes before it refuses (`ff7416b`,
  with a test of the edge and an entry in the library's changelog). Broiler.VM's width policy never
  came near it, and the package its feed holds is unaffected.
- **The search for surviving copies of the area**, made before the record, found hand-written
  conversions in built-ins outside the correspondence's list, most of them `ToIntegerOrInfinity` and
  `ToLength`, and some of them wrong. They call the library now (``eb3ff34``), and the changelog
  lists the corrections. Temporal's `ToIntegerWithTruncation` is one adapter over the library's
  `ToIntegerOrInfinity`.
- **One implementation-defined result changed:** a NaN read from binary16 (`Math.f16round`, a
  `Float16Array` element, `getFloat16`) is the platform's quiet NaN. A NaN written to binary16 keeps
  its sign and the high bits of its payload, as SJL-4.2's record above commits this engine to.
- **The third-party notices** name the two citations the numerics record found, `JSMath.Clz32`'s
  Jurassic reference and `SumPrecise`'s port of the proposal's algorithm, whose engine copy is gone,
  and correct what they said of arm J.
- **Left, as the area verdict leaves them:** the Temporal consumers' quantities (`PlainTime`,
  `PlainDateTime` and `ZonedDateTime`'s `round` round the nanoseconds since midnight) with SJL-10, and
  Intl's mathematical value with SJL-9. `UriHelper` cites Jurassic too, and the notices say that its
  derivation is not yet examined.

**Text algorithms (SJL-5.1), filed 2026-10-14: all six sub-areas accepted.** Broiler.VM's record is
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0070-text-algorithms-move-to-the-shared-algorithms-library.md`
(JSD-0070), which states each condition for whitespace and UTF-16, padding and indices, substitution
templates, URI, base64 and hex, and JSON lexing, each on its own; its input, the correspondence from code
with every member by file and line and each difference numbered, is
`Broiler.VM/docs/evidence/sjl-5-001/correspondence.md`. SJL-5 started on 2026-10-14 by the owner's choice,
before SJL-3 is met, which the programme's ledger records. This repository's part of it:

- **The correspondence was read from code**, at this repository's `62c42e3`, because the area has no
  vectors yet; SJL-5.2's vectors confirm it. Trim is already the library's (SJL-3), and
  `trimCharacters` is gone. Everywhere else this engine holds more of the area than the design named:
  `AdvanceStringIndex` and six inline copies, a third template walk in `JSRegExp.Replace(string, string)`,
  and the relative-index normalisation inline in about twenty built-ins. Each joins the deletion list,
  and SJL-5.5 removes it.
- **The corrections**, each listed in `CHANGELOG.md` at SJL-5.5 under decision SJL-D-16 (a), with the
  correspondence's row: a pad or a repeat past .NET's largest string lets `OutOfMemoryException`
  through (P4); `%TypedArray%.prototype.lastIndexOf` ignores `fromIndex` with more than two arguments
  (P5); a named group's value is converted with the CLR's `ToString` (S2); a RegExp whose
  `Symbol.replace` is absent is matched as a pattern, not searched for as text (S3); `setFromHex`
  parses its whole input before honouring the destination (B1); a non-object options argument is
  ignored and a `null` option read as the default (B2, B3); a detached array encodes as empty (B4);
  JSON nesting past System.Text.Json's default depth is refused (J1); an escaped lone surrogate is
  refused and a raw one lets a .NET exception through (J2, J3); indented JSON
  uses CRLF on Windows (J5); the raw-JSON brand is structural and forgeable (J6); and the brand's
  `rawJSON` property is not enumerable (J7).
  *Corrected 2026-10-14, at SJL-5.2:* the list read "`1e400` is refused (J4)" after the surrogates. Corpus
  revision 12 classifies this engine's first run on the text area, and its vectors `text/parse-number/0001`
  and `0002` show that `JSON.parse("1e400")` and `JSON.parse("-1e400")` answer the infinities, as the
  specification does: on .NET 10, System.Text.Json's `GetDouble` returns the infinity rather than refusing
  the text. J4 is not a correction of this engine, and the changelog does not list it.
- **Three corrections are this engine's own code, not shared:** `split`'s limit with an empty separator
  (X1), the ordinal comparison in `indexOf`, `includes`, `startsWith`, `endsWith` and the non-standard
  `contains`, which use the current culture today (X2), and the raw-JSON brand, which becomes an internal
  slot of this engine's object model (J6).
- **Two policies become parameters, and this engine states its own at SJL-5.5:** the longest string a
  pad builds, which maps to `RangeError` rather than to a .NET exception, and the JSON nesting depth,
  which System.Text.Json's default fixes today and nobody chose. Both are listed in the changelog.
- **Charging.** The shared members keep Broiler.VM's charging units (JSD-0070, decision 3), which costs
  nothing here, where no meter is passed.
- **Provenance.** `UriHelper` cites Jurassic's `GlobalObject.cs`, and the third-party notices say its
  derivation is not yet examined. **It is deleted, not moved**: the shared codec is written from the
  specification and from Broiler.VM's implementation. The notices drop the open entry when it is
  deleted, and say that it was deleted rather than resolved.
- **Decision SJL-D-25 has nothing to dispose of**: every member on this engine's deletion list is
  internal or private. *Corrected 2026-10-15, at SJL-5.5: one is public, `JSRegExp.Replace(string,
  string)`, so the decision has one member to dispose of; it is kept, its body over the shared
  template parser, as [`docs/public-api.md`](../public-api.md) records. The bullet read as above.*
- **Not shared, and staying here:** the JSON drivers (object building, the reviver, source-text
  records, `JSON.stringify`), `split`, `replace` and the evaluation of named groups, string search, and
  the option reading, receiver checks and errors around every codec. Outside the area, and left to their
  milestones as the correspondence names them: U+0085 as white space in the lexer and in `RegExp.escape`
  (SJL-14, SJL-11), `normalize`'s .NET exception on a lone surrogate (SJL-7), `Date.parse`'s white space
  and culture (SJL-6), and the tagged-template path's trimming of a raw segment (SJL-14).
  *Revised 2026-10-16, at SJL-7.1, by Broiler.VM's JSD-0074 (decision 6): U+0085 as white space in the
  scanner and in `RegExp.escape` is SJL-7's, because their white-space lists are on SJL-7.5's deletion list
  and their answer becomes the owner's `IsWhiteSpace`; the lexer and `RegExp.escape`'s other escaping stay
  SJL-14's and SJL-11's. The sentence read as above.*
- **The rules** are committed with this record, before any code of the area: test262 per path
  (`docs/evidence/sjl-5-001/decision-rule.md`, whose base is taken afresh at the commit that carries it,
  because the shared stream's version moved since SJL-4's after run) and the library-level reference
  arms (`docs/evidence/sjl-5-001/reference-arms.md`).

**The text vectors and the first runs (SJL-5.2), 2026-10-14.** Corpus revision 10 adds the area's vectors
(`conformance/vectors/text/`), and revision 11 widens the schema's clause ids, which four of their citations
need and this engine's validator refused. No text vector carries a `per-engine.broiler-js` expectation:
the only results the area leaves to the implementation are the two pads at Broiler.VM's string ceiling,
which hold that engine's. Each engine's first engine-level run on revision 11 is classified at revision
12, and both engines then classify every vector. This engine's differences confirm the rows of the
correspondence listed above, B1 to B4, J1 to J3, J6, J7, P5, S2 and S3, and SJL-5.5 answers each of them.
They refute J4 (above). P4, X1 and X2 have no vector: the vectors pad no further than 2^24 + 1 and test
neither `split` nor string search. J5's three vectors match because the run was on Linux, whose line
ending is LF. Broiler.VM's differences are its rows P1, P2 and S1, which SJL-5.4 answers. Every
difference the runs found is one the area verdict classified, so its falsifier does not fire. The runs are
retained in [`sjl-5-001/engine/`](../evidence/sjl-5-001/engine/). *(Revised 2026-10-14: the paragraph
gave the number of differences each engine's run found, which this plan does not hold; the bundle and
`conformance/vectors/REVISIONS.md` give them.)*

**The implementation (SJL-5.3), 2026-10-14.** The text area is in `Broiler.ECMAScript.Algorithms`
(`7af1424`, merged with Broiler-Platform/Broiler.JS#1028), with its adapter guide, and every text vector
the library answers passes at library level. The reference-arm comparison is met under
[`reference-arms.md`](../evidence/sjl-5-001/reference-arms.md) as revised before any run (bundle
`sjl-5-001` section 3). Its first run did not meet the rule, because of a defect of the classifier, not of
the library; the classifier was corrected and the whole comparison run again. Every difference it found is
a correction the area verdict names. For this engine they are `setFromHex`'s whole-input parse (B1), the
third template walk's `$<name>` (S3), JSON nesting past System.Text.Json's depth (J1) and the lone
surrogates (J2, J3), all answered at SJL-5.5; for Broiler.VM, the pad's refusal with an empty filler (P2).
The run under `en-US` lists the same differences as the run under the invariant culture. The preview
package of the shared stream's next version follows, before either cut-over.

**The cut-over (SJL-5.5), 2026-10-15.** This engine's text algorithms are the shared library's, the
stream's `0.1.0-preview.3` (`0db9042`), which Broiler.VM took the day before at SJL-5.4: the UTF-16
readings and `AdvanceStringIndex` with its six copies, `StringPad` and the relative indices, the
template walks, `UriHelper`, `Uint8Array`'s codecs, and `JSON.parse`'s System.Text.Json reading with its
exception mapping, `QuoteString` and the gap. Every correction the area verdict lists for this engine is
made and listed in [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md) under decision SJL-D-16
(a): P4, P5, S2, S3, B1 to B4, J1 to J3, J6 and J7, with X1 and X2 in this engine's own `split` and
string search; J5 was corrected on 2026-10-14, and J4 was refuted at SJL-5.2. The two policies are
stated there: the longest String a pad or a repeat builds is the longest a .NET string can be, and
`JSON.parse` reads 512 levels, as Broiler.VM does; past either, a `RangeError`. A typed array's `with`
keeps its normalisation inline, because its two steps read two lengths. `UriHelper` is deleted, and the
third-party notices say so. The corpus's next revision retires the fields these corrections answer.
*Added 2026-10-15:* corpus revision 13 retires them, with Broiler.VM's five.

**A data decision: each locale's default numbering system is CLDR's (SJL-5.6), 2026-10-15.** The owner
decided on 2026-10-15 that this engine takes each locale's default numbering system from CLDR, release
47.0.0, where every locale defaulted to `latn`: the table is generated in this repository by
`scripts/intl/generate_default_numbering_systems.py` from the `cldr-json` release's `cldr-numbers-full`
package, pinned by its integrity, and embedded in `Broiler.JavaScript.BuiltIns`, as decision SJL-D-6 (c)
has it for locale data; SJL-9 moves it into the shared locale data. The same change makes a Temporal
value's digits follow the numbering system, as a `Date`'s did. Bundle `sjl-5-001`'s first after run
found the need: `intl402/DateTimeFormat/prototype/format/temporal-objects-no-time-clip-non-latin-numerals.js`
passed at the base only because `String.prototype.includes` compared by culture, and correction X2
showed this engine's Latin digits for `ar-EG`. This is the dated data decision part A's class C5 of the
bundle's rule names: a row whose verdict changes because of it is listed there with this decision, and
in [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md).

**Date (SJL-6.1), filed 2026-10-15: all four sub-areas accepted, and decision SJL-D-19 recorded.**
Broiler.VM's record is
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0072-date-algorithms-move-to-the-shared-algorithms-library.md`
(JSD-0072), which states each condition for the time arithmetic, the civil-day helpers, the Date string
grammar and the Date string renderings, each on its own; its input, the correspondence from code with every
member by file and line and each difference numbered (T, D, G, L, P, R), is
`Broiler.VM/docs/evidence/sjl-6-001/correspondence.md`. SJL-6 began with SJL-6.0, a Broiler.VM workflow that
changes no product code, before SJL-3 is met, which the programme's ledger records. This repository's part
of it:

- **The correspondence was read from code**, at this repository's `bc3597f`, and every cell of this
  engine's that a row rests on was run through the shell in UTC and in `America/New_York`; SJL-6.2's vectors
  confirm it. **Four of the area design's differences are already gone**: SJL-4.5 made `TimeClip` fold -0,
  `MakeTime` and the setters truncate, and the setters convert large arguments without a wrap, and
  `Broiler.JS/CHANGELOG.md` lists them there; they are not SJL-6's. Everywhere else this engine holds more
  of the area than the design named: the civil-day arithmetic in nine types (`JSTemporalInstant`,
  `JSTemporalPlainDate`, `JSTemporalPlainDateTime`, `JSTemporalPlainMonthDay`, `JSTemporalPlainYearMonth`,
  `JSTemporalZonedDateTime`, `TemporalIsoString`, `TemporalArithmeticCalendar`, `TemporalNonIso`) and in
  `JSDate.DaysInMonth`; MakeFullYear inline in three places; and Intl's dozen reads of `JSDateMath`. Each
  joins the deletion list, and SJL-6.5 removes it, with the dead `JSDate.ToDateTime`, `invalidDate` and
  `IsValid(JSValue, ...)`.
- **The corrections**, each listed in `CHANGELOG.md` at SJL-6.5 under decision SJL-D-16 (a), with the
  correspondence's row: a date-only string read as local time in years 0001 to 9999 (P1); a month at or
  past 2^53 that saturates (T4); a setter that writes the invalid date before converting its later
  arguments (T3); the ISO week of a date in a negative year (G2); two offsets for one zone, the base one
  for storage and the components constructor and the DST-aware one for `toString` (L2); no disambiguation of
  skipped and repeated local times, and the offset before a transition read after it (L3); the offset of
  the moment of the call for a date outside `DateTimeOffset`'s range (L4); this engine's renderings outside
  0001 to 9999 that `Date.parse` cannot read back (P9); and the culture's minus sign in `toString`'s
  `GMT-hhmm` (R3).
- **Date's storage becomes the time value**, and five defects of the `DateTimeOffset` storage go with it,
  each listed in the changelog: a valid date outside 0001 to 9999 refused by twelve setters (D1), a date
  stored raw that those setters cannot change (D2), the instant 0001-01-01T00:00Z read as invalid (D3),
  `getTimezoneOffset` without a raw path (D4), and a structured clone that loses a raw date (D5). They are
  this engine's own code, not shared.
- **Decision SJL-D-19 is recorded here**: the shared library defines the local-time provider and the
  specification's `LocalTime` and `UTC` over it, and **this engine's provider wraps the host's
  `TimeZoneInfo`**, read at each instant with its daylight-saving time, which corrects L2 to L4. Its
  `Temporal` reads its bundled tzdata, so `Date` and `Temporal` read different data until SJL-8 gives the
  provider the shared IANA data; the milestone's "Either way" is corrected there. The clock and locale
  providers of the same decision are defined at SJL-9 and SJL-10.
- **The order of grammars**: `Date.parse` calls the shared strict dialect, then this engine's own fallback
  (`DateParser`'s `N/N/N` heuristic, its other format lists and its culture parses, which stay under
  SJL-D-17), then Broiler.VM's dialect for what both refuse, which reads this engine's renderings outside
  0001 to 9999. `TryParseExtendedIso` and `DateParser`'s lists of the format are deleted. **Whether the
  fallback keeps what only they accepted beyond the format** - fractions of zero or of four to nine
  digits, lower-case `t` (P4, P6) - is decided at SJL-6.5 and listed in the changelog.
- **Charging.** The shared parser keeps Broiler.VM's charging units (JSD-0072, decision 4), which costs
  nothing here, where no meter is passed.
- **Decision SJL-D-25.** `DateParser` is public, but its `Parse` is internal, so it keeps its type and has
  nothing to forward. `JSDate`'s public constructor from a `DateTimeOffset`, its `Value` and `DateTime`, the
  unused public `epoch`, `JSValue.CreateDate` and `ClrProxy.Marshal`'s date overloads keep their signatures
  over the time value, as derived views; what each answers outside 0001 to 9999 is decided and recorded in
  [`docs/public-api.md`](../public-api.md) at SJL-6.5.
- **Not shared, and staying here:** storage, argument conversion and its order, the culture-dependent
  fallback and the .NET format string of `toLocale*` (SJL-D-17), and the CLR converters, whose
  `DateTime.Parse` reads a guest string into a host type and is not `Date.parse`. Outside the area, and left
  to their milestones as the correspondence names them: `Temporal`'s grammar and limits (SJL-10), the
  bundled tzdata and the non-ISO calendars (SJL-8), and Intl's `ArgumentOutOfRangeException` for
  `formatToParts(8.64e15)` (SJL-9).
- **The rules** are committed with this record, before any code of the area: test262 per path in UTC and
  in `America/New_York`, the latter the non-UTC host run of clause 4, each taken by `test262 evidence`
  with its new `time_zone` input (`docs/evidence/sjl-6-001/decision-rule.md`), and the library-level
  reference arms (`docs/evidence/sjl-6-001/reference-arms.md`).

**The Date vectors and the first runs (SJL-6.2), 2026-10-15.** Corpus revision 14 adds the area's vectors
(`conformance/vectors/date/`), from the reference computation `scripts/conformance/dateref.py`, written from
the specification's text and the Temporal proposal's ISO calendar and tested against Python's proleptic
Gregorian calendar. No vector's outcome depends on the time zone, so each one holds in UTC and in
`America/New_York` alike; this engine's runner was run in both, from one build. The numeric slash dates are
the area's `impl-defined` vectors, with this engine's expectation from its `N/N/N` heuristic, which SJL-D-17
keeps, and this engine meets it. Each engine's first engine-level run on revision 14 is classified at revision
15, and both engines then classify every vector. This engine's differences confirm the rows of the
correspondence listed above, T3, T4, G2, L2, P1 and P9, and D1 to D3; SJL-6.5 answers each of them. Rows L2
and P1 show only in New York, and under UTC their fields are reported as no longer holding, which retires none
of them: a field goes when its correction is listed and the run in New York shows it gone. Two rows are
refined, and none is refuted: the vectors show T3, which was worked from the code, for seven setters, while
`setUTCHours`, `setUTCFullYear` and `setFullYear` convert as the specification says; and L2's mechanism shows
also in the years before the zone's standard time, where the tzdata's offset is local mean time. L3, L4, L5,
D4, D5, R3 and R4 have no vector, nor have P4 to P7 beyond the slash dates: their outcome depends on the zone,
the culture or the host, or lies outside ECMA-262, and part B's run and the reference arms carry them.
Broiler.VM's differences are its rows T4, G3 and P9, which SJL-6.4 answers. Every difference the runs found is
one the area verdict classified, so its falsifier does not fire. The runs are retained in
[`sjl-6-001/engine/`](../evidence/sjl-6-001/engine/), and the bases of both parts of this repository's test262
rule were taken at its commit, `5fef116`, by `test262 evidence`.

**The implementation (SJL-6.3), 2026-10-15.** The Date area is in `Broiler.ECMAScript.Algorithms`
(`c69b70c`, with two defects the comparison's first run showed fixed at `b75d12e9`): `EcmaTime`,
`IsoCalendarMath`, the local-time provider with UTC's disambiguation, `EcmaDateParser` in the strict dialect
and in Broiler.VM's, and `EcmaDateFormatter`, with its adapter guide; every Date vector the library answers
passes at library level, and the vectors whose outcome is an engine's own, the setters' conversions and the
local setters, are engine-level only. The reference-arm comparison is met under
[`reference-arms.md`](../evidence/sjl-6-001/reference-arms.md) as revised before any run and, on two points,
after its first judged run was seen, which is weaker evidence; the bundle states one judgement, on arm V's
negative control (bundle `sjl-6-001` section 3). The first run did not meet the rule: the library answered
`Day(-0)` with -0 and reset an earlier clock's seconds in Broiler.VM's dialect, the local-time arbiter's
boundary at 1913 did not hold, and the classifier had defects; the library was fixed, the rule revised, and
the whole comparison run again. Every correction it found is a row the correspondence names, or refines: for
this engine, the ISO week in negative years (G2), local time at a transition (L3) and past .NET's range (L4),
and its own renderings read back (P8, P9), with the fraction of a time value or a year dropped or kept, and,
under `fa-IR` and `ar-SA`, a read-back through the culture's calendar, a second way P8 fails; for Broiler.VM,
`Day(-0)`, a fractional year and its renderings of negative years read back (P9). The other differences are
strings outside the format that each engine reads its own way. Corpus revision 16 holds each of their kinds in
an `impl-defined` vector, with this engine's reading before its cut-over as its expectation, and this engine's
runs on it, in UTC and in New York, meet every one. The preview package of the shared stream's next version,
`0.1.0-preview.4`, follows, before either cut-over.

**Broiler.VM's cut-over (SJL-6.4), 2026-10-15.** The stream's next version is `0.1.0-preview.4` (`e04874d`),
packed from this repository and admitted to Broiler.VM's committed feed before it is published; publishing
it is the owner's to choose. Broiler.VM's Date area is the library's, its getters and setters keeping its
UTC provider's identity, and its corrections T4, G3 and P9 are made; its pin of the corpus moves to revision
16, where each of its sixteen divergence fields reports that it no longer holds, and its record is its
JSD-0073. This engine's cut-over, SJL-6.5, follows on the same version; a later corpus revision retires
Broiler.VM's fields, with this engine's. *Added 2026-10-15:* on the owner's instruction, `Publish shared
libraries` published `0.1.0-preview.4` to nuget.org from `d1e4b11` (run 38813070829), whose library code is
`e04874d`'s, and tagged it `ecmascript-v0.1.0-preview.4`; Broiler.VM's feed now holds the package as
published.

**This engine's cut-over (SJL-6.5), 2026-10-16.** This engine's Date area is the shared library's, on the
stream's `0.1.0-preview.4`, which nuget.org serves. Its getters, setters, constructor, `Date.UTC`, `getYear`
and `setYear` call `EcmaTime`; its local time is `EcmaLocalTime` over its provider of the host's
`TimeZoneInfo`, `HostLocalTimeProvider` (SJL-D-19); `Date.parse` reads the strict dialect, then `DateParser`,
its own fallback, then Broiler.VM's dialect; its renderings are `EcmaDateFormatter`'s; and Temporal's and
Intl's civil-day arithmetic is `IsoCalendarMath`'s and `EcmaTime`'s. **A `Date`'s storage is its time value
alone.** The deletion list is absent, with what JSD-0072 added to it, and every correction the area record
names is made and listed in [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md): those the corpus
holds, and D4, D5, L3, L4, R3 and P8, which `SharedDateAlgorithmTests` holds, its zone's cases under
`America/New_York`. **The readings the record left to this milestone are decided** (P4 to P6, R5):
`Date.parse`'s own grammar keeps the format's leniencies it read, a fraction of none to nine digits and an
offset without its colon, now in every form and year of the format; and a .NET format string of
`toLocaleString` formats the years 1 to 9999 only, and is a `RangeError` outside them. By the record's order,
`Date.parse` now reads in Broiler.VM's dialect what both of this engine's grammars refuse, a day past its
month's length among them, carried (P5). The CLR views' answers outside the years 1 to 9999 are recorded
in [`docs/public-api.md`](../public-api.md) under SJL-D-25. **One change the record did not name** follows
from the deletion: a Gregorian-family `Temporal.PlainMonthDay` with a year was bound-checked through a civil
copy that carried a month past 12 past the year, and is now bound-checked with its month as `constrain`
regulates it; the changelog lists it. The next corpus revision retires both engines' Date divergence
fields, and gives this engine's expectation of the strings outside the format its reading since the
cut-over; Broiler.VM's pin moves to it. *Added 2026-10-16:* corpus revision 17 does, and both engines'
expectations of two of those strings now agree on Broiler.VM's dialect's reading, which a library-level run
compares the library with: its runner reads a string outside the format as both engines ask the library
to, in the strict dialect and then, for what that refuses, in Broiler.VM's.

**Unicode (SJL-7.1), filed 2026-10-16: all six sub-areas accepted, placed with Broiler.Unicode, and
decision SJL-D-6 recorded for the first data area.** Broiler.VM's record is
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0074-unicode-moves-to-broiler-unicode-under-the-owner-conditions.md`
(JSD-0074), which states each condition of the extraction gate for properties and lexical classes, case
folding, case mapping, normalization, properties of strings and segmentation, each on its own; its input,
the correspondence from code with every member by file and line and each difference numbered (P, I, W, F,
C, N, S, G), is `Broiler.VM/docs/evidence/sjl-7-001/correspondence.md`. SJL-7 started before SJL-3 is met,
by the owner's choice, which the programme's ledger records. This repository's part of it:

- **The home is Broiler.Unicode**, under decision SJL-D-1 (b), because it adopts SJL-7.1's four owner
  conditions by a dated record in its `docs/roadmap.md`: the programme's conventions, its roadmap, an
  assurance gate, and generator inputs that are pinned and hashed. It meets them at SJL-7.2. **If it does
  not meet one by the end of SJL-7.2**, the sub-areas that condition reaches come to this repository, under
  `Broiler.ECMAScript`, by a dated record naming the condition, as the table above says.
- **The correspondence was read from code**, at this repository's `bdb1a3ca`, whose engine sources are
  `4d70821e`'s, and every cell of this engine's that a row rests on was run through the shell, on the
  host's ICU 74.2, which carries Unicode 15.1, as the hosted runners' does; SJL-7.3's vectors confirm it.
  **Two sub-areas are adoptions here, not moves**: this engine implements no normalization of its own, and
  takes graphemes from the runtime and has no sentence segmentation, so pinned data and the owner's
  algorithms replace the host's. **Its case folding under `u` and `v` is already Broiler.Regex's**, from the
  same derivation as Broiler.VM's, and the two agree; folding without `u` is the .NET translator's (F2),
  which SJL-11 retires.
- **The corrections**, each listed in `Broiler.JS/CHANGELOG.md` at SJL-7.5 under decision SJL-D-16 (a), with
  the correspondence's row: `\p{…}` names matched loosely, `Katakana_Or_Hiragana` refused, a name falling
  through to .NET's `\p`, and General_Category refused inside a `v`-mode set operation (P1 to P4);
  identifiers and group names that do not subtract Pattern_Syntax, and group names without the Unicode 17
  tables (I1, I3); U+0085 as white space to the scanner, `RegExp.escape` and the segmenter's words (W1);
  U+00AA and U+00BA not Cased (C1); the Turkic lower case through the runtime's older data (C4); four
  Soft_Dotted code points missing (C5); More_Above across a class-0 mark (C6); a list of locales that
  reaches `CultureInfo` and throws (C7); `null` and no argument read as the host's culture (C8); a lone
  surrogate that throws in `normalize` and `localeCompare` (N1); and sentence and word boundaries that do
  not follow UAX #29 (G1, G2). **Pinned data replaces the host's**, a correction of class C5 under
  SJL-D-6, for the identifier classes (I2), the default case mappings (C3), normalization at Unicode 16
  and 17 and under invariant globalization (N2, N3) and grapheme clusters (G3); and `isWordLike` becomes
  the owner's word status (G4).
- **The deletion list grows**, as JSD-0074 records: beside the milestone's, `IsValidNativeFunctionToStringName`'s
  `char.IsLetter`, the collator's `IsNormalized`, and the translator's loose property lookup with its
  `NormalizeKey`, which becomes a call to the owner's exact lookup while the translator itself stays
  SJL-11's. **Already gone**: `JSString.trimCharacters` and `NumberParser`'s list, since SJL-3.5, whose
  white space is the shared set in `trim`, `Number` and `parseFloat`.
- **Decision SJL-D-6 (c), as this engine applies it**: this engine references the owner's code packages
  directly, where today it reaches `Broiler.Unicode.Properties` only through Broiler.Regex while its code
  calls that package and the emoji package directly. It embeds the tables the owner's generator writes, in
  an assembly SJL-7.5 names, held byte for byte by a test of rule SL7's shape in this repository, and passes
  them to the owner's algorithms through the owner's provider. It references no package that carries
  tables.
- **Charging.** The owner's casing, normalization and segmentation take a meter and a maximum result
  length. This engine passes no meter, as with every shared area, and passes its own string ceiling as the
  maximum, keeping its own error.
- **Decision SJL-D-25.** `StringSpan.Trim`, `TrimStart` and `TrimEnd` are public and have no caller; their
  disposition is decided at SJL-7.5 and recorded in [`docs/public-api.md`](../public-api.md).
  `CharExtensions`' members are internal.
- **Not shared, and staying here:** receiver and argument conversion; this engine's locale
  canonicalisation and the mapping of a locale to the owner's language enumeration; its string ceiling and
  errors; `Intl.Segmenter`'s objects and iterators, whose shape differs from Broiler.VM's (G7) and is
  outside the area. Outside the area, and left to their milestones: the .NET translator and its folding
  and line terminators (F2, W5; SJL-11), the lexer (SJL-14) and collation, whose decomposition step alone
  moves here (SJL-17).
- **JSD-0031 §4(b)'s fifth reason**, the owner's runtime cost, is answered for this engine by its own
  measurement of `normalize`, casing and identifier scanning. If its lane is not a controlled lane, the
  part is void, as SJL-3's was, and the reason stays open for this engine.
- **The rules** are committed with this record, before any code of the area: test262 per path with
  negative-metadata tests included, so that the property-name and identifier corrections are inside the
  population, through `test262 evidence`'s new `include_negative` input, and the measurement over
  `UnicodeBenchmarks` (`docs/evidence/sjl-7-001/decision-rule.md`, parts A and B). The library-level rules,
  Unicode's own test files, the byte-for-byte generation and the reference arms, are the owner's, in
  Broiler.Unicode's bundle of the same name.

## Invariants

1. **No edge between the engines, in either direction.** No shared project references a
   `Broiler.JavaScript.*` engine project or package, or anything in Broiler.VM (rule SL1). This engine
   references the shared packages the way any consumer does.
2. **The shared libraries implement the specification only.** Extensions stay in this engine's
   adapters (SJL-D-17).
3. **Adapters hold no algorithm.** An adapter converts values, orders coercions, raises this engine's
   errors and charges; the algorithm is the library's.
4. **Deletion in the same change.** A cut-over that leaves a member of its area's deletion list in
   place has not removed the duplication.
5. **Each repository's gate is met by its own evidence.** Agreement between the engines is a
   cross-check, never an oracle; the two test262 runners' totals are never compared.
6. **No cross-engine speed comparison**, in any document. Each measurement compares this engine with
   itself, under a rule written before the run.

## Items

One item per programme milestone with Broiler.JS or home clauses, in the programme's delivery order.
**Owner of every item: the Broiler.JS maintainer**, with the JavaScript profile owner for the
programme's coordination; one person holds both roles, so no item here is independently confirmed by
a second reader. Each exit criterion is the Broiler.JS and home clauses of the named milestone file;
the summary here does not replace them.

| Item | Next action | Exit criterion |
|---|---|---|
| **SJL-0** — records, decisions and baselines | File this plan pair and the revisions of `dependencies.md`, `public-api.md`, `README.md`, `Assemblies.md`, `Roadmap.md`, `ModernizationDelivery.md`, `Modernization.md` and `builtins-vs-clr-types.md`; commit the decision-rule files; collect bundle `docs/evidence/sjl-0-001/` | Clauses 8 to 11 of `milestones/sjl-0-records-decisions-and-baselines.md`: every document above carries a dated revision, the plan pair exists, the bundle retains test262 per path, a public API listing, package contents and nuspecs and the pilot measurement at a named commit, and the decision-rule files hold no figure |
| **SJL-1** — the shared conformance vectors | Write the vector schema, the validator and its negative-control file, then the number vectors, then the engine-level runner | Home clauses 1 to 3: the validator refuses the malformed control and any vector without an independent source; every number vector has provenance; the engine-level run is retained with every difference classified under `docs/evidence/sjl-1-001/decision-rule.md` |
| **SJL-2** — the admission path | Create the shared folder with its conventions, the `SL` rules, the API baseline, the assurance gate, the version stream and the Native AOT probe *(corrected 2026-10-12: the list named "the prefix reservation" before the probe, which the owner withdrew)* | Clauses 7 to 10: SL1 to SL10 `Active` with witnesses; the API baseline and the assurance gate in CI, each watched failing on an injected drift; the probe pre-release packs, restores and passes package validation; the Native AOT probe publishes with zero warnings and runs |
| **SJL-3** — number conversion, the pilot | Implement the algorithms library's number area; run the reference arms; cut this engine over from `Parser`, `Runtime` and `BuiltIns` | Clauses 1 to 7: the library under the conventions with its vectors and reference arms classified; every deletion-list member gone and every public disposition applied; test262 per path with no path passing at base failing after; corrections in the changelog; the measurement under `docs/evidence/sjl-3-001/decision-rule.md` |
| **SJL-12** — the frontend contract and in-place preparation | Remove the parser's ambient state, mutable statics, `Runtime` references and parse-time desugaring in place (Modernization MOD-M2-3 to MOD-M2-5 are this half), and record the frontend contract here | Clauses 1 to 3: an architecture test with a witness enforces the parser's independence; test262 per path meets the rule; the contract record is in this plan |
| **SJL-4** — numerics | The area's library work and cut-over; SJL-D-15 recorded | Its Broiler.JS clauses: deletion list absent, dispositions recorded, test262 per path, vectors, corrections listed |
| **SJL-5** — text algorithms | As above | Its Broiler.JS clauses |
| **SJL-6** — Date | As above, with the provider interfaces of SJL-D-19 and Date storage migrated | Its Broiler.JS clauses, the non-UTC host run included |
| **SJL-7** — Unicode | Move off host Unicode data onto the owner's API | Its Broiler.JS clauses: no host Unicode API in the areas covered |
| **SJL-8** — time zones and calendars | Move the generators and pins here and `Temporal` off host zones and .NET calendars | Its Broiler.JS clauses, the reference datasets included |
| **SJL-11** — regular expressions | After Broiler.Regex meets every requirement and implements duplicate named groups, retire the translator and the validators | Clauses 4 to 7: translator gone, test262 `RegExp` per path, duplicate named groups still working, corrections listed; `Component.md` §3 closed |
| **SJL-10** — Temporal core | The area's library work and cut-over | Its Broiler.JS clauses, the polyfill dataset included |
| **SJL-9** — locale data and Intl | Move the CLDR generator and pins here; host globalization only behind the optional provider (SJL-D-22 (c)) | Its Broiler.JS clauses |
| **SJL-17** — collation | Share the collation algorithm after SJL-9 | Its Broiler.JS clauses: host collation only behind the optional provider |
| **SJL-13** — the shared syntax library | Implement the frontend's syntax half in this repository | Home clauses 1 to 3: under the contract and conventions, the parse corpus and syntax tests at library level, SL9 and the Native AOT probe |
| **SJL-14** — Broiler.JS on the shared syntax library | Move the compiler and the portable compiler onto the frontend; make `Ast` and `Parser` obsolete adapter packages (SJL-D-12 (i)) | Clauses 1 to 6, the Native AOT sample regenerated and run included |
| **SJL-15** — shared static semantics | Implement the semantic model here; delete the scattered early errors and scope analysis | Home clauses 1 to 3 and Broiler.JS clauses 10 and 11 |
| **SJL-16** — close-out | Sweep the deletion lists, retire the reference arms, confirm SJL-D-18 | The close-out's sweep in this repository |

The state of each item is one line in [`SharedLibraries.status.md`](SharedLibraries.status.md).
Following this directory's convention, no changing count is written here: a test262 total, a vector
count or a measurement belongs in a bundle under `docs/evidence/`, and the status record links it.
