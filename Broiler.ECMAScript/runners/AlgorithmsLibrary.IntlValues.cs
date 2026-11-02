using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Intl;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The Intl area's sub-area (b), the Intl mathematical value, digit options and FormatNumericToString, then plural rules and
/// plural ranges (corpus revision 27, <c>conformance/vectors/intl/values-and-plurals.vectors</c>; milestone SJL-9.3b): each
/// operation as an engine's adapter calls <c>Broiler.ECMAScript.Intl</c>, over the data home's generated tables and Broiler.VM's
/// providers, as sub-area (a)'s are (<c>AlgorithmsLibrary.Intl.cs</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>The options and the arguments are in the script.</b> The file's vectors give the locale in <c>in</c> and, for some, the
/// value, but the options bag and every other argument only in <c>script</c>, so <see cref="IVectorLibrary.Call"/>, which sees
/// the operation and the inputs alone, cannot tell two of them apart. This runner is therefore an
/// <see cref="IVectorRecordLibrary"/>: a vector of the file is read whole, and its script is read here by a reader of the few
/// forms the file's generator writes (<see cref="ValuesScript"/>), which refuses any other. What the script does around the
/// call - the options read in the constructor's order, each read value checked as GetOption and GetNumberOption check it, the
/// arguments made Intl mathematical values, <c>JSON.stringify</c> and <c>Object.keys</c> of the result - is written here once
/// more, as an engine's adapter does it. Every decision of the sub-area is the library's.
/// </para>
/// <para>
/// <b>Which operations are answered</b> (<see cref="IsLibraryIntlValues"/>): of the file, <c>Intl.PluralRules.prototype.select</c>,
/// <c>selectRange</c> and <c>resolvedOptions</c>, <c>Intl.PluralRules (refusals)</c>, <c>Intl.NumberFormat (refusals)</c> and
/// <c>Intl.NumberFormat.prototype.resolvedOptions</c>, whose vectors read the digit options. Not answered: the vectors of
/// <c>Intl.NumberFormat.prototype.format</c> and <c>formatToParts</c>, whose outcome is PartitionNumberPattern's - the
/// locale's symbols, grouping, affixes, percent scaling and exponents, sub-area (c)'s - though FormatNumericToString decides
/// their digits (rows M1a to M3 and M5 to M8, which the library's unit tests and the differential check hold at stage
/// SJL-9.3b); and the four whose outcome is an engine's reads or coercions: <c>Intl.PluralRules (option reads)</c>,
/// <c>Intl.NumberFormat (option reads)</c>, <c>(option coercions)</c> and <c>(resolvedOptions)</c>, a Proxy's log or an
/// object's <c>valueOf</c> count (row M4's reads).
/// </para>
/// <para>
/// <b>No operation of another area moves.</b> Two of the names are another file's too: <c>Intl.NumberFormat.prototype.resolvedOptions</c>
/// (<c>intl/numbers-and-text.vectors</c>, sub-area (c)'s numbering systems and keys) and <c>Intl.PluralRules.prototype.select</c>
/// (the corpus control <c>controls/bad-provenance.vectors</c>, which no runner loads). A vector is answered here only when its
/// identifier is this file's, so those keep their outcome, which <c>LibraryRunTests</c> holds for every operation this
/// part answers, across every area.
/// </para>
/// </remarks>
internal sealed partial class AlgorithmsLibrary : IVectorRecordLibrary
{
    /// <summary>The identifier prefix of the Intl area's sub-area (b) vectors, the file <c>intl/values-and-plurals.vectors</c>.</summary>
    internal const string IntlValuesPrefix = "intl/values-and-plurals/";

    /// <summary>The operations of the file this part answers.</summary>
    internal static readonly IReadOnlyList<string> IntlValuesOperations =
    [
        "Intl.PluralRules.prototype.select",
        "Intl.PluralRules.prototype.selectRange",
        "Intl.PluralRules.prototype.resolvedOptions",
        "Intl.PluralRules (refusals)",
        "Intl.NumberFormat (refusals)",
        "Intl.NumberFormat.prototype.resolvedOptions",
    ];

    /// <summary>Broiler.VM's BigInt policy is not this runner's concern: no vector gives a prefixed String past a few digits.</summary>
    private static readonly BigIntPolicy IntlValuesPolicy = BigIntPolicy.Unbounded;

