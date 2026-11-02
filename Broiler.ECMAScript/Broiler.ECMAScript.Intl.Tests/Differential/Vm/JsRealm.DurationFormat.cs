// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsRealm.DurationFormat.cs
//     blob 8a6dfae2acddacef9b3b676316026f0a80abc301: JsRealm's Intl.DurationFormat part and JsDurationFormatObject, of which the constructor's reads, GetDurationUnitOptions, DurationSign, the fractions, the number formats, FormatNumericUnits and PartitionDurationFormatPattern are kept
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: the members that build the prototype and its methods (`DurationFormatConstructor`, `SetupDurationFormat`, `DurationFormatOfThis`) and the reads of the argument (`DurationFieldsRead`, `DurationFormatInput`, `ToDurationRecord`, `IsValidDuration`), which read script objects and Temporal's objects through the Temporal realm (SJL-10's) and which the library leaves to the engine (JSD-0082, decision D24, row U22).

using System.Collections.Generic;
using System.Numerics;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// <c>Intl.DurationFormat</c> (ECMA-402 s13; JSD-0051): a duration's units written by unit number
/// formats, or as a digital clock, and joined by a unit list format.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every number is the profile's own <c>Intl.NumberFormat</c>, and the list its own
/// <c>Intl.ListFormat</c></b>, each built by its constructor with the options the draft names, so a
/// duration writes each unit exactly as those formats write it.
/// </para>
/// <para>
/// <b>A fractional value is exact.</b> The draft adds milliseconds, microseconds and nanoseconds to a
/// larger unit as a mathematical value, and notes that floating point cannot. The sum is made here as
/// a BigInteger of the smallest unit and written as a decimal string, which the number format reads
/// exactly.
/// </para>
/// </remarks>
internal sealed partial class JsRealm
{
    /// <summary><c>%Intl.DurationFormat.prototype%</c>, or nothing where the surface is not built.</summary>
    internal JsObject? DurationFormatPrototype { get; private set; }

    /// <summary>Table 20 and table 24: each unit, its singular, its styles and its digital default.</summary>
    private static readonly (string Unit, string Singular, string[] Styles, string Digital)[] DurationUnits =
    [
        ("years", "year", ["long", "short", "narrow"], "short"),
        ("months", "month", ["long", "short", "narrow"], "short"),
        ("weeks", "week", ["long", "short", "narrow"], "short"),
        ("days", "day", ["long", "short", "narrow"], "short"),
        ("hours", "hour", ["long", "short", "narrow", "numeric", "2-digit"], "numeric"),
        ("minutes", "minute", ["long", "short", "narrow", "numeric", "2-digit"], "numeric"),
        ("seconds", "second", ["long", "short", "narrow", "numeric", "2-digit"], "numeric"),
        ("milliseconds", "millisecond", ["long", "short", "narrow", "numeric"], "numeric"),
        ("microseconds", "microsecond", ["long", "short", "narrow", "numeric"], "numeric"),
        ("nanoseconds", "nanosecond", ["long", "short", "narrow", "numeric"], "numeric"),
    ];

