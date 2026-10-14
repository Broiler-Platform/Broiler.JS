using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Text;
using J = Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Text.J;
using V = Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Text.V;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Text;

/// <summary>
/// One arm's implementation of one operation, with the shared implementation it is compared with: S
/// called as that arm's engine will call it (under its policy, with or without its meter, projected onto
/// what the arm's member answers). <see cref="Applies"/> says which inputs the arm's engine can hand the
/// member at all. An arm named "S" is S under one more policy, compared with S under the arm's.
/// </summary>
internal sealed record TextVariant(
    string Arm, string Name, Func<TextInput, string> Run, Func<TextInput, string> Shared,
    Func<TextInput, bool>? Applies = null);

/// <summary>An operation of the rule: the classes it runs over, how a line becomes its inputs, and its variants.</summary>
internal sealed record TextOperation(
    string Name, string[] Classes, Func<TextInput, IEnumerable<TextInput>> Expand, TextVariant[] Variants,
    Func<TextInput, string>? SharedOnly = null);

/// <summary>
/// The operations of bundle sjl-5-001's reference-arm rule, as its revision of 2026-10-14 reads them, and
/// for each the shared implementation (arm S) and the arms' variants.
/// </summary>
/// <remarks>
/// <para>
/// <b>An arm is called as its engine calls the member.</b> Where an engine's call site does more than
/// call the member - Broiler.VM's <c>padStart</c> answers before it converts the filler, its
/// <c>codePointAt</c> charges one unit before it reads - S's variant repeats those lines, and says so,
/// because that is how the engine will call S after its cut-over.
/// </para>
/// <para>
/// <b>S under each engine's policy.</b> Every variant of arm V is compared with S under Broiler.VM's
/// policies (its string ceiling, 2^24 code units; its JSON depth, 512 for <c>JSON.parse</c> and 1024 for
/// the module translation) and its meter, the total charge after a bar. Every variant of arm J is compared
/// with S under no limit the population reaches and no meter: Broiler.JS's depth is System.Text.Json's,
/// which no policy of S can reproduce (correspondence row J1), and its string ceiling is
/// <c>int.MaxValue</c>.
/// </para>
/// </remarks>
internal static class TextArms
{
    public const long VmStringCeiling = 1L << 24;

    public const int VmJsonDepth = 512;

    public const int VmModuleDepth = 1024;

    /// <summary>The longest pad arm J is asked to build: the revision's bound, above which its builder allocates past .NET's largest string.</summary>
    public const long JPadBound = (1L << 24) + 1;

    public static readonly string[] Strings = ["T1U", "T1E", "T1W", "T1R"];

    public static IReadOnlyList<(string Arm, string Operation, string Reason)> NotImplemented { get; } =
    [
        ("J,V", "TrimString", "neither arm's list copies it: both engines' trim already reads the library's StrWhiteSpaceChar set, Broiler.JS's through TrimStrWhiteSpace itself; S runs over the population alone"),
        ("J,V", "AdvanceStringIndex at index -1", "no engine calls it below zero: its index is lastIndex after ToLength"),
        ("J,V", "CodePointAt beyond the String", "both engines answer undefined before reading, as S's adapter does; the arms run it and S's variant answers it"),
        ("J", "StringPad to more than 2^24 + 1 code units", "the revision of 2026-10-14: arm J's builder allocates up to int.MaxValue code units, past .NET's largest string; P4 is shown by the vectors"),
        ("J,V", "the relative index for lengths above int.MaxValue", "Relative, ClampRelativeIndex and StringRelativeIndex take an int length; ArrayRelative takes every length"),
        ("V", "JSON validation at 64", "Broiler.VM's limits are 512 and 1024; S runs at 64 alone"),
        ("J", "JSON validation at any limit", "Broiler.JS's depth is System.Text.Json's; S is compared with arm J under no limit"),
        ("V", "StringExpandDollars and RegExpReplaceText with captures or named groups", "a String pattern has neither"),
        ("J", "JSString.GetSubstitution with captures or named groups", "a String pattern has neither"),
    ];

    /// <summary>The operations, built on first use, after every static field they read is initialised.</summary>
    public static IReadOnlyList<TextOperation> Operations => operations ??= Build();

    private static IReadOnlyList<TextOperation>? operations;

    /// <summary>
    /// The rule's negative controls: a copy of arm V's GlobalUriEncode that charges one unit more for the
    /// input "A" of U1C, and a copy of arm J's DecodeBase64 whose bytes for the input "AAAA" of B1E have
    /// their first byte changed. Each is run once, alone, and the comparison must report exactly the one
    /// input it changes; neither copy is in <see cref="Operations"/>.
    /// </summary>
    public static IReadOnlyList<TextOperation> NegativeControls => BuildControls();