    /// <inheritdoc/>
    public string Call(VectorRecord vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        return IsLibraryIntlValues(vector)
            ? IntlValuesArea(vector["op"]!, vector.Script)
            : IsLibraryIntlNumbers(vector)
                ? IntlNumbersArea(vector)
                : IsLibraryIntlDateTimes(vector)
                    ? IntlDateTimesArea(vector)
                    : Call(vector["op"] ?? string.Empty, LibraryRun.Inputs(vector));
    }

    /// <summary>Whether a vector is sub-area (b)'s and one this runner answers: of the file, and of an operation it answers.</summary>
    internal static bool IsLibraryIntlValues(VectorRecord vector) =>
        vector.Id.StartsWith(IntlValuesPrefix, StringComparison.Ordinal) && IsIntlValuesOperation(vector["op"]);

    private static bool IsIntlValuesOperation(string? operation)
    {
        foreach (var name in IntlValuesOperations)
        {
            if (name == operation)
                return true;
        }

        return false;
    }

    private static string IntlValuesArea(string operation, string script)
    {
        var read = ValuesScript.Read(script) ?? throw new NotSupportedException("no sub-area (b) vector's script has this form: " + script);

        if (read.Service == "PluralRules")
        {
            if (NewPluralRules(read.Locale, read.Options, out var rules) is { } refused)
                return refused;

            return read.Use switch
            {
                ValuesUse.Select => Text(Select(rules!, read.Arguments[0])),
                ValuesUse.SelectRange => SelectRange(rules!, read.Arguments[0], read.Arguments[1]),
                ValuesUse.SelectEach => Json(read.Arguments.ConvertAll(argument => Select(rules!, argument))),
                ValuesUse.PluralCategories => Json(rules!.PluralCategories),
                ValuesUse.ResolvedOptions => Text(PluralRulesOptions(rules!, keysOnly: false)),
                ValuesUse.ResolvedOptionKeys => Text(PluralRulesOptions(rules!, keysOnly: true)),
                _ => throw new NotSupportedException(operation + " has no plural-rules use " + read.Use),
            };
        }

        if (NewNumberFormatDigits(read.Locale, read.Options, out var digits) is { } numberRefused)
            return numberRefused;

        return read.Use switch
        {
            ValuesUse.DigitProperties => Text(DigitProperties(digits!, read.Properties)),

            // A refusal vector whose options construct: formatting is sub-area (c)'s, so nothing answers it here.
            _ => EmptyLibrary.NotImplemented,
        };
    }

    // ---- the constructors, their reads in order ------------------------------------------------------------------------

    /// <summary>
    /// <c>new Intl.PluralRules(locale, options)</c> (<c>sec-intl.pluralrules</c>): ResolveOptions over Broiler.VM's providers,
    /// then <c>type</c>, <c>notation</c>, <c>compactDisplay</c> and SetNumberFormatDigitOptions with 0 and 3.
    /// </summary>
    private static string? NewPluralRules(string locale, IReadOnlyDictionary<string, ScriptValue> options, out PluralRulesRecord? rules)
    {
        rules = null;
        if (StringOption(options, "localeMatcher", ["lookup", "best fit"], "best fit", out _) is { } matcher)
            return matcher;

        if (ResolvedFor(locale, []) is not { } resolved)
            return RangeError;

        if (StringOption(options, "type", ["cardinal", "ordinal"], "cardinal", out var type) is { } refusedType)
            return refusedType;
        if (StringOption(options, "notation", ["standard", "scientific", "engineering", "compact"], "standard", out var notationName) is { } refusedNotation)
            return refusedNotation;
        if (StringOption(options, "compactDisplay", ["short", "long"], "short", out var compactName) is { } refusedCompact)
            return refusedCompact;

        var notation = NotationOf(notationName);
        if (DigitOptionsOf(options, new FractionDigitDefaults(0, 3), notation, out var digits) is { } refusedDigits)
            return refusedDigits;

        rules = PluralRulesRecord.Create(IntlData, resolved.Locale, resolved.DataLocale,
            type == "ordinal" ? PluralRuleType.Ordinal : PluralRuleType.Cardinal, notation,
            compactName == "long" ? CompactDisplay.Long : CompactDisplay.Short, digits!);
        return null;
    }

