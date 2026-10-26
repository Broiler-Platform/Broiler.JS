# Broiler.ECMAScript Code Assurance

GENERATED - DO NOT EDIT MANUALLY. Regenerate with
`dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.JS/Broiler.ECMAScript`, which rewrites this file,
`HUMAN_REVIEW.md`, `assurance.manifest.json` and every generated source header from the
product tree.

**No code unit in this component carries a decision on its human line yet.** This report
records that absence precisely. It is not a claim that the code is reviewed, assured or safe,
and the figures below are the measurement of how far from that claim the per-unit record is.

## Summary

| Metric | Value |
|---|---:|
| Files scanned | 106 |
| Files not covered | 0 |
| Files carrying an annotation | 106 |
| Code units | 1833 |
| Relevant | 1384 |
| Exempt by predicate | 449 |
| Annotated | 1384 of 1384 (100%) |
| Human reviewed | 0 of 1384 (0%) |
| Unverified | 1384 |

## Review states

| State | Count |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 1384 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 449 |

## IP risk

| Value | Units |
|---|---:|
| None | 51 |
| Low | 1378 |
| Medium | 8 |
| High | 0 |
| Unknown | 0 |
| *not annotated* | 0 |

## Security risk

| Value | Units |
|---|---:|
| None | 77 |
| Low | 758 |
| Medium | 457 |
| High | 145 |
| Critical | 0 |
| *not annotated* | 0 |

## Resource impact

| Metric | Value |
|---|---:|
| Maximum | 9 / 10 |
| Average over annotated units | 1.3 / 10 |
| Units scored | 1384 |

## High-security review areas

