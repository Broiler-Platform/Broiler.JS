# Changelog — Broiler.JS

Changes to the engine (the `Broiler.JavaScript.*` packages) a consumer can observe, newest first.
This file starts with the shared JavaScript libraries programme: under its decision SJL-D-16 (a), a
cut-over that moves an area to a shared library lists here, by name, every result that changes
because the shared implementation follows the specification where this engine did not, each with the
vector or test262 path that names it. Earlier changes are recorded in the history and in
[`docs/public-api.md`](../docs/public-api.md). The shared libraries have their own changelog,
[`Broiler.ECMAScript/CHANGELOG.md`](../Broiler.ECMAScript/CHANGELOG.md).

## Unreleased

### Number conversion moves to `Broiler.ECMAScript.Algorithms` (2026-10-12, milestone SJL-3.5)

`Broiler.JavaScript.Parser`, `.Runtime`, `.BuiltIns` and `.Storage` reference the shared library,
and every number conversion is its: `Number::toString` at every radix, `toFixed`, `toExponential`
and `toPrecision`, `StringToNumber`, `parseInt` and `parseFloat`, the values of numeric literals,
array-index and canonical numeric keys, and the white-space set of `String.prototype.trim` and the
parsers. The built-ins keep their coercions, their RangeErrors and their string creation. The API
dispositions are in [`docs/public-api.md`](../docs/public-api.md).

**Corrections.** Each was a `divergence.broiler-js` field of the shared corpus
(`conformance/vectors/numbers`, revision 4), with its reason in
`scripts/conformance/number-divergences.txt`; corpus revision 5 retires the fields, and
[`docs/evidence/sjl-3-001/engine/retired-divergences.tsv`](../docs/evidence/sjl-3-001/engine/retired-divergences.tsv)
lists every retired vector under its reason. The argument handling the corpus does not hold is
covered by `Broiler.JavaScript.BuiltIns.Tests/SharedNumberConversionTests.cs`.

- **`Number.prototype.toString(radix)` at a radix other than 10 gives the shortest digits that read
  back as the value** (reason `radix-longer`, 233 vectors of `numbers/to-string-radix`, such as
  `0051`). The fraction was expanded digit by digit, up to 1,100 digits; the digit cap is gone with
  it.
- **Its digits denote the value** (`radix-inexact`, 124 vectors, such as `numbers/to-string-radix/0065`).
  The fraction was expanded in double arithmetic, whose rounding error entered the digits.
- **Its integer part is exact at 2^63 and beyond** (`radix-64-bit`, 238 vectors, such as
  `numbers/to-string-radix/0015`). It was converted through a 64-bit integer.
- **A radix whose `ToIntegerOrInfinity` is 0 is a RangeError: `0`, `-0`, `null` and `NaN`**
  (`radix-argument`, `numbers/to-string-radix-arguments/0002`, `0005`, `0006` and `0010`). The first
  three were read as radix 10, and `NaN` escaped as a host `ArgumentException`.
- **`toPrecision` coerces its argument before it looks at the value, and checks the truncated
  precision** (`precision-argument`, `numbers/to-precision-arguments/0003`): `(1.5).toPrecision(100.5)`
  is 100 digits instead of a RangeError, and `Infinity.toPrecision(x)` now calls `x`'s `valueOf`, as
  step 3 does before step 4.
- **`parseInt(null, radix)` and `parseInt(undefined, radix)` parse `"null"` and `"undefined"`**:
  `parseInt(null, 36)` is 1112745 instead of NaN (ECMA-262 §19.2.5 step 1;
  `SharedNumberConversionTests`). The radix is converted once, where it was converted twice.
- **The white-space set is the specification's**, generated from the pinned Unicode 17.0.0 data:
  `parseInt` no longer trims U+0085 NEXT LINE, which is not white space (`next-line-white-space`,
  `numbers/parse-int/0064` to `0070`); it trimmed with .NET's `string.Trim`. U+FEFF was, and is,
  trimmed by every parser and by `String.prototype.trim`, which read the set from a list of its own
  before. `BigInt(string)` and the comparisons of a BigInt with a string (StringToBigInt) trimmed
  .NET's set: they trim U+FEFF now, so `BigInt("﻿1")` is `1n` instead of a SyntaxError, and no
  longer trim U+0085 (`SharedNumberConversionTests`).
- **Hexadecimal, octal and binary integers beyond 2^53 are rounded to the nearest double, ties to
  even**, as literals and as strings (`non-decimal-rounding`, 62 vectors of `numbers/literal` and
  `numbers/string-to-number`, such as `numbers/literal/0213`). They were rounded twice.
- **`Number()` and `parseFloat` complete for a decimal significand of more than 309 digits**
  (`long-significand-hang`, 6 vectors, such as `numbers/parse-float/0777`), **and give its value, not
  NaN, below 1e-308** (`long-significand-nan`, 12 vectors, such as `numbers/parse-float/0918`).
- **Numeric literals the grammar refuses are SyntaxErrors**: `07.5` (`legacy-octal-fraction`,
  `numbers/literal/0091`), `1e` (`empty-exponent`, `0105` and `0106`), `01n` and `08n`
  (`bigint-leading-zero`, `0137` to `0140`) and `.5n` (`bigint-fraction`, `0145` and `0146`). The
  scanner's digit runs let them through and the value code read them; the scanner now takes each
  literal's value from the shared library, which refuses them.
- **Canonical numeric keys are `Number::toString`'s.** `"1E+21"` and `"1E-07"`, the .NET `"R"`
  format's spellings, were classified as canonical numeric strings, so a typed array ignored them as
  keys; they are ordinary property keys now (`SharedNumberConversionTests`).

- **A Number equals a String through ToNumber only.** Three fast paths of `==` compared .NET's text of
  the number with the string, so `NaN == "NaN"` and `"NaN" == NaN` were `true`; they are `false`
  (ECMA-262 §7.2.14; `SharedNumberConversionTests`). The search for surviving duplicates of the area
  (bundle sjl-3-001, milestone SJL-3.7) found them.
- **The portable compiler's generated C# holds every number constant exactly.**
  `PortableCompiler.EmitCSharp` wrote a constant in .NET's `"R"` format, which does not read back as
  the same double for every value: `2.9802322387695312e-8` (2\*\*-25) was written
  `2.980232238769531E-08`, the double below it, and an infinite one, such as the literal `1e400`, was
  written `Infinity`, which C# does not compile. Each is written as its IEEE-754 bits now
  (`PortableCompilerTests`). The same search found it.

**Not corrected here.** A `NonOctalDecimalIntegerLiteral` (`08`, `09.5`) in strict mode code is still
accepted (`strict-non-octal-decimal`, `numbers/literal/0082`, `0084`, `0088` and `0090`): strictness
is the compiler's, and item SJL-14 answers it.

**Kept, as decision SJL-D-17 has them:** the `m` (`Decimal`) literal suffix, `Number.prototype.clz`,
and `toLocaleString`'s .NET format strings.