    /// <summary>
    /// <c>new Intl.NumberFormat(locale, options)</c>'s reads up to and through SetNumberFormatDigitOptions (<c>sec-intl.numberformat</c>,
    /// SetNumberFormatUnitOptions), and the reads after it that can refuse: the digit options it resolves.
    /// </summary>
    private static string? NewNumberFormatDigits(string locale, IReadOnlyDictionary<string, ScriptValue> options, out DigitOptions? digits)
    {
        digits = null;
        if (StringOption(options, "localeMatcher", ["lookup", "best fit"], "best fit", out _) is { } matcher)
            return matcher;
        if (StringOption(options, "numberingSystem", null, null, out var numberingSystem) is { } refusedSystem)
            return refusedSystem;
        if (numberingSystem is not null && !LocaleId.IsUnicodeType(numberingSystem))
            return RangeError;

        if (ResolvedFor(locale, [new RelevantKey("nu", NumberingSystemValues()) { Option = numberingSystem }]) is null)
            return RangeError;

        if (StringOption(options, "style", ["decimal", "percent", "currency", "unit"], "decimal", out var style) is { } refusedStyle)
            return refusedStyle;
        if (StringOption(options, "currency", null, null, out var currency) is { } refusedCurrency)
            return refusedCurrency;
        if (currency is null ? style == "currency" : !(currency.Length == 3 && char.IsAsciiLetter(currency[0]) && char.IsAsciiLetter(currency[1]) && char.IsAsciiLetter(currency[2])))
            return currency is null ? "error: TypeError" : RangeError;
        if (StringOption(options, "currencyDisplay", ["code", "symbol", "narrowSymbol", "name"], "symbol", out _) is { } refusedDisplay)
            return refusedDisplay;
        if (StringOption(options, "currencySign", ["standard", "accounting"], "standard", out _) is { } refusedSign)
            return refusedSign;
        if (StringOption(options, "unit", null, null, out var unit) is { } refusedUnit)
            return refusedUnit;
        if (unit is null && style == "unit")
            return "error: TypeError";
        if (unit is not null)
            throw new NotSupportedException("no sub-area (b) vector gives a unit");
        if (StringOption(options, "unitDisplay", ["short", "narrow", "long"], "short", out _) is { } refusedUnitDisplay)
            return refusedUnitDisplay;
        if (StringOption(options, "notation", ["standard", "scientific", "engineering", "compact"], "standard", out var notationName) is { } refusedNotation)
            return refusedNotation;

        var notation = NotationOf(notationName);
        var numberStyle = style switch { "percent" => NumberStyle.Percent, "currency" => NumberStyle.Currency, "unit" => NumberStyle.Unit, _ => NumberStyle.Decimal };
        if (DigitOptionsOf(options, DigitOptionRules.NumberFormatDefaults(numberStyle, currency, notation, IntlData), notation, out digits) is { } refusedDigits)
            return refusedDigits;

        if (StringOption(options, "compactDisplay", ["short", "long"], "short", out _) is { } refusedCompact)
            return refusedCompact;
        if (options.ContainsKey("useGrouping"))
            throw new NotSupportedException("no sub-area (b) vector gives useGrouping");
        return StringOption(options, "signDisplay", ["auto", "never", "always", "exceptZero", "negative"], "auto", out _);
    }

    /// <summary>ResolveLocale over Broiler.VM's providers for one requested tag, as each service's constructor runs it.</summary>
    private static ResolvedLocale? ResolvedFor(string locale, IReadOnlyList<RelevantKey> keys)
    {
        if (LocaleNegotiation.CanonicalizeLocaleList([locale], IntlData, out var requested, out _) != IntlStatus.Completed)
            return null;

        return LocaleNegotiation.ResolveLocale(LocaleNegotiation.MatchLocale(IntlAvailable, requested, IntlDefaultLocale), keys, IntlData);
    }