    private static List<TextOperation> BuildControls() =>
    [
        new("Encode encodeURIComponent (negative control)", ["U1C"], Once,
        [
            new("V", "GlobalUriEncode, one charge changed", static i => VCharged(e =>
            {
                if (i.Text == "A")
                    e.Charge(1);
                return TextRender.Str(V.JsRealmText.GlobalUriEncode(e, i.Text, V.JsRealmText.GlobalUriUnescapedComponent));
            }), static i => Charged(m => SEncode(i, string.Empty, m.Charge))),
        ]),
        new("FromBase64 (negative control)", ["B1E"], Decodes,
        [
            new("J", "DecodeBase64, one result changed", static i =>
            {
                var result = J.JSUInt8ArrayText.DecodeBase64(i.Text, i.Parameter == 0 ? "base64" : "base64url", Modes[i.Parameter2], i.Int < 0 ? int.MaxValue : (int)i.Int);
                var bytes = result.Bytes;
                if (i.Text == "AAAA" && i.Parameter == 0 && i.Parameter2 == 0 && i.Int < 0)
                    bytes = [0xFF, .. bytes[1..]];
                return TextRender.Decoded(result.Read, bytes, result.Error?.Kind);
            }, static i => SBase64(i, null)),
        ]),
    ];

    // ---------------------------------------------------------------------------------------------
    // Helpers.

    private static IEnumerable<TextInput> Once(TextInput input) => [input];

    private static string Safe(Func<string> run)
    {
        try
        {
            return run();
        }
        catch (ArmError error)
        {
            return TextRender.Error(error.Kind);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return "exception:" + exception.GetType().Name;
        }
    }

    private static string Charged(Func<Meter, string> run)
    {
        var meter = new Meter();
        var result = Safe(() => run(meter));
        return Render.Charged(result, meter.Total);
    }

    private static string VCharged(Func<V.JsEngine, string> run)
    {
        var engine = new V.JsEngine();
        var result = Safe(() => run(engine));
        return Render.Charged(result, engine.Charged);
    }

    private static string Num(double value) => double.IsNaN(value) ? Render.Undefined : Render.Number(value);

    // ---------------------------------------------------------------------------------------------

    private static List<TextOperation> Build()
    {
        var operations = new List<TextOperation>();
        operations.AddRange(Utf16Operations());
        operations.AddRange(PaddingOperations());
        operations.Add(SubstitutionOperation());
        operations.AddRange(UriOperations());
        operations.AddRange(Base64Operations());
        operations.AddRange(JsonOperations());
        return operations;
    }

    // ---- Whitespace and UTF-16 ------------------------------------------------------------------

    private static IEnumerable<TextOperation> Utf16Operations()
    {
        yield return new("TrimString", Strings, input => new[] { "start", "end", "start+end" }.Select((where, at) => input with { Parameter = at, Detail = where }), [],
            SharedOnly: static i => TextRender.Str(new string(NumberParsing.TrimStrWhiteSpace(i.Text, i.Parameter switch { 0 => TrimEnds.Start, 1 => TrimEnds.End, _ => TrimEnds.Both }))));

        yield return new("IsStringWellFormedUnicode", Strings, Once,
        [
            new("J", "IsWellFormedUtf16", static i => TextRender.Bool(J.JSStringText.IsWellFormedUtf16(i.Text)), static i => TextRender.Bool(Utf16.IsWellFormed(i.Text))),
            new("V", "isWellFormed", static i => VCharged(e => TextRender.Bool(V.JsRealmText.IsWellFormed(e, i.Text))),
                static i => Charged(m => TextRender.Bool(Utf16.IsWellFormed(i.Text, m.Charge)))),
        ]);

        yield return new("toWellFormed", Strings, Once,
        [
            new("J", "ToWellFormedUtf16", static i => TextRender.Str(J.JSStringText.ToWellFormedUtf16(i.Text)), static i => TextRender.Str(Utf16.ToWellFormed(i.Text))),
            new("V", "toWellFormed", static i => VCharged(e => TextRender.Str(V.JsRealmText.ToWellFormed(e, i.Text))),
                static i => Charged(m => TextRender.Str(Utf16.ToWellFormed(i.Text, m.Charge)))),
        ]);

        // codePointAt's adapter in both engines: undefined outside the String; Broiler.VM charges one unit first.
        static string SCodePoint(TextInput i) =>
            i.Int < 0 || i.Int >= i.Text.Length ? Render.Undefined : Render.Number(Utf16.CodePointAt(i.Text, (int)i.Int).CodePoint);

        yield return new("CodePointAt", Strings, static input => Enumerable.Range(-1, input.Text.Length + 3).Select(at => input with { Int = at, Detail = at.ToString(CultureInfo.InvariantCulture) }),
        [
            new("J", "JSString.CodePointAt", static i => Num(J.JSStringText.CodePointAt(i.Text, i.Int)), SCodePoint),
            new("V", "codePointAt", static i => VCharged(e => Num(V.JsRealmText.CodePointAt(e, i.Text, i.Int))),
                static i => Charged(m => { m.Charge(1); return SCodePoint(i); })),
        ]);

        static IEnumerable<TextInput> Indices(TextInput input) =>
            from at in Enumerable.Range(-1, input.Text.Length + 3)
            from unicode in new[] { 0, 1 }
            select input with { Int = at, Parameter = unicode, Detail = at.ToString(CultureInfo.InvariantCulture) + (unicode == 1 ? "/unicode" : string.Empty) };

        static bool NotNegative(TextInput i) => i.Int >= 0;

        yield return new("AdvanceStringIndex", Strings, Indices,
        [
            new("J", "AdvanceStringIndex", static i => Render.Number((long)J.JSStringText.AdvanceStringIndex(i.Text, (int)i.Int, i.Parameter == 1)),
                static i => Render.Number(Utf16.AdvanceStringIndex(i.Text, i.Int, i.Parameter == 1)), NotNegative),
            new("V", "RegExpAdvanceIndex", static i => Render.Number(V.JsRealmText.RegExpAdvanceIndex(i.Text, i.Int, i.Parameter == 1)),
                static i => Render.Number(Utf16.AdvanceStringIndex(i.Text, i.Int, i.Parameter == 1)), NotNegative),
        ]);

        // QuoteJSONString over T1's strings (the rule's J1: "for QuoteJSONString, T1's strings").
        yield return new("QuoteJSONString", Strings, Once,
        [
            new("J", "JSJSON.QuoteString", static i =>
            {
                var writer = new StringWriter();
                J.JSJsonText.QuoteString(i.Text, writer);
                return TextRender.Str(writer.ToString());
            }, static i => TextRender.Str(JsonText.Quote(i.Text))),
            new("V", "JsonQuote", static i => VCharged(e => TextRender.Str(V.JsRealmJson.JsonQuote(e, i.Text))),
                static i => Charged(m => TextRender.Str(JsonText.Quote(i.Text, m.Charge)))),
        ]);
    }