    /// <summary>ECMA-402's <c>Intl.DurationFormat</c> constructor (s13.1.1): the options read in its order.</summary>
    internal static JsValue NewDurationFormat(JsEngine engine, JsValue newTarget, JsValue[] arguments)
    {
        var prototype = engine.PrototypeFromConstructor(newTarget, engine.Realm.DurationFormatPrototype!);

        // RESOLVEOPTIONS: the locales, the options through GetOptionsObject, the matcher, and nu.
        var requested = CanonicalizeLocaleList(engine, Argument(arguments, 0));
        var options = JsValue.Object(Base64Options(engine, Argument(arguments, 1)));
        _ = GetStringOption(engine, options, "localeMatcher", ["lookup", "best fit"], "best fit");
        var numberingSystem = GetStringOption(engine, options, "numberingSystem", null, null);

        if (numberingSystem is not null && !IsTypeSequence(numberingSystem))
        {
            return engine.ThrowRangeError("Intl.DurationFormat: the numberingSystem option is not a well-formed type: " + numberingSystem);
        }

        var numbers = engine.Intl!.Numbers;
        var systems = new List<string?> { "latn" };

        foreach (var name in numbers.NumberingSystems.Keys)
        {
            if (name != "latn")
            {
                systems.Add(name);
            }
        }

        systems.Sort(1, systems.Count - 1, System.StringComparer.Ordinal);
        var nuValues = systems.ToArray();

        var resolved = ResolveLocale(engine, requested, new Dictionary<string, string?>(System.StringComparer.Ordinal) { ["nu"] = numberingSystem }, ["nu"], (locale, key) => key == "nu" ? nuValues : [null]);

        // [[DIGITALFORMAT]]: CLDR's time separator, and whether its hours-minutes-seconds pattern
        // writes the hours with two digits.
        var language = JsLocaleTag.Parse(resolved.DataLocale)?.Language ?? "en";
        var separator = numbers.Value(language, "symbols.timeSeparator") ?? ":";
        var twoDigitHours = (numbers.Value(language, "duration.hms") ?? "h:mm:ss").StartsWith("hh", System.StringComparison.Ordinal);

        var style = GetStringOption(engine, options, "style", ["long", "short", "narrow", "digital"], "short")!;
        var units = new (string Style, string Display)[DurationUnits.Length];
        var previous = string.Empty;

        for (var i = 0; i < DurationUnits.Length; i++)
        {
            var (unit, _, styles, digital) = DurationUnits[i];
            units[i] = GetDurationUnitOptions(engine, unit, options, style, styles, digital, previous, twoDigitHours);

            if (unit is "hours" or "minutes" or "seconds" or "milliseconds" or "microseconds")
            {
                previous = units[i].Style;
            }
        }

        var fractionalDigits = GetNumberOption(engine, options, "fractionalDigits", 0, 9, null);

        return JsValue.Object(new JsDurationFormatObject(prototype)
        {
            Locale = resolved.Locale,
            NumberingSystem = resolved.Keys["nu"] ?? "latn",
            Style = style,
            Units = units,
            FractionalDigits = fractionalDigits,
            HourMinuteSeparator = separator,
            MinuteSecondSeparator = separator,
        });
    }

    private static bool IsFractionalSecondUnitName(string unit) => unit is "milliseconds" or "microseconds" or "nanoseconds";

    /// <summary>ECMA-402's GetDurationUnitOptions (s13.5.6) with ValidateDurationUnitStyle (s13.5.6.2).</summary>
    private static (string Style, string Display) GetDurationUnitOptions(JsEngine engine, string unit, JsValue options, string baseStyle, string[] styles, string digitalBase, string previous, bool twoDigitHours)
    {
        var style = GetStringOption(engine, options, unit, styles, null);
        var displayDefault = "always";

        if (style is null)
        {
            if (baseStyle == "digital")
            {
                style = digitalBase;

                if (unit is not ("hours" or "minutes" or "seconds"))
                {
                    displayDefault = "auto";
                }
            }
            else if (previous is "fractional" or "numeric" or "2-digit")
            {
                style = "numeric";

                if (unit is not ("minutes" or "seconds"))
                {
                    displayDefault = "auto";
                }
            }
            else
            {
                style = baseStyle;
                displayDefault = "auto";
            }
        }

        if (style == "numeric" && IsFractionalSecondUnitName(unit))
        {
            style = "fractional";
            displayDefault = "auto";
        }

        var display = GetStringOption(engine, options, unit + "Display", ["auto", "always"], displayDefault)!;

        if (display == "always" && style == "fractional")
        {
            throw engine.Error("RangeError", "Intl.DurationFormat: " + unit + " cannot always be displayed as a fraction");
        }

        if (previous == "fractional" && style != "fractional")
        {
            throw engine.Error("RangeError", "Intl.DurationFormat: " + unit + " must be fractional after a fractional unit");
        }

        if (previous is "numeric" or "2-digit" && style is not ("fractional" or "numeric" or "2-digit"))
        {
            throw engine.Error("RangeError", "Intl.DurationFormat: " + unit + " must be numeric after a numeric unit");
        }

        if (unit == "hours" && twoDigitHours)
        {
            style = "2-digit";
        }

        if (unit is "minutes" or "seconds" && previous is "numeric" or "2-digit")
        {
            style = "2-digit";
        }

        return (style, display);
    }

    /// <summary>ECMA-402's DurationSign (s13.5.4).</summary>
    private static int DurationSign(double[] record)
    {
        foreach (var value in record)
        {
            if (value < 0)
            {
                return -1;
            }

            if (value > 0)
            {
                return 1;
            }
        }

        return 0;
    }

    /// <summary>
    /// The value of unit <paramref name="index"/> with ComputeFractionalDigits (s13.5.7) added: the
    /// fractional units that follow it, each a thousandth of the one before, as an exact decimal.
    /// </summary>
    private static JsDecimal WithFractions(JsEngine engine, JsDurationFormatObject format, double[] record, int index)
    {
        var total = new BigInteger(record[index]);
        var digits = 0;

        for (var i = index + 1; i < DurationUnits.Length && format.Units[i].Style == "fractional"; i++)
        {
            total = (total * 1_000) + new BigInteger(record[i]);
            digits += 3;
        }

        if (digits == 0)
        {
            return JsDecimal.FromNumber(record[index]);
        }

        var negative = total.Sign < 0;
        var text = BigInteger.Abs(total).ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(digits + 1, '0');
        var decimalText = (negative ? "-" : string.Empty) + text[..^digits] + "." + text[^digits..];
        return JsDecimal.FromString(decimalText, engine.WorkCharge);
    }