    /// <summary>
    /// SetNumberFormatDigitOptions (<c>sec-setnumberformatdigitoptions</c>) as an engine runs it: each option read in its order,
    /// the number options checked by GetNumberOption and DefaultNumberOption at the step that converts them, then the library's
    /// decisions.
    /// </summary>
    private static string? DigitOptionsOf(IReadOnlyDictionary<string, ScriptValue> options, FractionDigitDefaults defaults, NumberNotation notation, out DigitOptions? digits)
    {
        digits = null;
        if (NumberOption(options, "minimumIntegerDigits", 1, 21, 1, out var mnid) is { } refusedMnid)
            return refusedMnid;

        options.TryGetValue("minimumFractionDigits", out var mnfd);
        options.TryGetValue("maximumFractionDigits", out var mxfd);
        options.TryGetValue("minimumSignificantDigits", out var mnsd);
        options.TryGetValue("maximumSignificantDigits", out var mxsd);

        if (NumberOption(options, "roundingIncrement", 1, 5000, 1, out var increment) is { } refusedIncrement)
            return refusedIncrement;
        if (!DigitOptionRules.IsAdmittedRoundingIncrement(increment!.Value))
            return RangeError;

        if (StringOption(options, "roundingMode", ["ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven"], "halfExpand", out var modeName) is { } refusedMode)
            return refusedMode;
        if (StringOption(options, "roundingPriority", ["auto", "morePrecision", "lessPrecision"], "auto", out var priorityName) is { } refusedPriority)
            return refusedPriority;
        if (StringOption(options, "trailingZeroDisplay", ["auto", "stripIfInteger"], "auto", out var trailingName) is { } refusedTrailing)
            return refusedTrailing;

        _ = ExactRounding.TryParseRoundingMode(modeName, out var mode);
        var priority = priorityName switch { "morePrecision" => RoundingPriority.MorePrecision, "lessPrecision" => RoundingPriority.LessPrecision, _ => RoundingPriority.Auto };
        var hasSd = mnsd is not null || mxsd is not null;
        var hasFd = mnfd is not null || mxfd is not null;
        var plan = DigitOptionRules.Plan(hasSd, hasFd, priority, notation);

        int? minSd = null, maxSd = null, minFd = null, maxFd = null;
        if (plan.NeedSignificantDigits && hasSd)
        {
            if (Default(mnsd, 1, 21, 1, out minSd) is { } refused)
                return refused;
            if (Default(mxsd, minSd!.Value, 21, 21, out maxSd) is { } refusedMax)
                return refusedMax;
        }

        if (plan.NeedFractionDigits && hasFd)
        {
            if (Default(mnfd, 0, 100, null, out minFd) is { } refused)
                return refused;
            if (Default(mxfd, 0, 100, null, out maxFd) is { } refusedMax)
                return refusedMax;
        }

        var values = new DigitOptionValues(mnid!.Value, hasSd, minSd, maxSd, hasFd, minFd, maxFd, increment.Value, mode, priority,
            trailingName == "stripIfInteger" ? TrailingZeroDisplay.StripIfInteger : TrailingZeroDisplay.Auto);
        return DigitOptionRules.SetNumberFormatDigitOptions(values, defaults, notation, out digits) switch
        {
            IntlStatus.Completed => null,
            IntlStatus.RoundingIncrementNeedsFractionDigits => "error: TypeError",
            _ => RangeError,
        };
    }

    /// <summary>GetOption of a String option (<c>sec-getoption</c>): absent is the fallback; a value outside the list a <c>RangeError</c>.</summary>
    private static string? StringOption(IReadOnlyDictionary<string, ScriptValue> options, string name, string[]? values, string? fallback, out string? value)
    {
        value = options.TryGetValue(name, out var given) ? given.ToStringValue() : fallback;
        return value is not null && values is not null && Array.IndexOf(values, value) < 0 ? RangeError : null;
    }

    /// <summary>GetNumberOption (<c>sec-getnumberoption</c>): absent is the fallback; ToNumber, then DefaultNumberOption's range.</summary>
    private static string? NumberOption(IReadOnlyDictionary<string, ScriptValue> options, string name, int minimum, int maximum, int? fallback, out int? value)
    {
        options.TryGetValue(name, out var given);
        return Default(given, minimum, maximum, fallback, out value);
    }

    /// <summary>DefaultNumberOption (<c>sec-defaultnumberoption</c>) of a value read earlier: ToNumber here, the range the library's.</summary>
    private static string? Default(ScriptValue? given, int minimum, int maximum, int? fallback, out int? value)
    {
        if (given is null)
        {
            value = fallback;
            return null;
        }

        var status = DigitOptionRules.DefaultNumberOption(given.ToNumber(), minimum, maximum, out var converted);
        value = converted;
        return status == IntlStatus.Completed ? null : RangeError;
    }

    private static NumberNotation NotationOf(string? name) => name switch
    {
        "scientific" => NumberNotation.Scientific,
        "engineering" => NumberNotation.Engineering,
        "compact" => NumberNotation.Compact,
        _ => NumberNotation.Standard,
    };

    // ---- the methods ---------------------------------------------------------------------------------------------------

    /// <summary><c>select</c> (<c>sec-intl.pluralrules.prototype.select</c>): ToIntlMathematicalValue, then ResolvePlural.</summary>
    private static string Select(PluralRulesRecord rules, ScriptValue value) => rules.ResolvePlural(value.ToIntlMathematicalValue()).PluralCategory;