- `Broiler.ECMAScript.LengthLimit.Admits(long)` in `Broiler.ECMAScript.Algorithms/Limits.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.DepthLimit.Admits(int)` in `Broiler.ECMAScript.Algorithms/Limits.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.Multiply(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.Divide(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.Remainder(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.Power(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.LeftShift(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.SignedRightShift(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.AsIntN(ulong, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.AsUintN(ulong, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.FromIntegralNumber(double)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.ToNumber(BigInteger, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.CompareToNumber(BigInteger, double, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.LinearCost(int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.ProductCost(int, int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.LeafCost(int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.Bounded(BigInteger, BigIntPolicy, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.Shift(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntPolicy` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntPolicy.RepresentationLimit` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntPolicy.BigIntPolicy(long)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntPolicy.Unbounded` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntPolicy.MaximumBits` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntPolicy.Admits(BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.ToString(BigInteger, int, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.StringToBigInt(ReadOnlySpan<char>, BigIntPolicy, WorkCharge?, out BigInteger, out int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.ToDecimalString(BigInteger, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.AppendDecimal(StringBuilder, BigInteger, int, int, List<BigInteger>, List<int>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.AppendRadix(StringBuilder, BigInteger, int, int, int, ulong, int, List<BigInteger>, List<int>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.ParseRadix(ReadOnlySpan<char>, int, BigIntPolicy, WorkCharge?, out BigInteger, out int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.ParseDecimal(ReadOnlySpan<char>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.ParseDecimalPart(ReadOnlySpan<char>, int, List<BigInteger>, List<int>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigNumber.ShiftLeft(int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigNumber.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.NumberParsing.StringToNumber(ReadOnlySpan<char>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/NumberParsing.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.NumberParsing.ParseInt(ReadOnlySpan<char>, int, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/NumberParsing.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.NumberParsing.Integer(ReadOnlySpan<char>, int)` in `Broiler.ECMAScript.Algorithms/Numbers/NumberParsing.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.NumericLiterals.Evaluate(ReadOnlySpan<char>, bool, LengthLimit, out NumericLiteral, out Failure, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/NumericLiterals.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.NumericLiterals.BigInt(ReadOnlySpan<char>, int, NumericLiteralForm, LengthLimit, WorkCharge?, out NumericLiteral, out Failure)` in `Broiler.ECMAScript.Algorithms/Numbers/NumericLiterals.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.MonthCode.Parse(ReadOnlySpan<char>, out MonthCode)` in `Broiler.ECMAScript.Algorithms/Temporal/Calendars/MonthCode.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalCalendars.Canonicalize(ReadOnlySpan<char>, out CalendarId)` in `Broiler.ECMAScript.Algorithms/Temporal/Calendars/TemporalCalendars.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParse(ReadOnlySpan<char>, ReadOnlySpan<TemporalGoal>, out IsoParseResult?, out TemporalParseError, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParseInstant(ReadOnlySpan<char>, out BigInteger, out TemporalParseError, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParseTimeZoneString(ReadOnlySpan<char>, out TimeZoneIdentifierRecord, out TemporalParseError, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParseTimeZoneIdentifier(ReadOnlySpan<char>, out TimeZoneIdentifierRecord, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParseCalendarString(ReadOnlySpan<char>, out string?, out TemporalParseError, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParseOffsetString(ReadOnlySpan<char>, out long)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParseDuration(ReadOnlySpan<char>, out DurationFields, out TemporalParseError, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.IsTimeZoneIdentifier(ReadOnlySpan<char>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.IsUtcOffset(ReadOnlySpan<char>, bool)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.IsAnnotationValue(ReadOnlySpan<char>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parse(ReadOnlySpan<char>, ReadOnlySpan<TemporalGoal>, out IsoParseResult?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.IsWholeTimeZoneIdentifier(ReadOnlySpan<char>)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.IsAnnotationValueCore(ReadOnlySpan<char>)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.IsAnnotationKey(ReadOnlySpan<char>)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Duration(ReadOnlySpan<char>, out DurationFields)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.AnnotatedDateTime(bool, bool, bool)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.AnnotatedTime()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.AnnotatedYearMonth()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.AnnotatedMonthDay()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateYear()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateMonth()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateDay()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateSpec()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateSpecYearMonth()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateSpecMonthDay()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.Fraction(bool)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.TimeSpec()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.UtcOffset(bool, out bool)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.TimeZoneIanaName()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.TimeZoneIdentifier()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.Annotations()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateTimeOffset(bool)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.Digits(int)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.Bounded(int)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Text.Base64Codec.FromBase64(ReadOnlySpan<char>, Base64Alphabet, LastChunkHandling, Span<byte>, out int, out int, out Failure, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Text/Base64Codec.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Text.Base64Codec.FromHex(ReadOnlySpan<char>, Span<byte>, out int, out int, out Failure, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Text/Base64Codec.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Text.JsonReader` in `Broiler.ECMAScript.Algorithms/Text/JsonReader.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Text.JsonReader.Read()` in `Broiler.ECMAScript.Algorithms/Text/JsonReader.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Text.JsonReader.Validate(ReadOnlySpan<char>, DepthLimit, out Failure, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Text/JsonReader.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Text.JsonReader.ReadValue()` in `Broiler.ECMAScript.Algorithms/Text/JsonReader.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Text.JsonReader.ReadNumber()` in `Broiler.ECMAScript.Algorithms/Text/JsonReader.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Text.JsonReader.ReadString(out string)` in `Broiler.ECMAScript.Algorithms/Text/JsonReader.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Text.StringPadding.Pad(string, long, string, PadPlacement, LengthLimit, out string, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Text/StringPadding.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Text.UriCodec.Decode(ReadOnlySpan<char>, ReadOnlySpan<char>, out string, out Failure, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Text/UriCodec.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.CalendarYears.GetYear(ICalendarTables, TableCalendar, long, out CalendarYear)` in `Broiler.ECMAScript.Algorithms/Time/CalendarYears.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.EcmaDateParser` in `Broiler.ECMAScript.Algorithms/Time/EcmaDateParser.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.EcmaDateParser.Parse(ReadOnlySpan<char>, DateParseDialect, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Time/EcmaDateParser.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.EcmaLocalTime.Utc(double, ILocalTimeProvider)` in `Broiler.ECMAScript.Algorithms/Time/LocalTime.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.OffsetTimeZones.IsTimeZoneOffsetString(ReadOnlySpan<char>)` in `Broiler.ECMAScript.Algorithms/Time/OffsetTimeZones.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.OffsetTimeZones.TryParseOffsetIdentifier(ReadOnlySpan<char>, out int)` in `Broiler.ECMAScript.Algorithms/Time/OffsetTimeZones.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.OffsetTimeZones.Parse(ReadOnlySpan<char>, out int, out bool)` in `Broiler.ECMAScript.Algorithms/Time/OffsetTimeZones.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.TimeZoneDatabase.TryFind(ReadOnlySpan<char>, out TimeZoneId)` in `Broiler.ECMAScript.Algorithms/Time/TimeZoneDatabase.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.TableZoneRules.NextTransition(long, long, bool)` in `Broiler.ECMAScript.Algorithms/Time/TimeZoneDatabase.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.TableZoneRules.PreviousTransition(long, bool)` in `Broiler.ECMAScript.Algorithms/Time/TimeZoneDatabase.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.ZonedTime.GetPossibleEpochNanoseconds(ITimeZoneRules, IsoDateTime, Span<BigInteger>, out int)` in `Broiler.ECMAScript.Algorithms/Time/ZonedTime.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.ZonedTime.DisambiguatePossibleEpochNanoseconds(ITimeZoneRules, ReadOnlySpan<BigInteger>, IsoDateTime, Disambiguation, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Time/ZonedTime.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.ZonedTime.GetStartOfDay(ITimeZoneRules, IsoDate, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Time/ZonedTime.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.ZonedTime.Possible(ITimeZoneRules, IsoDateTime, Span<BigInteger>, out int)` in `Broiler.ECMAScript.Algorithms/Time/ZonedTime.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Time.ZonedTime.Disambiguate(ITimeZoneRules, ReadOnlySpan<BigInteger>, IsoDateTime, Disambiguation, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Time/ZonedTime.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.CldrTableGenerator.RootCollation.Read(string)` in `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.Collation.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.CldrTableGenerator.RootCollation.Elements(string)` in `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.Collation.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.CldrTableGenerator.Tailoring.Apply(RootCollation, CldrUcd, string, IReadOnlyDictionary<string, string>)` in `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.Collation.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.CldrTableGenerator.Generate(IntlCldrPin, IReadOnlyDictionary<string, byte[]>, CldrUcd, IntlGeneratorOptions)` in `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.CldrTableGenerator.Archive.Bytes(string)` in `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.CldrTableGenerator.Text(string, string, IEnumerable<string>)` in `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.CldrUcd.Read(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Intl/CldrUcd.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.CldrUcd.UnicodeDataEntries(string)` in `Broiler.ECMAScript.Generation/Intl/CldrUcd.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.IntlCldrPin` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.IntlCldrPin.Parse(string)` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.IntlCldrPin.Violations(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.IntlCldrPin.Verified(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.IntlCldrPin.VersionViolation(string, byte[])` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.IntlPinText` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.IntlPinText.Parse(string, string)` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.IntlLocaleList.Parse(string)` in `Broiler.ECMAScript.Generation/Intl/IntlLocaleList.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.IntlUcdPin` in `Broiler.ECMAScript.Generation/Intl/IntlUcdPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.IntlUcdPin.Parse(string)` in `Broiler.ECMAScript.Generation/Intl/IntlUcdPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.IntlUcdPin.Violations(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Intl/IntlUcdPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Intl.IntlUcdPin.Verified(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Intl/IntlUcdPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.ArchiveReading` in `Broiler.ECMAScript.Generation/Time/ArchiveReading.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.ArchiveReading.RegularFiles(byte[], IReadOnlySet<string>)` in `Broiler.ECMAScript.Generation/Time/ArchiveReading.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.CalendarPin` in `Broiler.ECMAScript.Generation/Time/CalendarPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.CalendarPin.Parse(string)` in `Broiler.ECMAScript.Generation/Time/CalendarPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.CalendarPin.Extract(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CalendarPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.CalendarPin.ArchiveViolations(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CalendarPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.CalendarPin.Violations(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CalendarPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.CalendarPin.Verified(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CalendarPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.CalendarTableGenerator` in `Broiler.ECMAScript.Generation/Time/CalendarTableGenerator.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.CalendarTableGenerator.Generate(CalendarPin, IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CalendarTableGenerator.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.CldrPin` in `Broiler.ECMAScript.Generation/Time/CldrPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.CldrPin.Parse(string)` in `Broiler.ECMAScript.Generation/Time/CldrPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.CldrPin.Violations(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CldrPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.CldrPin.Verified(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CldrPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.TzdbPin` in `Broiler.ECMAScript.Generation/Time/TzdbPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.TzdbPin.Parse(string)` in `Broiler.ECMAScript.Generation/Time/TzdbPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.TzdbPin.Extract(byte[])` in `Broiler.ECMAScript.Generation/Time/TzdbPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.TzdbPin.TarballViolations(byte[])` in `Broiler.ECMAScript.Generation/Time/TzdbPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.TzdbPin.Violations(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/TzdbPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.TzdbPin.Verified(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/TzdbPin.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.TzdbTableGenerator` in `Broiler.ECMAScript.Generation/Time/TzdbTableGenerator.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Generation.Time.TzdbTableGenerator.Generate(TzdbPin, IReadOnlyDictionary<string, byte[]>, CldrPin, IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/TzdbTableGenerator.cs` - Security=High, human line PENDING

## Falsification criteria

| Metric | Value |
|---|---:|
| Units carrying a criterion | 145 |
| Units required to carry one | 145 |
| Required and missing | 0 |

A `Broiler-Falsified-If:` line states, at the declaration, the observation that would make
the unit wrong. `Security=High` says a unit is risky, which is a set and not a test; the
criterion is the test. It is required where `Security` is `High` or `Critical` and written
nowhere else: `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.JS/Broiler.ECMAScript` names every unit that owes one and carries none, and
every unit below `High` that carries one.

The line is a comment, so it is outside every fingerprint by construction: rewording a
criterion moves no recorded value here, in a file header or in
`assurance.manifest.json`, and invalidates nothing. That is the intended reading - a
criterion is an instruction to whoever reads the unit, not part of what a review is bound to.

## Exemption

Exemption is decided by one predicate in `CSharpAssuranceScanner`, not per unit, so
that the rule is reviewable in one place rather than in several hundred.

| Case | Units |
|---|---:|
| TrivialPropertyOrAccessor | 96 |
| ParameterAssigningConstructor | 13 |
| TrivialExpressionBodiedMember | 31 |
| CompilerSuppliedRecordOrEnumMember | 34 |
| DelegatingOverrideOrOperator | 3 |
| InsideAssemblyMarker | 0 |
| FieldDeclaringStorage | 69 |
| EnumMemberOfADeclaredVocabulary | 203 |
| DeclaredInSource | 0 |

## Per-unit exemptions

| Metric | Value |
|---|---:|
| Per-unit exemptions | 0 |

A per-unit `EXEMPT=<reason>` line exempts one unit by a reason a human wrote, for what the
predicate cannot see. Nothing mechanical checks that the reason is true, that it describes
the unit it sits on, or that it says anything at all, so every use is counted and named
here.

No unit in this component states a per-unit exemption.

## Files not covered

No file under a covered project's directory, and no file a covered project compiles in
through a `<Compile Include>` it states, is left out of the record.

## Change detection

`assurance.manifest.json` lists **every** code unit in the 3 covered assemblies -
1833 of them, exempt and relevant alike - with the fingerprint of its declaration.
This manifest is a change-detection record, not a review. A unit listed there is watched, not reviewed:
the entry records what the declaration's tokens hashed to when the generator last ran, and
nothing else. What the manifest adds is that a unit the exemption predicate treats as
trivial is no longer invisible: a semantic change to one moves a value in a generated file
the check compares byte for byte. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.JS/Broiler.ECMAScript` holds the manifest to the tree.

Beside the units it lists **every covered file** - 106 of them - with a
fingerprint over the complete token stream of its compilation unit. A unit entry exists only
for a declaration kind the scanner enumerates, and an enumeration is a whitelist: an
`[assembly: ...]` attribute is a member of nothing and can be in no unit at all.
Nothing in a covered file can change without something moving here, whatever kind of declaration it is. Comments are outside the stream, because a token's
text is its own characters, so the generated header above and the annotation lines below move
no file fingerprint - which is what lets one generation be a fixed point.

## Verification

The generator and the check are one computation: `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.JS/Broiler.ECMAScript` works out what the
generator would write and compares it with the tree byte for byte, so a record edited by
hand, or left behind by code that moved, is reported rather than trusted.

| Mode | Command | Effect |
|---|---|---|
| Generate | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.JS/Broiler.ECMAScript` | Fills every `Fingerprint=TBF`, refreshes a decision the code has outrun into `STALE; Previous=...`, rewrites the generated headers, `HUMAN_REVIEW.md`, `assurance.manifest.json` and this file. |
| Check | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.JS/Broiler.ECMAScript` | Reports every generated artefact that is not byte-identical to what the generator would produce, every relevant unit with no annotation, every annotation this system cannot read, every fingerprint out of date and every unit at the top of the security vocabulary without a criterion, or below it with one. |
| Release | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.JS/Broiler.ECMAScript --release` | The check, and additionally every relevant unit left in a state that blocks a release. |

The fingerprint is six hex characters - 24 bits - of SHA-256 over the declaration's token
texts, joined by single spaces. Trivia is excluded because a token's text is its own
characters and never the comments or whitespace around it, so `dotnet format` moves no
fingerprint and an annotation is never part of what it describes. The value answers whether a
unit changed since it was reviewed. It is not a collision-free identifier across units and it
is not a cryptographic commitment.