    /// <summary>A number format of the duration format's locale, built by the constructor from <paramref name="settings"/>.</summary>
    private static JsNumberFormatter DurationNumberFormat(JsEngine engine, JsDurationFormatObject format, List<(string Key, JsValue Value)> settings)
    {
        var options = new JsObject(null);
        options.DefineOrdinary("numberingSystem", JsValue.String(format.NumberingSystem));

        foreach (var (key, value) in settings)
        {
            options.DefineOrdinary(key, value);
        }

        return ((JsNumberFormatObject)NewNumberFormat(engine, JsValue.Object(engine.Realm.NumberFormatConstructor!),
            [JsValue.String(format.Locale), JsValue.Object(options)], JsValue.Undefined, called: false).AsObject()).Formatter;
    }

    /// <summary>The digits settings of a value with fractions: up to nine digits, or exactly fractionalDigits, truncated.</summary>
    private static void FractionSettings(JsDurationFormatObject format, List<(string Key, JsValue Value)> settings)
    {
        var digits = format.FractionalDigits;
        settings.Add(("maximumFractionDigits", JsValue.Number(digits ?? 9)));
        settings.Add(("minimumFractionDigits", JsValue.Number(digits ?? 0)));
        settings.Add(("roundingMode", JsValue.String("trunc")));
    }

    /// <summary>
    /// ECMA-402's FormatNumericHours, FormatNumericMinutes and FormatNumericSeconds (s13.5.9 to
    /// s13.5.11): one clock field, after its separator where a larger field precedes it.
    /// </summary>
    private static void FormatNumericField(JsEngine engine, JsDurationFormatObject format, int index, JsDecimal value,
        string? separator, bool signDisplayed, List<(string Type, string Value, string? Unit)> result)
    {
        if (separator is not null)
        {
            result.Add(("literal", separator, null));
        }

        var settings = new List<(string Key, JsValue Value)>();

        if (format.Units[index].Style == "2-digit")
        {
            settings.Add(("minimumIntegerDigits", JsValue.Number(2)));
        }

        if (!signDisplayed)
        {
            settings.Add(("signDisplay", JsValue.String("never")));
        }

        settings.Add(("useGrouping", JsValue.Boolean(false)));

        if (index == 6)
        {
            FractionSettings(format, settings);
        }

        foreach (var part in DurationNumberFormat(engine, format, settings).Parts(engine, value))
        {
            result.Add((part.Type, part.Value, DurationUnits[index].Singular));
        }
    }

    /// <summary>ECMA-402's FormatNumericUnits (s13.5.12): the clock, from its first numeric field.</summary>
    private static List<(string Type, string Value, string? Unit)> FormatNumericUnits(
        JsEngine engine, JsDurationFormatObject format, double[] record, int first, bool signDisplayed)
    {
        var result = new List<(string Type, string Value, string? Unit)>();
        var hours = record[4];
        var minutes = record[5];
        var seconds = record[7] != 0 || record[8] != 0 || record[9] != 0
            ? WithFractions(engine, format, record, 6)
            : JsDecimal.FromNumber(record[6]);

        var hoursFormatted = first == 4 && (hours != 0 || format.Units[4].Display == "always");
        var secondsFormatted = !seconds.IsZero || format.Units[6].Display == "always";
        var minutesFormatted = first is 4 or 5 &&
            ((hoursFormatted && secondsFormatted) || minutes != 0 || format.Units[5].Display == "always");

        if (hoursFormatted)
        {
            var value = signDisplayed && hours == 0 && DurationSign(record) == -1 ? -0.0 : hours;
            FormatNumericField(engine, format, 4, JsDecimal.FromNumber(value), null, signDisplayed, result);
            signDisplayed = false;
        }

        if (minutesFormatted)
        {
            var value = signDisplayed && minutes == 0 && DurationSign(record) == -1 ? -0.0 : minutes;
            FormatNumericField(engine, format, 5, JsDecimal.FromNumber(value), hoursFormatted ? format.HourMinuteSeparator : null, signDisplayed, result);
            signDisplayed = false;
        }

        if (secondsFormatted)
        {
            FormatNumericField(engine, format, 6, seconds, minutesFormatted ? format.MinuteSecondSeparator : null, signDisplayed, result);
        }

        return result;
    }