    /// <summary><c>selectRange</c> (<c>sec-intl.pluralrules.prototype.selectrange</c>): both ends made values, then ResolvePluralRange.</summary>
    private static string SelectRange(PluralRulesRecord rules, ScriptValue start, ScriptValue end) =>
        rules.ResolvePluralRange(start.ToIntlMathematicalValue(), end.ToIntlMathematicalValue(), out var category) == IntlStatus.Completed
            ? Text(category)
            : RangeError;

    /// <summary>
    /// <c>resolvedOptions()</c> of a plural rules object, as <c>JSON.stringify</c> writes it (<c>sec-intl.pluralrules.prototype.resolvedoptions</c>):
    /// its table's properties in order, each one that is set; or the keys alone, as <c>JSON.stringify(Object.keys(...))</c>.
    /// </summary>
    private static string PluralRulesOptions(PluralRulesRecord rules, bool keysOnly)
    {
        var digits = rules.DigitOptions;
        var properties = new List<(string Name, string? Json)>
        {
            ("locale", JsonString(rules.Locale)),
            ("type", JsonString(rules.Type == PluralRuleType.Ordinal ? "ordinal" : "cardinal")),
            ("notation", JsonString(NotationName(rules.Notation))),
            ("compactDisplay", rules.Notation == NumberNotation.Compact ? JsonString(rules.CompactDisplay == CompactDisplay.Long ? "long" : "short") : null),
            ("minimumIntegerDigits", JsonNumber(digits.MinimumIntegerDigits)),
            ("minimumFractionDigits", JsonNumber(digits.MinimumFractionDigits)),
            ("maximumFractionDigits", JsonNumber(digits.MaximumFractionDigits)),
            ("minimumSignificantDigits", JsonNumber(digits.MinimumSignificantDigits)),
            ("maximumSignificantDigits", JsonNumber(digits.MaximumSignificantDigits)),
            ("pluralCategories", JsonArray(rules.PluralCategories)),
            ("roundingIncrement", JsonNumber(digits.RoundingIncrement)),
            ("roundingMode", JsonString(RoundingModeName(digits.RoundingMode))),
            ("roundingPriority", JsonString(PriorityName(digits.ComputedRoundingPriority))),
            ("trailingZeroDisplay", JsonString(digits.TrailingZeroDisplay == TrailingZeroDisplay.StripIfInteger ? "stripIfInteger" : "auto")),
        };

        var text = new StringBuilder(keysOnly ? "[" : "{");
        foreach (var (name, json) in properties)
        {
            if (json is null)
                continue;
            if (text.Length > 1)
                text.Append(',');
            text.Append(JsonString(name));
            if (!keysOnly)
                text.Append(':').Append(json);
        }

        return text.Append(keysOnly ? ']' : '}').ToString();
    }

    /// <summary>A NumberFormat's digit properties as the script reads them from <c>resolvedOptions()</c>, a missing one <c>null</c>.</summary>
    private static string DigitProperties(DigitOptions digits, IReadOnlyList<string> names)
    {
        var text = new StringBuilder("[");
        foreach (var name in names)
        {
            if (text.Length > 1)
                text.Append(',');
            text.Append(name switch
            {
                "minimumIntegerDigits" => JsonNumber(digits.MinimumIntegerDigits),
                "minimumFractionDigits" => JsonNumber(digits.MinimumFractionDigits),
                "maximumFractionDigits" => JsonNumber(digits.MaximumFractionDigits),
                "minimumSignificantDigits" => JsonNumber(digits.MinimumSignificantDigits),
                "maximumSignificantDigits" => JsonNumber(digits.MaximumSignificantDigits),
                "roundingIncrement" => JsonNumber(digits.RoundingIncrement),
                "roundingMode" => JsonString(RoundingModeName(digits.RoundingMode)),
                "roundingPriority" => JsonString(PriorityName(digits.ComputedRoundingPriority)),
                "trailingZeroDisplay" => JsonString(digits.TrailingZeroDisplay == TrailingZeroDisplay.StripIfInteger ? "stripIfInteger" : "auto"),
                _ => throw new NotSupportedException("no sub-area (b) vector reads " + name),
            } ?? "null");
        }

        return text.Append(']').ToString();
    }

    private static string NotationName(NumberNotation notation) => notation switch
    {
        NumberNotation.Scientific => "scientific",
        NumberNotation.Engineering => "engineering",
        NumberNotation.Compact => "compact",
        _ => "standard",
    };