    // ---- Padding and indices --------------------------------------------------------------------

    /// <summary>The fillers, from P1F, set by the comparison before it runs.</summary>
    public static IReadOnlyList<string> Fillers { get; set; } = [];

    /// <summary>The maximum lengths, from P1M, set by the comparison before it runs.</summary>
    public static IReadOnlyList<long> Maxima { get; set; } = [];

    private static IEnumerable<TextInput> Pads(TextInput input)
    {
        var targets = Enumerable.Range(0, 71).Select(static t => (long)t);
        if (input.Class == "P1S")
            targets = targets.Concat(Maxima.SelectMany(static m => new[] { m - 1, m, m + 1 }).Where(static t => t > 70));

        foreach (var target in targets)
        {
            for (var f = 0; f < Fillers.Count; f++)
            {
                for (var placement = 0; placement < 2; placement++)
                {
                    yield return input with
                    {
                        Int = target, Text2 = Fillers[f], Parameter = placement,
                        Detail = target.ToString(CultureInfo.InvariantCulture) + "/filler" + f.ToString(CultureInfo.InvariantCulture) + (placement == 0 ? "/start" : "/end"),
                    };
                }
            }
        }
    }

    /// <summary>S as an engine's padStart and padEnd call it: the early answer before the filler, then the library under the engine's ceiling.</summary>
    private static string SPad(TextInput i, long ceiling, WorkCharge? charge)
    {
        if (i.Int <= i.Text.Length)
        {
            charge?.Invoke(i.Text.Length == 0 ? 1UL : (ulong)i.Text.Length);
            return TextRender.Str(i.Text);
        }

        var status = StringPadding.Pad(i.Text, i.Int, i.Text2!, i.Parameter == 0 ? PadPlacement.Start : PadPlacement.End, new LengthLimit(ceiling), out var padded, charge);
        return status == ResultStatus.Completed ? TextRender.Str(padded) : TextRender.Error("RangeError");
    }

    private static IEnumerable<TextOperation> PaddingOperations()
    {
        var variants = new List<TextVariant>
        {
            new("V", "StringPadTo", static i => VCharged(e => TextRender.Str(V.JsRealmText.StringPadTo(e, i.Text, i.Int, i.Text2, i.Parameter == 0))),
                static i => Charged(m => SPad(i, VmStringCeiling, m.Charge))),
            new("J", "StringPad", static i => Safe(() => TextRender.Str(J.JSStringText.StringPad(i.Text, i.Int, i.Text2, i.Parameter == 0))),
                static i => SPad(i, int.MaxValue, null), static i => i.Int <= JPadBound),
        };

        // S under each other maximum length, against S under arm J's: the same result, or a refusal when the pad is longer than the maximum.
        foreach (var maximum in new[] { 0L, 1, 16, 1L << 30 })
        {
            variants.Add(new("S", "under the maximum " + maximum.ToString(CultureInfo.InvariantCulture),
                i => SPad(i, maximum, null),
                i => i.Int > maximum && i.Int > i.Text.Length && i.Text2!.Length > 0 ? TextRender.Error("RangeError") : SPad(i, int.MaxValue, null)));
        }

        yield return new("StringPad", ["P1S", "T1R"], Pads, [.. variants]);

        static int Small(TextInput i) => (int)i.Int;
        static bool IntLength(TextInput i) => i.Int <= int.MaxValue;

        // Broiler.JS's typed arrays clamp ToIntegerOrInfinity's result to the long sentinels first (LongIntegerOf).
        static long LongInteger(double integer) => integer >= long.MaxValue ? long.MaxValue : integer <= long.MinValue ? long.MinValue : (long)integer;

        static string S(TextInput i) => Render.Number(RelativeIndex.Clamped(i.Number, i.Int));

        yield return new("RelativeIndex", ["P1X"], static input => [input with { Detail = input.Raw }],
        [
            new("J", "JSString.Relative", static i => Render.Number((long)J.JSStringText.Relative(i.Number, Small(i))), S, IntLength),
            new("J", "JSTypedArray.ClampRelativeIndex", static i => Render.Number((long)J.JSStringText.ClampRelativeIndex(LongInteger(i.Number), Small(i))), S, IntLength),
            new("V", "StringRelativeIndex", static i => Render.Number((long)V.JsRealmText.StringRelativeIndex(i.Number, Small(i))), S, IntLength),
            new("V", "ArrayRelative", static i => Render.Number(V.JsRealmText.ArrayRelative(i.Number, i.Int)), S),
        ]);
    }