    /// <summary>
    /// ECMA-402's PartitionDurationFormatPattern (s13.5.15): each unit before the clock by a unit
    /// number format, the clock, and the whole joined by ListFormatParts (s13.5.14).
    /// </summary>
    private static List<(string Type, string Value, string? Unit)> PartitionDurationFormatPattern(
        JsEngine engine, JsDurationFormatObject format, double[] record)
    {
        var lists = new List<List<(string Type, string Value, string? Unit)>>();
        var signDisplayed = true;

        for (var i = 0; i < DurationUnits.Length; i++)
        {
            engine.Charge(1);
            var (style, display) = format.Units[i];

            if (style is "numeric" or "2-digit")
            {
                var numeric = FormatNumericUnits(engine, format, record, i, signDisplayed);

                if (numeric.Count != 0)
                {
                    lists.Add(numeric);
                }

                break;
            }

            var settings = new List<(string Key, JsValue Value)>();
            var nextFractional = i + 1 < DurationUnits.Length && i >= 6 && format.Units[i + 1].Style == "fractional";
            var value = nextFractional ? WithFractions(engine, format, record, i) : JsDecimal.FromNumber(record[i]);

            if (nextFractional)
            {
                FractionSettings(format, settings);
            }

            if (display == "always" || !value.IsZero)
            {
                if (signDisplayed)
                {
                    signDisplayed = false;

                    if (value.IsZero && DurationSign(record) == -1)
                    {
                        value = JsDecimal.FromNumber(-0.0);
                    }
                }
                else
                {
                    settings.Add(("signDisplay", JsValue.String("never")));
                }

                settings.Add(("style", JsValue.String("unit")));
                settings.Add(("unit", JsValue.String(DurationUnits[i].Singular)));
                settings.Add(("unitDisplay", JsValue.String(style)));

                var list = new List<(string Type, string Value, string? Unit)>();

                foreach (var part in DurationNumberFormat(engine, format, settings).Parts(engine, value))
                {
                    list.Add((part.Type, part.Value, DurationUnits[i].Singular));
                }

                lists.Add(list);
            }

            if (nextFractional)
            {
                break;
            }
        }

        // LISTFORMATPARTS: a unit list format of the style (short for digital), and each element's parts in its place.
        var listOptions = new JsObject(null);
        listOptions.DefineOrdinary("type", JsValue.String("unit"));
        listOptions.DefineOrdinary("style", JsValue.String(format.Style == "digital" ? "short" : format.Style));
        var listFormat = (JsListFormatObject)NewListFormat(
            engine,
            JsValue.Object(engine.Realm.ListFormatConstructor!),
            [JsValue.String(format.Locale), JsValue.Object(listOptions)]).AsObject();

        var strings = new List<string>();

        foreach (var parts in lists)
        {
            var text = new System.Text.StringBuilder();

            foreach (var (Type, Value, Unit) in parts)
            {
                text.Append(Value);
            }

            strings.Add(text.ToString());
        }

        var result = new List<(string Type, string Value, string? Unit)>();
        var element = 0;

        foreach (var (type, value) in CreatePartsFromList(engine, listFormat, strings))
        {
            if (type == "element")
            {
                result.AddRange(lists[element]);
                element++;
            }
            else
            {
                result.Add(("literal", value, null));
            }
        }

        return result;
    }
}

/// <summary>An <c>Intl.DurationFormat</c>: its resolved options, each unit's style and display.</summary>
internal sealed class JsDurationFormatObject : JsObject
{
    internal JsDurationFormatObject(JsObject prototype)
        : base(prototype)
    {
    }

    /// <summary>The resolved locale.</summary>
    internal string Locale { get; init; } = string.Empty;

    /// <summary>The resolved numbering system.</summary>
    internal string NumberingSystem { get; init; } = "latn";

    /// <summary><c>long</c>, <c>short</c>, <c>narrow</c> or <c>digital</c>.</summary>
    internal string Style { get; init; } = "short";

    /// <summary>Each unit's style and display, in table 24's order.</summary>
    internal (string Style, string Display)[] Units { get; init; } = [];

    /// <summary>[[FractionalDigits]], or nothing.</summary>
    internal int? FractionalDigits { get; init; }

    /// <summary>[[HourMinuteSeparator]].</summary>
    internal string HourMinuteSeparator { get; init; } = ":";

    /// <summary>[[MinuteSecondSeparator]].</summary>
    internal string MinuteSecondSeparator { get; init; } = ":";
}