    private static string PriorityName(RoundingPriority priority) => priority switch
    {
        RoundingPriority.MorePrecision => "morePrecision",
        RoundingPriority.LessPrecision => "lessPrecision",
        _ => "auto",
    };

    private static string RoundingModeName(RoundingMode mode) => mode switch
    {
        RoundingMode.Ceil => "ceil",
        RoundingMode.Floor => "floor",
        RoundingMode.Expand => "expand",
        RoundingMode.Trunc => "trunc",
        RoundingMode.HalfCeil => "halfCeil",
        RoundingMode.HalfFloor => "halfFloor",
        RoundingMode.HalfTrunc => "halfTrunc",
        RoundingMode.HalfEven => "halfEven",
        _ => "halfExpand",
    };

    private static string? JsonNumber(int? value) => value?.ToString(NumberFormatInfo.InvariantInfo);

    private static string JsonString(string value)
    {
        var text = new StringBuilder("\"");
        foreach (var c in value)
        {
            if (c is '"' or '\\' || c < ' ')
                text.Append("\\u").Append(((int)c).ToString("x4", NumberFormatInfo.InvariantInfo));
            else
                text.Append(c);
        }

        return text.Append('"').ToString();
    }

    private static string JsonArray(IReadOnlyList<string> values)
    {
        var text = new StringBuilder("[");
        for (var i = 0; i < values.Count; i++)
            text.Append(i == 0 ? string.Empty : ",").Append(JsonString(values[i]));
        return text.Append(']').ToString();
    }

    // ---- the script reader -----------------------------------------------------------------------------------------------

    /// <summary>What a sub-area (b) script does with the object it constructs.</summary>
    private enum ValuesUse
    {
        Select,
        SelectRange,
        SelectEach,
        PluralCategories,
        ResolvedOptions,
        ResolvedOptionKeys,
        DigitProperties,
        Format,
    }

    /// <summary>A literal of the scripts: a Number or a BigInt, or a String an options bag gives.</summary>
    private sealed class ScriptValue
    {
        private ScriptValue(double number, BigInteger? bigInt, string? text)
        {
            Number = number;
            BigInt = bigInt;
            String = text;
        }

        internal double Number { get; }

        internal BigInteger? BigInt { get; }

        internal string? String { get; }

        internal static ScriptValue Of(double value) => new(value, null, null);

        internal static ScriptValue Of(BigInteger value) => new(0, value, null);

        internal static ScriptValue Of(string value) => new(0, null, value);

        /// <summary>ECMA-262 ToString of the literal, as GetOption calls it.</summary>
        internal string ToStringValue() =>
            String ?? (BigInt is { } big ? big.ToString(NumberFormatInfo.InvariantInfo) : NumberText.ToString(Number));

        /// <summary>ECMA-262 ToNumber of the literal, as GetNumberOption calls it.</summary>
        internal double ToNumber() =>
            String is { } text ? NumberParsing.StringToNumber(text, null)
            : BigInt is not null ? throw new NotSupportedException("no sub-area (b) vector gives a BigInt option")
            : Number;

        /// <summary>ToIntlMathematicalValue (<c>sec-tointlmathematicalvalue</c>) of the literal, its ToPrimitive being itself.</summary>
        internal IntlDecimal ToIntlMathematicalValue() =>
            BigInt is { } big ? IntlDecimal.FromBigInt(big)
            : String is { } text ? IntlDecimal.FromString(text, IntlValuesPolicy)
            : IntlDecimal.FromNumber(Number);
    }

    /// <summary>
    /// A reader of the forms <c>scripts/conformance/generate_intl_vectors.py</c> writes for sub-area (b): one
    /// <c>new Intl.PluralRules(...)</c> or <c>new Intl.NumberFormat(...)</c> with a String locale and an options bag of literals,
    /// and one use of it. Any other form is not one of the file's, and is refused with <see langword="null"/>.
    /// </summary>
    private sealed class ValuesScript
    {
        private const string SelectEachHead = "JSON.stringify([";

        private const string SelectEachTail = "].map(function (n) { return ";

        private const string DigitsHead = "(function (o) { return JSON.stringify([";

        private const string DigitsTail = "]); })(";

        private ValuesScript(string service, string locale, IReadOnlyDictionary<string, ScriptValue> options, ValuesUse use, List<ScriptValue> arguments, IReadOnlyList<string> properties)
        {
            Service = service;
            Locale = locale;
            Options = options;
            Use = use;
            Arguments = arguments;
            Properties = properties;
        }

        internal string Service { get; }

        internal string Locale { get; }