    // ---- Substitution templates -----------------------------------------------------------------

    private static readonly int[] CaptureCounts = [0, 1, 2, 9, 10, 11, 99];

    private static readonly string[] CaptureStates = ["defined", "undefined", "empty"];

    private static readonly string[] Positions = ["start", "middle", "end"];

    private const string Matched = "ab";

    private static string Subject(int position) => position switch { 0 => "abzw", 1 => "xyabzw", _ => "xyab" };

    private static int At(int position) => position == 0 ? 0 : 2;

    private static List<string?> Captures(int count, int state) =>
        Enumerable.Range(1, count).Select(n => state switch { 0 => "c" + n.ToString(CultureInfo.InvariantCulture), 1 => (string?)null, _ => string.Empty }).ToList();

    private static Dictionary<string, string?>? Named(int configuration) =>
        configuration == 0 ? null : new Dictionary<string, string?>(StringComparer.Ordinal) { ["a"] = "ga", ["b"] = null };

    private static IEnumerable<TextInput> Templates(TextInput input) =>
        from count in CaptureCounts
        from state in count == 0 ? new[] { 0 } : new[] { 0, 1, 2 }
        from named in new[] { 0, 1 }
        from position in new[] { 0, 1, 2 }
        select input with
        {
            Int = count, Parameter2 = state, Parameter3 = named, Parameter4 = position,
            Detail = count.ToString(CultureInfo.InvariantCulture) + "/" + (count == 0 ? "-" : CaptureStates[state]) + "/" + (named == 0 ? "no-groups" : "groups") + "/" + Positions[position],
        };

    /// <summary>S: the template parsed by the library and its parts evaluated as Broiler.VM's RegExpSubstitute evaluates them, charging each expansion its length plus one.</summary>
    private static string SSubstitute(TextInput i, WorkCharge? charge)
    {
        var subject = Subject(i.Parameter4);
        var position = At(i.Parameter4);
        var captures = Captures((int)i.Int, i.Parameter2);
        var named = Named(i.Parameter3);
        var template = i.Text;
        var parts = ReplacementTemplate.Parse(template, captures.Count, named is not null, charge);
        var builder = new StringBuilder();

        void Append(string text)
        {
            charge?.Invoke((ulong)text.Length + 1);
            builder.Append(text);
        }

        foreach (var part in parts)
        {
            switch (part.Kind)
            {
                case ReplacementPartKind.Literal: builder.Append(template, part.Start, part.Length); break;
                case ReplacementPartKind.Dollar: builder.Append('$'); break;
                case ReplacementPartKind.Match: Append(Matched); break;
                case ReplacementPartKind.Before: Append(subject[..position]); break;
                case ReplacementPartKind.After: Append(subject[Math.Min(position + Matched.Length, subject.Length)..]); break;
                case ReplacementPartKind.Capture:
                    if (captures[part.CaptureIndex - 1] is { } capture)
                        Append(capture);
                    break;
                case ReplacementPartKind.NamedCapture:
                    if (named!.TryGetValue(template.Substring(part.Start, part.Length), out var value) && value is not null)
                        Append(value);
                    break;
            }
        }

        return TextRender.Str(builder.ToString());
    }

    private static string SReplaced(TextInput i)
    {
        var subject = Subject(i.Parameter4);
        var position = At(i.Parameter4);
        var replacement = SSubstitute(i, null)["str:".Length..];
        return TextRender.Str(subject[..position] + NumericsPopulation.Unescape(replacement) + subject[(position + Matched.Length)..]);
    }

    private static bool StringPattern(TextInput i) => i.Int == 0 && i.Parameter3 == 0;

    private static J.RegexMatchData Match(TextInput i)
    {
        var position = At(i.Parameter4);
        var groups = new List<J.RegexCapture> { new(true, position, Matched.Length, Matched) };
        groups.AddRange(Captures((int)i.Int, i.Parameter2).Select(c => new J.RegexCapture(c is not null, c is null ? -1 : position, c?.Length ?? 0, c!)));
        return new J.RegexMatchData { Success = true, Index = position, Length = Matched.Length, Value = Matched, Groups = [.. groups] };
    }

