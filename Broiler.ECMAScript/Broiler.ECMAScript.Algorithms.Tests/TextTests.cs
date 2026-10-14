using System;
using System.Collections.Generic;
using System.Linq;
using Broiler.ECMAScript.Text;
using Xunit;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The text area (milestone SJL-5.3): each member's edges, failures and charges. The area's vectors are
/// its own tests, and the reference-arm comparison holds every charge to Broiler.VM's; these pin the
/// cases a vector cannot show, such as where a charge falls and what a failure reports.
/// </summary>
public class TextTests
{
    private sealed class Meter
    {
        public List<ulong> Charges { get; } = [];

        public ulong Total => Charges.Aggregate(0UL, static (sum, units) => sum + units);

        public void Charge(ulong units) => Charges.Add(units);
    }

    private static readonly string Lead = ((char)0xD800).ToString();

    private static readonly string Trail = ((char)0xDC00).ToString();

    // ---- UTF-16 -------------------------------------------------------------------------------

    [Fact]
    public void CodePointAt_reads_a_pair_as_one_code_point_and_a_lone_surrogate_as_itself()
    {
        Assert.Equal(new CodePointRecord(0x10000, 2, false), Utf16.CodePointAt(Lead + Trail, 0));
        Assert.Equal(new CodePointRecord(0xDC00, 1, true), Utf16.CodePointAt(Lead + Trail, 1));
        Assert.Equal(new CodePointRecord(0xD800, 1, true), Utf16.CodePointAt(Lead + "a", 0));
        Assert.Equal(new CodePointRecord('a', 1, false), Utf16.CodePointAt("a", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Utf16.CodePointAt("a", 1));
    }

    [Fact]
    public void ToWellFormed_replaces_each_lone_surrogate_and_answers_a_well_formed_String_itself()
    {
        var text = "a" + Lead + Trail + "b";
        Assert.Same(text, Utf16.ToWellFormed(text));
        Assert.Equal("a" + (char)0xFFFD + "b" + (char)0xFFFD, Utf16.ToWellFormed("a" + Trail + "b" + Lead));
        Assert.True(Utf16.IsWellFormed(text));
        Assert.False(Utf16.IsWellFormed(Trail + Lead));

        var meter = new Meter();
        _ = Utf16.ToWellFormed("abc", meter.Charge);
        _ = Utf16.IsWellFormed("abc", meter.Charge);
        Assert.Equal([4UL, 4UL], meter.Charges);
    }

    [Theory]
    [InlineData(0, false, 1)]
    [InlineData(0, true, 2)]
    [InlineData(1, true, 2)]
    [InlineData(2, true, 3)]
    [InlineData(5, true, 6)]
    public void AdvanceStringIndex_steps_over_a_pair_only_in_Unicode_mode(long index, bool unicode, long expected) =>
        Assert.Equal(expected, Utf16.AdvanceStringIndex(Lead + Trail + "x", index, unicode));

    // ---- Padding and indices ----------------------------------------------------------------------

    [Fact]
    public void Pad_repeats_and_truncates_the_filler_and_charges_the_result()
    {
        var meter = new Meter();
        Assert.Equal(ResultStatus.Completed, StringPadding.Pad("x", 6, "ab", PadPlacement.Start, new LengthLimit(16), out var start, meter.Charge));
        Assert.Equal("ababax", start);
        Assert.Equal(ResultStatus.Completed, StringPadding.Pad("x", 4, "ab", PadPlacement.End, new LengthLimit(16), out var end, meter.Charge));
        Assert.Equal("xaba", end);
        Assert.Equal([6UL, 4UL], meter.Charges);
    }

    [Fact]
    public void Pad_answers_the_String_for_an_empty_filler_past_the_limit_and_refuses_otherwise_without_a_charge()
    {
        var meter = new Meter();
        Assert.Equal(ResultStatus.Completed, StringPadding.Pad("x", 100, string.Empty, PadPlacement.End, new LengthLimit(16), out var same, meter.Charge));
        Assert.Equal("x", same);
        Assert.Equal(ResultStatus.Completed, StringPadding.Pad(string.Empty, 0, " ", PadPlacement.End, new LengthLimit(16), out _, meter.Charge));
        Assert.Equal([1UL, 1UL], meter.Charges);

        Assert.Equal(ResultStatus.LimitExceeded, StringPadding.Pad("x", 17, " ", PadPlacement.End, new LengthLimit(16), out _, meter.Charge));
        Assert.Equal(ResultStatus.LimitExceeded, StringPadding.Pad("x", int.MaxValue, " ", PadPlacement.End, new LengthLimit(long.MaxValue), out _, meter.Charge));
        Assert.Equal(2, meter.Charges.Count);
    }

    [Theory]
    [InlineData(double.NegativeInfinity, 7, 0)]
    [InlineData(-9, 7, 0)]
    [InlineData(-2, 7, 5)]
    [InlineData(3, 7, 3)]
    [InlineData(9, 7, 7)]
    [InlineData(double.PositiveInfinity, 7, 7)]
    [InlineData(-1, 0, 0)]
    public void Clamped_counts_back_from_the_end_and_stays_within_the_length(double relative, long length, long expected) =>
        Assert.Equal(expected, RelativeIndex.Clamped(relative, length));

    [Fact]
    public void TryElement_and_LastSearchStart_answer_only_an_index_of_the_list()
    {
        Assert.True(RelativeIndex.TryElement(-1, 7, out var last));
        Assert.Equal(6, last);
        Assert.False(RelativeIndex.TryElement(7, 7, out _));
        Assert.False(RelativeIndex.TryElement(-8, 7, out _));
        Assert.False(RelativeIndex.TryElement(double.NegativeInfinity, 7, out _));
        Assert.Equal(6, RelativeIndex.LastSearchStart(double.PositiveInfinity, 7));
        Assert.Equal(-1, RelativeIndex.LastSearchStart(double.NegativeInfinity, 7));
        Assert.Equal(-1, RelativeIndex.LastSearchStart(-8, 7));
        Assert.Equal(5, RelativeIndex.LastSearchStart(-2, 7));
        Assert.Throws<ArgumentOutOfRangeException>(() => RelativeIndex.Clamped(double.NaN, 7));
        Assert.Throws<ArgumentOutOfRangeException>(() => RelativeIndex.Clamped(0.5, 7));
    }

    // ---- Substitution templates -------------------------------------------------------------------

    private static string Kinds(ReplacementPart[] parts, string template) => string.Join(" ", parts.Select(p => p.Kind switch
    {
        ReplacementPartKind.Literal => "'" + template.Substring(p.Start, p.Length) + "'",
        ReplacementPartKind.Capture => "$" + p.CaptureIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ReplacementPartKind.NamedCapture => "<" + template.Substring(p.Start, p.Length) + ">",
        _ => p.Kind.ToString(),
    }));

    [Theory]
    [InlineData("", 0, false, "")]
    [InlineData("x", 0, false, "'x'")]
    [InlineData("$$$&$`$'", 0, false, "Dollar Match Before After")]
    [InlineData("a$1b", 0, false, "'a$1b'")]
    [InlineData("$01$10", 1, false, "$1 $1 '0'")]
    [InlineData("$10", 10, false, "$10")]
    [InlineData("$0$00", 10, false, "'$0$00'")]
    [InlineData("$<a>", 0, false, "'$<a>'")]
    [InlineData("$<a>", 0, true, "<a>")]
    [InlineData("$<a", 0, true, "'$<a'")]
    [InlineData("x$", 0, false, "'x$'")]
    [InlineData("$x", 0, false, "'$x'")]
    public void A_template_parses_into_the_parts_GetSubstitution_evaluates(string template, int captures, bool named, string expected) =>
        Assert.Equal(expected, Kinds(ReplacementTemplate.Parse(template, captures, named), template));

    [Fact]
    public void A_template_is_charged_its_length_plus_one()
    {
        var meter = new Meter();
        _ = ReplacementTemplate.Parse("abc", 0, false, meter.Charge);
        _ = ReplacementTemplate.Parse(string.Empty, 0, false, meter.Charge);
        Assert.Equal([4UL, 1UL], meter.Charges);
    }

    // ---- URI ---------------------------------------------------------------------------------------

    [Fact]
    public void Encode_and_Decode_round_trip_and_keep_the_preserved_escapes()
    {
        Assert.Equal(ResultStatus.Completed, UriCodec.Encode("a b/" + Lead + Trail, string.Empty, out var encoded, out _));
        Assert.Equal("a%20b%2F%F0%90%80%80", encoded);
        Assert.Equal(ResultStatus.Completed, UriCodec.Decode(encoded, string.Empty, out var decoded, out _));
        Assert.Equal("a b/" + Lead + Trail, decoded);
        Assert.Equal(ResultStatus.Completed, UriCodec.Decode("%2F%41", UriCodec.ReservedAndNumberSign, out var preserved, out _));
        Assert.Equal("%2FA", preserved);
    }

    [Theory]
    [InlineData("%", 0, "uri.malformed-escape")]
    [InlineData("a%4", 1, "uri.malformed-escape")]
    [InlineData("%80", 0, "uri.invalid-octet")]
    [InlineData("%C3%41", 3, "uri.invalid-octet")]
    [InlineData("%C3", 3, "uri.malformed-escape")]
    [InlineData("%C0%80", 0, "uri.invalid-utf8")]
    [InlineData("%ED%A0%80", 0, "uri.invalid-utf8")]
    [InlineData("%F4%90%80%80", 0, "uri.invalid-utf8")]
    public void A_malformed_decode_reports_where_and_why(string text, int position, string messageId)
    {
        Assert.Equal(ResultStatus.Malformed, UriCodec.Decode(text, string.Empty, out _, out var failure));
        Assert.Equal(new Failure(ResultStatus.Malformed, position, messageId), failure);
    }

    [Fact]
    public void The_codecs_charge_a_unit_a_step_and_a_unit_a_continuation_octet()
    {
        var meter = new Meter();
        _ = UriCodec.Encode("ab" + Lead + Trail, string.Empty, out _, out _, meter.Charge);
        Assert.Equal(3UL, meter.Total);
        meter.Charges.Clear();
        _ = UriCodec.Decode("x%E2%82%AC", string.Empty, out _, out _, meter.Charge);
        Assert.Equal(4UL, meter.Total);
        meter.Charges.Clear();
        _ = UriCodec.Decode("%C3", string.Empty, out _, out _, meter.Charge);
        Assert.Equal(2UL, meter.Total);
        meter.Charges.Clear();
        _ = UriCodec.Unescape("%u0041%41a", meter.Charge);
        Assert.Equal(3UL, meter.Total);
    }

    [Fact]
    public void Escape_and_Unescape_follow_Annex_B()
    {
        Assert.Equal("a%20%u0100@", UriCodec.Escape("a \u0100@"));
        Assert.Equal("a \u0100%uZZZZ%G", UriCodec.Unescape("a%20%u0100%uZZZZ%G"));
    }

    // ---- Base64 and hex ----------------------------------------------------------------------------

    [Fact]
    public void A_decode_into_a_short_destination_stops_before_a_chunk_that_would_not_fit()
    {
        var destination = new byte[4];
        Assert.Equal(ResultStatus.Completed, Base64Codec.FromBase64("aGVsbG8=", Base64Alphabet.Base64, LastChunkHandling.Loose, destination, out var read, out var written, out _));
        Assert.Equal((4, 3), (read, written));
        Assert.Equal([0x68, 0x65, 0x6C, 0x00], destination);
    }

    [Fact]
    public void A_decode_writes_what_it_decoded_before_its_error()
    {
        var destination = new byte[8];
        Assert.Equal(ResultStatus.Malformed, Base64Codec.FromBase64("aGVs!", Base64Alphabet.Base64, LastChunkHandling.Loose, destination, out var read, out var written, out var failure));
        Assert.Equal((4, 3), (read, written));
        Assert.Equal(new Failure(ResultStatus.Malformed, 4, "base64.invalid-character"), failure);

        var hex = new byte[1];
        Assert.Equal(ResultStatus.Completed, Base64Codec.FromHex("aaZZ", hex, out read, out written, out _));
        Assert.Equal((2, 1), (read, written));
        Assert.Equal(ResultStatus.Malformed, Base64Codec.FromHex("aaZZ", new byte[2], out read, out written, out failure));
        Assert.Equal((2, 1, "hex.invalid-digit"), (read, written, failure.MessageId));
    }

    [Theory]
    [InlineData("YQ", LastChunkHandling.Loose, ResultStatus.Completed, 2, "61")]
    [InlineData("YQ", LastChunkHandling.Strict, ResultStatus.Malformed, 0, "")]
    [InlineData("YQ", LastChunkHandling.StopBeforePartial, ResultStatus.Completed, 0, "")]
    [InlineData("YR==", LastChunkHandling.Loose, ResultStatus.Completed, 4, "61")]
    [InlineData("YR==", LastChunkHandling.Strict, ResultStatus.Malformed, 0, "")]
    [InlineData("YQ=", LastChunkHandling.Loose, ResultStatus.Malformed, 0, "")]
    [InlineData("YQ=", LastChunkHandling.StopBeforePartial, ResultStatus.Completed, 0, "")]
    [InlineData("Y", LastChunkHandling.Loose, ResultStatus.Malformed, 0, "")]
    [InlineData(" Y Q = = ", LastChunkHandling.Strict, ResultStatus.Completed, 9, "61")]
    public void The_last_chunk_is_handled_as_asked(string text, LastChunkHandling handling, ResultStatus status, int read, string bytes)
    {
        var destination = new byte[Base64Codec.DecodedLengthBound(text.Length)];
        Assert.Equal(status, Base64Codec.FromBase64(text, Base64Alphabet.Base64, handling, destination, out var actualRead, out var written, out _));
        Assert.Equal(read, actualRead);
        Assert.Equal(bytes, Convert.ToHexStringLower(destination.AsSpan(0, written)));
    }

    [Fact]
    public void The_encoders_write_both_alphabets_and_omit_padding_on_request()
    {
        byte[] data = [0xFB, 0xFF];
        Assert.Equal("+/8=", Base64Codec.ToBase64(data, Base64Alphabet.Base64, false));
        Assert.Equal("-_8", Base64Codec.ToBase64(data, Base64Alphabet.Base64Url, true));
        Assert.Equal("fbff", Base64Codec.ToHex(data));

        var meter = new Meter();
        _ = Base64Codec.ToBase64(data, Base64Alphabet.Base64, false, meter.Charge);
        _ = Base64Codec.ToHex(data, meter.Charge);
        _ = Base64Codec.FromBase64("YQ==", Base64Alphabet.Base64, LastChunkHandling.Loose, [], out _, out _, out _, meter.Charge);
        _ = Base64Codec.FromHex("abc", new byte[2], out _, out _, out _, meter.Charge);
        _ = Base64Codec.FromHex("ab", new byte[2], out _, out _, out _, meter.Charge);
        Assert.Equal([6UL, 5UL, 3UL], meter.Charges);
    }

    // ---- JSON ----------------------------------------------------------------------------------------

    private static List<string> Tokens(string text, int depth = 512)
    {
        var reader = new JsonReader(text, new DepthLimit(depth));
        var tokens = new List<string>();
        while (true)
        {
            var status = reader.Read();
            if (status != ResultStatus.Completed)
            {
                tokens.Add(reader.Failure.MessageId + "@" + reader.Failure.Position.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return tokens;
            }

            tokens.Add(reader.Token switch
            {
                JsonToken.PropertyName => "name:" + reader.StringValue,
                JsonToken.String => "string:" + reader.StringValue,
                JsonToken.Number => "number:" + reader.NumberValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                _ => reader.Token.ToString(),
            });
            if (reader.Token == JsonToken.EndOfText)
                return tokens;
        }
    }

    [Fact]
    public void The_reader_answers_tokens_in_document_order()
    {
        Assert.Equal(["BeginObject", "name:a", "BeginArray", "number:1", "string:x", "True", "Null", "EndArray", "name:b", "False", "EndObject", "EndOfText"],
            Tokens(" { \"a\" : [ 1 , \"x\" , true , null ] , \"b\":false } "));
        Assert.Equal(["number:-0", "EndOfText"], Tokens("-0"));
        Assert.Equal(["number:Infinity", "EndOfText"], Tokens("1e400"));
        Assert.Equal(["string:" + Lead, "EndOfText"], Tokens("\"\\ud800\""));
        Assert.Equal(["string:" + Lead, "EndOfText"], Tokens("\"" + Lead + "\""));
    }

    [Theory]
    [InlineData("", "json.unexpected@0")]
    [InlineData("01", "json.unexpected@1")]
    [InlineData("[1,]", "json.unexpected@3")]
    [InlineData("{\"a\" 1}", "json.unexpected@5")]
    [InlineData("\"abc", "json.unterminated-string@4")]
    [InlineData("\"a\tb\"", "json.control-character@2")]
    [InlineData("\"\\x\"", "json.bad-escape@2")]
    [InlineData("\"\\u12G4\"", "json.bad-unicode-escape@5")]
    [InlineData("\"\\u12", "json.bad-unicode-escape@3")]
    [InlineData("tru", "json.unexpected@0")]
    [InlineData("1 2", "json.unexpected@2")]
    public void A_malformed_text_fails_where_the_grammar_stops(string text, string expected) =>
        Assert.Equal(expected, Tokens(text)[^1]);

    [Fact]
    public void The_depth_is_the_value_s_and_is_refused_before_the_value_is_read()
    {
        Assert.Equal("EndOfText", Tokens("[[]]", 1)[^1]);
        Assert.Equal("json.too-deep@2", Tokens("[[1]]", 1)[^1]);
        Assert.Equal(ResultStatus.LimitExceeded, JsonReader.Validate(new string('[', 2000) + new string(']', 2000), new DepthLimit(1024), out var failure));
        Assert.Equal("json.too-deep", failure.MessageId);
    }

    [Fact]
    public void The_reader_charges_as_Broiler_VM_s_reader_did()
    {
        // {"a": [1, 2]}: one unit for each of the four values, one for ':' and one for ',', the two spaces
        // skipped one by one, the key's whole source length when it closes, and each number's length.
        var meter = new Meter();
        Assert.Equal(ResultStatus.Completed, JsonReader.Validate("{\"a\": [1, 2]}", new DepthLimit(512), out _, meter.Charge));
        Assert.Equal([1UL, 3UL, 1UL, 1UL, 1UL, 1UL, 1UL, 1UL, 1UL, 1UL, 1UL], meter.Charges);
    }

    [Fact]
    public void Quote_and_the_gap_follow_JSON_stringify()
    {
        Assert.Equal("\"a\\\"\\\\\\b\\f\\n\\r\\t\\u0001\\ud800" + Lead + Trail + "\"", JsonText.Quote("a\"\\\b\f\n\r\t\u0001" + Lead + Lead + Trail));
        var meter = new Meter();
        _ = JsonText.Quote("ab", meter.Charge);
        Assert.Equal([3UL], meter.Charges);
        Assert.Equal(string.Empty, JsonText.GapFromNumber(0.5));
        Assert.Equal("  ", JsonText.GapFromNumber(2.9));
        Assert.Equal(new string(' ', 10), JsonText.GapFromNumber(double.PositiveInfinity));
        Assert.Equal(string.Empty, JsonText.GapFromNumber(double.NaN));
        Assert.Equal("0123456789", JsonText.GapFromString("0123456789AB"));
    }
}