        internal IReadOnlyDictionary<string, ScriptValue> Options { get; }

        internal ValuesUse Use { get; }

        internal List<ScriptValue> Arguments { get; }

        internal IReadOnlyList<string> Properties { get; }

        internal static ValuesScript? Read(string script)
        {
            var start = script.IndexOf("new Intl.", StringComparison.Ordinal);
            if (start < 0)
                return null;

            var reader = new Reader(script, start + "new Intl.".Length);
            var service = reader.Name();
            if (service is not ("PluralRules" or "NumberFormat") || !reader.Take("(") || reader.String() is not { } locale)
                return null;

            var options = new Dictionary<string, ScriptValue>(StringComparer.Ordinal);
            if (reader.Take(", "))
            {
                if (reader.Options(options) is null)
                    return null;
            }

            if (!reader.Take(")"))
                return null;

            var head = script[..start];
            var tail = script[reader.At..];
            var arguments = new List<ScriptValue>();
            var properties = new List<string>();
            ValuesUse use;

            if (head.Length == 0 && tail.StartsWith(".select(", StringComparison.Ordinal) && tail.EndsWith(')'))
            {
                use = ValuesUse.Select;
                if (!ReadArguments(tail[".select(".Length..^1], arguments, 1))
                    return null;
            }
            else if (head.Length == 0 && tail.StartsWith(".selectRange(", StringComparison.Ordinal) && tail.EndsWith(')'))
            {
                use = ValuesUse.SelectRange;
                if (!ReadArguments(tail[".selectRange(".Length..^1], arguments, 2))
                    return null;
            }
            else if (head.Length == 0 && tail == ".format(1)")
            {
                use = ValuesUse.Format;
            }
            else if (head == "JSON.stringify(" && tail == ".resolvedOptions().pluralCategories)")
            {
                use = ValuesUse.PluralCategories;
            }
            else if (head == "JSON.stringify(" && tail == ".resolvedOptions())")
            {
                use = ValuesUse.ResolvedOptions;
            }
            else if (head == "JSON.stringify(Object.keys(" && tail == ".resolvedOptions()))")
            {
                use = ValuesUse.ResolvedOptionKeys;
            }
            else if (head.StartsWith(SelectEachHead, StringComparison.Ordinal) && head.EndsWith(SelectEachTail, StringComparison.Ordinal) && tail == ".select(n); }))")
            {
                use = ValuesUse.SelectEach;
                if (!ReadArguments(head[SelectEachHead.Length..^SelectEachTail.Length], arguments, -1))
                    return null;
            }
            else if (head.StartsWith(DigitsHead, StringComparison.Ordinal) && head.EndsWith(DigitsTail, StringComparison.Ordinal) && tail == ".resolvedOptions())")
            {
                use = ValuesUse.DigitProperties;
                foreach (var item in head[DigitsHead.Length..^DigitsTail.Length].Split(", "))
                {
                    if (!item.StartsWith("o.", StringComparison.Ordinal))
                        return null;
                    properties.Add(item[2..]);
                }
            }
            else
            {
                return null;
            }

            return new ValuesScript(service, locale, options, use, arguments, properties);
        }

        /// <summary>A comma-separated list of argument literals, of <paramref name="count"/> items or any number for -1.</summary>
        private static bool ReadArguments(string text, List<ScriptValue> arguments, int count)
        {
            foreach (var item in TopLevelItems(text))
            {
                if (Argument(item.Trim()) is not { } value)
                    return false;
                arguments.Add(value);
            }

            return count < 0 || arguments.Count == count;
        }

        /// <summary>The items of a comma-separated list, a comma inside parentheses not separating two.</summary>
        private static List<string> TopLevelItems(string text)
        {
            var items = new List<string>();
            var depth = 0;
            var begin = 0;
            for (var i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '(':
                        depth++;
                        break;
                    case ')':
                        depth--;
                        break;
                    case ',' when depth == 0:
                        items.Add(text[begin..i]);
                        begin = i + 1;
                        break;
                }
            }

            items.Add(text[begin..]);
            return items;
        }