    private static TextOperation SubstitutionOperation() =>
        new("GetSubstitution", ["S1E", "S1R"], Templates,
        [
            new("V", "RegExpSubstitute", static i => VCharged(e => TextRender.Str(V.JsRealmText.RegExpSubstitute(e, Matched, Subject(i.Parameter4), At(i.Parameter4), Captures((int)i.Int, i.Parameter2), Named(i.Parameter3), i.Text))),
                static i => Charged(m => SSubstitute(i, m.Charge))),
            new("V", "StringExpandDollars", static i => TextRender.Str(V.JsRealmText.StringExpandDollars(i.Text, Matched, Subject(i.Parameter4), At(i.Parameter4))),
                static i => SSubstitute(i, null), StringPattern),
            new("V", "RegExpReplaceText", static i => VCharged(e => TextRender.Str(V.JsRealmText.RegExpReplaceText(e, Subject(i.Parameter4), Matched, i.Text))),
                static i => Render.Charged(SReplaced(i), 0), StringPattern),
            new("J", "JSString.GetSubstitution", static i => TextRender.Str(J.JSStringText.GetSubstitution(Matched, Subject(i.Parameter4), At(i.Parameter4), i.Text)),
                static i => SSubstitute(i, null), StringPattern),
            new("J", "initializer GetSubstitution", static i => Safe(() => TextRender.Str(J.JSStringText.InitializerGetSubstitution(Matched, Subject(i.Parameter4), At(i.Parameter4), Captures((int)i.Int, i.Parameter2), Named(i.Parameter3), i.Text))),
                static i => SSubstitute(i, null)),
            new("J", "JSRegExp.Replace(string, string)", static i => Safe(() => TextRender.Str(J.JSStringText.Replace(Subject(i.Parameter4), i.Text, Match(i)))),
                static i => SSubstitute(i, null)),
        ]);

    // ---- URI ------------------------------------------------------------------------------------

    private static string SEncode(TextInput i, string extra, WorkCharge? charge) =>
        UriCodec.Encode(i.Text, extra, out var result, out _, charge) == ResultStatus.Completed ? TextRender.Str(result) : TextRender.Error("URIError");

    private static string SDecode(TextInput i, string preserve, WorkCharge? charge) =>
        UriCodec.Decode(i.Text, preserve, out var result, out _, charge) == ResultStatus.Completed ? TextRender.Str(result) : TextRender.Error("URIError");

    /// <summary>A U1C code point's text as escape writes it, for unescape's inverse: each code unit as %XX below 256 and %uXXXX above.</summary>
    private static IEnumerable<TextInput> Escaped(TextInput input)
    {
        if (input.Class != "U1C")
            return [input];

        var text = new StringBuilder();
        foreach (var unit in input.Text)
        {
            text.Append(unit < 256
                ? "%" + ((int)unit).ToString("X2", CultureInfo.InvariantCulture)
                : "%u" + ((int)unit).ToString("X4", CultureInfo.InvariantCulture));
        }

        return [input with { Text = text.ToString(), Detail = "escaped" }];
    }

    private static IEnumerable<TextOperation> UriOperations()
    {
        yield return new("Encode encodeURIComponent", ["U1C", "U1R"], Once,
        [
            new("J", "UriHelper.EncodeURIComponent", static i => Safe(() => TextRender.Str(J.UriHelper.EncodeURIComponent(i.Text))), static i => SEncode(i, string.Empty, null)),
            new("V", "GlobalUriEncode", static i => VCharged(e => TextRender.Str(V.JsRealmText.GlobalUriEncode(e, i.Text, V.JsRealmText.GlobalUriUnescapedComponent))),
                static i => Charged(m => SEncode(i, string.Empty, m.Charge))),
        ]);
        yield return new("Encode encodeURI", ["U1C", "U1R"], Once,
        [
            new("J", "UriHelper.EncodeURI", static i => Safe(() => TextRender.Str(J.UriHelper.EncodeURI(i.Text))), static i => SEncode(i, UriCodec.ReservedAndNumberSign, null)),
            new("V", "GlobalUriEncode", static i => VCharged(e => TextRender.Str(V.JsRealmText.GlobalUriEncode(e, i.Text, V.JsRealmText.GlobalUriUnescapedUri))),
                static i => Charged(m => SEncode(i, UriCodec.ReservedAndNumberSign, m.Charge))),
        ]);
        yield return new("Decode decodeURIComponent", ["U1D", "U1R"], Once,
        [
            new("J", "UriHelper.DecodeURIComponent", static i => Safe(() => TextRender.Str(J.UriHelper.DecodeURIComponent(i.Text))), static i => SDecode(i, string.Empty, null)),
            new("V", "GlobalUriDecode", static i => VCharged(e => TextRender.Str(V.JsRealmText.GlobalUriDecode(e, i.Text, string.Empty))),
                static i => Charged(m => SDecode(i, string.Empty, m.Charge))),
        ]);
        yield return new("Decode decodeURI", ["U1D", "U1R"], Once,
        [
            new("J", "UriHelper.DecodeURI", static i => Safe(() => TextRender.Str(J.UriHelper.DecodeURI(i.Text))), static i => SDecode(i, UriCodec.ReservedAndNumberSign, null)),
            new("V", "GlobalUriDecode", static i => VCharged(e => TextRender.Str(V.JsRealmText.GlobalUriDecode(e, i.Text, V.JsRealmText.GlobalUriPreservedUri))),
                static i => Charged(m => SDecode(i, UriCodec.ReservedAndNumberSign, m.Charge))),
        ]);
        yield return new("escape", ["U1C", "U1R"], Once,
        [
            new("J", "UriHelper.Escape", static i => TextRender.Str(J.UriHelper.Escape(i.Text)), static i => TextRender.Str(UriCodec.Escape(i.Text))),
            new("V", "GlobalEscapeText", static i => VCharged(e => TextRender.Str(V.JsRealmText.GlobalEscapeText(e, i.Text))),
                static i => Charged(m => TextRender.Str(UriCodec.Escape(i.Text, m.Charge)))),
        ]);
        yield return new("unescape", ["U1N", "U1C", "U1R"], Escaped,
        [
            new("J", "UriHelper.Unescape", static i => TextRender.Str(J.UriHelper.Unescape(i.Text)), static i => TextRender.Str(UriCodec.Unescape(i.Text))),
            new("V", "GlobalUnescapeText", static i => VCharged(e => TextRender.Str(V.JsRealmText.GlobalUnescapeText(e, i.Text))),
                static i => Charged(m => TextRender.Str(UriCodec.Unescape(i.Text, m.Charge)))),
        ]);
    }