        /// <summary>
        /// One argument literal: a decimal Number with an optional minus, <c>NaN</c>, <c>Infinity</c> and <c>-Infinity</c>, the
        /// prelude's <c>$f64(hi, lo)</c>, a BigInt <c>1n</c>, <c>-1n</c> or <c>10n ** 30n</c>, a String, or <c>Object(...)</c> of
        /// one, whose ToPrimitive is the one.
        /// </summary>
        private static ScriptValue? Argument(string text)
        {
            if (text.StartsWith("Object(", StringComparison.Ordinal) && text.EndsWith(')'))
                return Argument(text["Object(".Length..^1]);

            if (text.StartsWith("$f64(", StringComparison.Ordinal) && text.EndsWith(')'))
            {
                var halves = text["$f64(".Length..^1].Split(", ");
                if (halves.Length == 2 && Hex(halves[0]) is { } high && Hex(halves[1]) is { } low)
                    return ScriptValue.Of(BitConverter.UInt64BitsToDouble(((ulong)high << 32) | low));
                return null;
            }

            switch (text)
            {
                case "NaN":
                    return ScriptValue.Of(double.NaN);
                case "Infinity":
                    return ScriptValue.Of(double.PositiveInfinity);
                case "-Infinity":
                    return ScriptValue.Of(double.NegativeInfinity);
            }

            var power = text.IndexOf(" ** ", StringComparison.Ordinal);
            if (power > 0)
            {
                return BigIntLiteral(text[..power]) is { } baseValue && BigIntLiteral(text[(power + 4)..]) is { } exponent && exponent <= 100_000
                    ? ScriptValue.Of(BigInteger.Pow(baseValue, (int)exponent))
                    : null;
            }

            if (BigIntLiteral(text) is { } bigInt)
                return ScriptValue.Of(bigInt);

            if (text.Length > 1 && text[0] == '"' && text[^1] == '"' && text.IndexOf('\\', StringComparison.Ordinal) < 0)
                return ScriptValue.Of(text[1..^1]);

            var digits = text.StartsWith('-') ? text[1..] : text;
            if (digits.Length == 0)
                return null;
            foreach (var c in digits)
            {
                if (!char.IsAsciiDigit(c) && c != '.')
                    return null;
            }

            return ScriptValue.Of(double.Parse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo));
        }

        private static BigInteger? BigIntLiteral(string text)
        {
            if (text.Length < 2 || text[^1] != 'n')
                return null;

            var digits = text.StartsWith('-') ? text[1..^1] : text[..^1];
            if (digits.Length == 0)
                return null;
            foreach (var c in digits)
            {
                if (!char.IsAsciiDigit(c))
                    return null;
            }

            var value = BigInteger.Parse(digits, NumberStyles.None, NumberFormatInfo.InvariantInfo);
            return text.StartsWith('-') ? -value : value;
        }

        private static uint? Hex(string text) =>
            text.StartsWith("0x", StringComparison.Ordinal) && uint.TryParse(text[2..], NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out var value)
                ? value
                : null;

        /// <summary>A cursor over a script.</summary>
        private sealed class Reader(string text, int at)
        {
            internal int At { get; private set; } = at;

            internal bool Take(string literal)
            {
                if (string.CompareOrdinal(text, At, literal, 0, literal.Length) != 0)
                    return false;
                At += literal.Length;
                return true;
            }

            internal string Name()
            {
                var begin = At;
                while (At < text.Length && char.IsAsciiLetterOrDigit(text[At]))
                    At++;
                return text[begin..At];
            }

            /// <summary>A double-quoted String literal without escapes, as the generator writes a locale and an option's value.</summary>
            internal string? String()
            {
                if (!Take("\""))
                    return null;
                var end = text.IndexOf('"', At);
                if (end < 0)
                    return null;
                var value = text[At..end];
                if (value.Contains('\\', StringComparison.Ordinal))
                    return null;
                At = end + 1;
                return value;
            }

            /// <summary>An options bag of literals, <c>{}</c> or <c>{name: literal, ...}</c>; anything else is not the file's form.</summary>
            internal Dictionary<string, ScriptValue>? Options(Dictionary<string, ScriptValue> options)
            {
                if (!Take("{"))
                    return null;
                if (Take("}"))
                    return options;

                while (true)
                {
                    var name = Name();
                    if (name.Length == 0 || !Take(": "))
                        return null;

                    ScriptValue? value;
                    if (text[At] == '"')
                    {
                        value = String() is { } chars ? ScriptValue.Of(chars) : null;
                    }
                    else
                    {
                        var end = At;
                        while (end < text.Length && text[end] != ',' && text[end] != '}')
                            end++;
                        value = Argument(text[At..end]);
                        At = end;
                    }

                    if (value is null || !options.TryAdd(name, value))
                        return null;
                    if (Take("}"))
                        return options;
                    if (!Take(", "))
                        return null;
                }
            }
        }
    }
}