    // ---- Base64 and hex ---------------------------------------------------------------------------

    private static readonly int[] Destinations = [0, 1, 2, 3, 4, -1];

    private static readonly string[] Modes = ["loose", "strict", "stop-before-partial"];

    private static IEnumerable<TextInput> Decodes(TextInput input) =>
        from alphabet in new[] { 0, 1 }
        from mode in new[] { 0, 1, 2 }
        from destination in Destinations
        select input with
        {
            Parameter = alphabet, Parameter2 = mode, Int = destination,
            Detail = (alphabet == 0 ? "base64" : "base64url") + "/" + Modes[mode] + "/" + (destination < 0 ? "unbounded" : destination.ToString(CultureInfo.InvariantCulture)),
        };

    private static IEnumerable<TextInput> HexDecodes(TextInput input) =>
        Destinations.Select(d => input with { Int = d, Detail = d < 0 ? "unbounded" : d.ToString(CultureInfo.InvariantCulture) });

    /// <summary>S's FromBase64 into the input's destination (one never filled for unbounded), as the outcome Broiler.VM's Base64Result carries.</summary>
    private static string SBase64(TextInput i, WorkCharge? charge)
    {
        var destination = new byte[i.Int < 0 ? Base64Codec.DecodedLengthBound(i.Text.Length) : (int)i.Int];
        var status = Base64Codec.FromBase64(i.Text, i.Parameter == 0 ? Base64Alphabet.Base64 : Base64Alphabet.Base64Url,
            (LastChunkHandling)i.Parameter2, destination, out var read, out var written, out _, charge);
        return TextRender.Decoded(read, destination.AsSpan(0, written), status == ResultStatus.Completed ? null : "SyntaxError");
    }

    private static string SHex(TextInput i, WorkCharge? charge)
    {
        var destination = new byte[i.Int < 0 ? i.Text.Length / 2 + 1 : (int)i.Int];
        var status = Base64Codec.FromHex(i.Text, destination, out var read, out var written, out _, charge);
        return TextRender.Decoded(read, destination.AsSpan(0, written), status == ResultStatus.Completed ? null : "SyntaxError");
    }

    /// <summary>Broiler.VM's FromBase64 maxLength: the destination's length, or long.MaxValue for fromBase64.</summary>
    private static long VMax(TextInput i) => i.Int < 0 ? long.MaxValue : i.Int;

    private static string VResult(V.JsRealmText.Base64Result result) =>
        TextRender.Decoded(result.Read, result.Bytes.ToArray(), result.Error is null ? null : "SyntaxError");

    /// <summary>arm J's FromHex and SetFromHex, projected: the bytes, or (read, written, destination) for a destination, or the error with the destination as written.</summary>
    private static string JHex(TextInput i)
    {
        if (i.Int < 0)
            return Safe(() => TextRender.Bytes(J.JSUInt8ArrayText.FromHex(i.Text)));

        var buffer = new byte[i.Int];
        try
        {
            var (read, written) = J.JSUInt8ArrayText.SetFromHex(i.Text, buffer);
            return "read:" + read.ToString(CultureInfo.InvariantCulture) + "|written:" + written.ToString(CultureInfo.InvariantCulture) + "|" + TextRender.Bytes(buffer);
        }
        catch (ArmError error)
        {
            return TextRender.Error(error.Kind) + "|" + TextRender.Bytes(buffer);
        }
    }

    /// <summary>S projected as arm J's fromHex and setFromHex answer.</summary>
    private static string SJHex(TextInput i)
    {
        var destination = new byte[i.Int < 0 ? i.Text.Length / 2 + 1 : (int)i.Int];
        var status = Base64Codec.FromHex(i.Text, destination, out var read, out var written, out _);
        if (i.Int < 0)
            return status == ResultStatus.Completed ? TextRender.Bytes(destination.AsSpan(0, written)) : TextRender.Error("SyntaxError");

        return status == ResultStatus.Completed
            ? "read:" + read.ToString(CultureInfo.InvariantCulture) + "|written:" + written.ToString(CultureInfo.InvariantCulture) + "|" + TextRender.Bytes(destination)
            : TextRender.Error("SyntaxError") + "|" + TextRender.Bytes(destination);
    }

    private static IEnumerable<TextOperation> Base64Operations()
    {
        yield return new("FromBase64", ["B1E", "B1R"], Decodes,
        [
            new("J", "DecodeBase64", static i =>
            {
                var result = J.JSUInt8ArrayText.DecodeBase64(i.Text, i.Parameter == 0 ? "base64" : "base64url", Modes[i.Parameter2], i.Int < 0 ? int.MaxValue : (int)i.Int);
                return TextRender.Decoded(result.Read, result.Bytes, result.Error?.Kind);
            }, static i => SBase64(i, null)),
            new("V", "Base64Decode", static i => VCharged(e => VResult(V.JsRealmText.Base64Decode(e, i.Text, i.Parameter == 1, Modes[i.Parameter2], VMax(i)))),
                static i => Charged(m => SBase64(i, m.Charge))),
        ]);
        yield return new("FromHex", ["B1H", "B1Q"], HexDecodes,
        [
            new("J", "FromHex and SetFromHex", JHex, SJHex),
            new("V", "Base64DecodeHex", static i => VCharged(e => VResult(V.JsRealmText.Base64DecodeHex(e, i.Text, VMax(i)))),
                static i => Charged(m => SHex(i, m.Charge))),
        ]);

        static IEnumerable<TextInput> Encodings(TextInput input) =>
            from alphabet in new[] { 0, 1 }
            from omit in new[] { 0, 1 }
            select input with { Parameter = alphabet, Parameter2 = omit, Detail = (alphabet == 0 ? "base64" : "base64url") + (omit == 1 ? "/omitPadding" : string.Empty) };

        static string SBase64Text(TextInput i, WorkCharge? charge) =>
            TextRender.Str(Base64Codec.ToBase64(i.Bytes, i.Parameter == 0 ? Base64Alphabet.Base64 : Base64Alphabet.Base64Url, i.Parameter2 == 1, charge));

        yield return new("ToBase64", ["B1Y"], Encodings,
        [
            new("J", "ToBase64", static i => TextRender.Str(J.JSUInt8ArrayText.ToBase64(i.Bytes, i.Parameter == 0 ? "base64" : "base64url", i.Parameter2 == 1)), static i => SBase64Text(i, null)),
            new("V", "Base64Encode", static i => VCharged(e => TextRender.Str(V.JsRealmText.Base64Encode(e, i.Bytes, i.Parameter == 1, i.Parameter2 == 1))),
                static i => Charged(m => SBase64Text(i, m.Charge))),
        ]);
        yield return new("ToHex", ["B1Y"], Once,
        [
            new("J", "ToHex", static i => TextRender.Str(J.JSUInt8ArrayText.ToHex(i.Bytes)), static i => TextRender.Str(Base64Codec.ToHex(i.Bytes))),
            new("V", "toHex", static i => VCharged(e => TextRender.Str(V.JsRealmText.ToHex(e, i.Bytes))),
                static i => Charged(m => TextRender.Str(Base64Codec.ToHex(i.Bytes, m.Charge)))),
        ]);
    }

    // ---- JSON -----------------------------------------------------------------------------------

    /// <summary>S's parse as an engine's JSON.parse builds it over the reader, or the error it raises.</summary>
    internal static string SParse(string text, int depth, WorkCharge? charge)
    {
        var reader = new JsonReader(text, new DepthLimit(depth), charge);
        var stack = new Stack<(JsonTree Container, string? Name)>();
        string? name = null;
        JsonTree? root = null;
        while (true)
        {
            var status = reader.Read();
            if (status != ResultStatus.Completed)
                return TextRender.Error(status == ResultStatus.LimitExceeded ? "RangeError" : "SyntaxError");

            JsonTree completed;
            switch (reader.Token)
            {
                case JsonToken.BeginObject:
                    stack.Push((new JsonTree { Kind = JsonTreeKind.Object, Members = [] }, name));
                    continue;
                case JsonToken.BeginArray:
                    stack.Push((new JsonTree { Kind = JsonTreeKind.Array, Elements = [] }, name));
                    continue;
                case JsonToken.PropertyName:
                    name = reader.StringValue;
                    continue;
                case JsonToken.EndObject:
                case JsonToken.EndArray:
                    (completed, name) = stack.Pop();
                    break;
                case JsonToken.String:
                    completed = new JsonTree { Kind = JsonTreeKind.String, String = reader.StringValue };
                    break;
                case JsonToken.Number:
                    completed = new JsonTree { Kind = JsonTreeKind.Number, Number = reader.NumberValue };
                    break;
                case JsonToken.True:
                    completed = new JsonTree { Kind = JsonTreeKind.True };
                    break;
                case JsonToken.False:
                    completed = new JsonTree { Kind = JsonTreeKind.False };
                    break;
                case JsonToken.Null:
                    completed = new JsonTree { Kind = JsonTreeKind.Null };
                    break;
                default:
                    return TextRender.Json(root!);
            }

            if (stack.Count == 0)
            {
                root = completed;
                continue;
            }

            var container = stack.Peek().Container;
            if (container.Elements is { } elements)
            {
                elements.Add(completed);
                continue;
            }

            var members = container.Members!;
            var at = members.FindIndex(m => m.Key == name);
            if (at < 0)
                members.Add((name!, completed));
            else
                members[at] = (name!, completed);
        }
    }

    private static JsonTree FromV(V.JsValue value) => value.Payload switch
    {
        null => new JsonTree { Kind = JsonTreeKind.Null },
        bool b => new JsonTree { Kind = b ? JsonTreeKind.True : JsonTreeKind.False },
        double d => new JsonTree { Kind = JsonTreeKind.Number, Number = d },
        string s => new JsonTree { Kind = JsonTreeKind.String, String = s },
        V.JsArray a => new JsonTree { Kind = JsonTreeKind.Array, Elements = a.Elements.Select(FromV).ToList() },
        V.JsObject o => new JsonTree { Kind = JsonTreeKind.Object, Members = o.Members.Select(m => (m.Key, FromV(m.Value))).ToList() },
        _ => throw new InvalidOperationException("arm V built a " + value.Payload.GetType().Name),
    };

    private static JsonTree FromJ(J.JSValue value) => value switch
    {
        J.JSNull => new JsonTree { Kind = JsonTreeKind.Null },
        J.JSBoolean b => new JsonTree { Kind = b.Value ? JsonTreeKind.True : JsonTreeKind.False },
        J.JSNumber n => new JsonTree { Kind = JsonTreeKind.Number, Number = n.Value },
        J.JSString s => new JsonTree { Kind = JsonTreeKind.String, String = s.Value },
        J.JSArray a => new JsonTree { Kind = JsonTreeKind.Array, Elements = a.Elements.Select(FromJ).ToList() },
        J.JSObject o => new JsonTree
        {
            Kind = JsonTreeKind.Object,
            Members = o.Indexed.Select(p => (p.Key.ToString(CultureInfo.InvariantCulture), FromJ(p.Value))).Concat(o.Named.Select(p => (p.Key, FromJ(p.Value)))).ToList(),
        },
        _ => throw new InvalidOperationException("arm J built a " + value.GetType().Name),
    };

    private static string SModule(string text)
    {
        var status = JsonReader.Validate(text, new DepthLimit(VmModuleDepth), out _);
        return status switch
        {
            ResultStatus.Completed => "accepted",
            ResultStatus.LimitExceeded => "refused:too-deep",
            _ => "refused",
        };
    }

    private static IEnumerable<TextOperation> JsonOperations()
    {
        string[] texts = ["J1E", "J1R", "J1S", "J1N", "J1D"];
        yield return new("JSON parse", texts, Once,
        [
            new("V", "JsonParseEntry", static i => VCharged(e => TextRender.Json(FromV(new V.JsRealmJson().JsonParseEntry(e, i.Text)))),
                static i => Charged(m => SParse(i.Text, VmJsonDepth, m.Charge))),
            new("V", "JsJsonModule.TryTranslate", static i => V.JsJsonModule.TryTranslate(i.Text, out _, out _, out var tooDeep) ? "accepted" : tooDeep ? "refused:too-deep" : "refused",
                static i => SModule(i.Text)),
            new("J", "JSJSON.Parse", static i => Safe(() => TextRender.Json(FromJ(J.JSJsonText.Parse(i.Text)))),
                static i => SParse(i.Text, int.MaxValue, null)),
        ],
        SharedOnly: static i => SParse(i.Text, 64, null));

        yield return new("JSON gap", ["J1G"], Once,
        [
            new("J", "Stringify's gap", static i => TextRender.Str(i.Parameter == 0 ? J.JSJsonText.GapFromNumber(i.Number) : J.JSJsonText.GapFromString(i.Text)),
                static i => TextRender.Str(i.Parameter == 0 ? JsonText.GapFromNumber(i.Number) : JsonText.GapFromString(i.Text))),
            new("V", "JsonApplySpace", static i =>
            {
                var state = new V.JsRealmJson.JsonWriter();
                V.JsRealmJson.JsonApplySpace(state, i.Parameter == 0 ? V.JsValue.Number(i.Number) : V.JsValue.String(i.Text));
                return TextRender.Str(state.Gap);
            }, static i => TextRender.Str(i.Parameter == 0 ? JsonText.GapFromNumber(i.Number) : JsonText.GapFromString(i.Text))),
        ]);
    }
}
