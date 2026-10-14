// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   19
// Annotated:        19/19
// Exempt:           32
// Human-reviewed:   0/19
// IP risk:          Low
// Security risk:    High
// Criteria:         6/6
// Resource impact:  3/10 max
// Unverified:       19
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Text;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Text;

/// <summary>What <see cref="JsonReader.Read"/> found.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=Low; Resources=1; Fingerprint=6F9069
// Broiler-Human:        PENDING
public enum JsonToken
{
    /// <summary>Nothing has been read yet.</summary>
    None = 0,

    /// <summary><c>{</c>: an object begins.</summary>
    BeginObject = 1,

    /// <summary><c>}</c>: the innermost object ends.</summary>
    EndObject = 2,

    /// <summary><c>[</c>: an array begins.</summary>
    BeginArray = 3,

    /// <summary><c>]</c>: the innermost array ends.</summary>
    EndArray = 4,

    /// <summary>A member's name and its <c>:</c>; <see cref="JsonReader.StringValue"/> holds the name.</summary>
    PropertyName = 5,

    /// <summary>A string value; <see cref="JsonReader.StringValue"/> holds it.</summary>
    String = 6,

    /// <summary>A number value; <see cref="JsonReader.NumberValue"/> holds it.</summary>
    Number = 7,

    /// <summary><c>true</c>.</summary>
    True = 8,

    /// <summary><c>false</c>.</summary>
    False = 9,

    /// <summary><c>null</c>.</summary>
    Null = 10,

    /// <summary>The text ended after its one value and its trailing white space.</summary>
    EndOfText = 11,
}

/// <summary>
/// A pull reader over one JSON text in the grammar <c>JSON.parse</c> reads (ECMA-262 25.5.1, ECMA-404):
/// tokens in document order, under the caller's depth limit and meter.
/// </summary>
/// <remarks>
/// <para>
/// <b>The reader checks the grammar and converts the leaves; the caller builds the values</b>
/// (contract convention 2). Object and array construction, <c>CreateDataProperty</c>'s handling of a
/// repeated key, the reviver, the parse records and the raw-JSON brand stay in the engine, which reads
/// a <see cref="JsonToken.PropertyName"/> and then the member's value, and calls
/// <see cref="Read"/> again only when it is ready for the next token. A string's escapes are resolved
/// code unit by code unit, so an escaped or a raw lone surrogate is a string holding that code unit; a
/// number is <c>StringToNumber</c> of its text, so <c>1e400</c> is infinity.
/// </para>
/// <para>
/// <b>Depth is the value's, and the limit is the caller's policy</b> (contract convention 5): the
/// document is depth 0 and each member or element one deeper than its container, and a value deeper
/// than the limit is <see cref="ResultStatus.LimitExceeded"/>, never the stack's exhaustion - the reader
/// does not recurse. A malformed text is <see cref="ResultStatus.Malformed"/> with the position the
/// grammar stops at: <c>json.unexpected</c> (at a code unit the grammar has no place for, or at the end
/// of the text), <c>json.unterminated-string</c>, <c>json.control-character</c>, <c>json.bad-escape</c>
/// or <c>json.bad-unicode-escape</c>; past the limit it is <c>json.too-deep</c>.
/// </para>
/// <para>
/// <b>The charges are Broiler.VM's before the cut-over</b> (JSD-0070, decision 3), in its order: one
/// unit for each value, before its depth is checked; one for each <c>:</c> and each <c>,</c>; the count
/// of each run of white space skipped; a string's whole source length, quotes included, when it
/// closes; and a number's text through <c>StringToNumber</c>.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=High; Resources=3; Fingerprint=3D7898
// Broiler-Falsified-If: a text outside JSON.parse's grammar is read to its end, or the reader recurses so that a deep text exhausts the stack instead of meeting the limit
// Broiler-Human:        PENDING
public ref struct JsonReader
{
    private readonly ReadOnlySpan<char> text;
    private readonly DepthLimit maximumDepth;
    private readonly WorkCharge? charge;
    private bool[] containers;
    private int depth;
    private int position;
    private State state;

    /// <summary>Creates a reader over one JSON text.</summary>
    /// <param name="text">The whole text.</param>
    /// <param name="maximumDepth">The deepest value the caller accepts; the document itself is depth 0.</param>
    /// <param name="charge">The caller's meter, or <see langword="null"/>.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=Medium; Resources=1; Fingerprint=220C7B
    // Broiler-Human:        PENDING
    public JsonReader(ReadOnlySpan<char> text, DepthLimit maximumDepth, WorkCharge? charge = null)
    {
        this.text = text;
        this.maximumDepth = maximumDepth;
        this.charge = charge;
        containers = new bool[16];
        StringValue = string.Empty;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=Low; Resources=1; Fingerprint=15B9F5
    // Broiler-Human:        PENDING
    private enum State
    {
        Start,
        AfterBeginObject,
        AfterBeginArray,
        AfterName,
        AfterValue,
        Ended,
    }

    /// <summary>The token the last successful <see cref="Read"/> found.</summary>
    public JsonToken Token { get; private set; }

    /// <summary>A string token's value or a member's name, its escapes resolved; empty for other tokens.</summary>
    public string StringValue { get; private set; }

    /// <summary>A number token's value; zero for other tokens.</summary>
    public double NumberValue { get; private set; }

    /// <summary>Where the token's source text starts: a string's opening quote, a number's first character.</summary>
    public int TokenStart { get; private set; }

    /// <summary>Where the token's source text ends, exclusive: after a string's closing quote, a number's last digit.</summary>
    public int TokenEnd { get; private set; }

    /// <summary>How many containers enclose the reader's position: the depth the next member or element will have.</summary>
    public readonly int Depth => depth;

    /// <summary>Why the last <see cref="Read"/> failed, when it did.</summary>
    public Failure Failure { get; private set; }

    /// <summary>Reads the next token.</summary>
    /// <returns>
    /// <see cref="ResultStatus.Completed"/> with <see cref="Token"/> set;
    /// <see cref="ResultStatus.Malformed"/> or <see cref="ResultStatus.LimitExceeded"/> with
    /// <see cref="Failure"/> set, after which the reader may not be read again.
    /// </returns>
    /// <exception cref="InvalidOperationException">The reader has already read <see cref="JsonToken.EndOfText"/>, or has failed.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=High; Resources=3; Fingerprint=9C4E6F
    // Broiler-Falsified-If: a text outside the JSON grammar completes, a text inside it fails below the depth limit, a value deeper than the limit is read, or a charge falls other than where Broiler.VM's reader charged it
    // Broiler-Human:        PENDING
    public ResultStatus Read()
    {
        StringValue = string.Empty;
        NumberValue = 0;
        switch (state)
        {
            case State.Start:
                SkipSpace();
                return ReadValue();

            case State.AfterBeginObject:
                SkipSpace();
                if (Peek('}'))
                    return End(JsonToken.EndObject);

                return ReadMember();

            case State.AfterBeginArray:
                SkipSpace();
                if (Peek(']'))
                    return End(JsonToken.EndArray);

                SkipSpace();
                return ReadValue();

            case State.AfterName:
                return ReadValue();

            case State.AfterValue:
                SkipSpace();
                if (depth == 0)
                {
                    if (position != text.Length)
                        return Refuse(ResultStatus.Malformed, position, "json.unexpected");

                    TokenStart = TokenEnd = position;
                    Token = JsonToken.EndOfText;
                    state = State.Ended;
                    return ResultStatus.Completed;
                }

                var inObject = containers[depth - 1];
                if (Peek(','))
                {
                    position++;
                    charge?.Invoke(1);
                    if (inObject)
                        return ReadMember();

                    SkipSpace();
                    return ReadValue();
                }

                if (Peek(inObject ? '}' : ']'))
                    return End(inObject ? JsonToken.EndObject : JsonToken.EndArray);

                return Refuse(ResultStatus.Malformed, position, "json.unexpected");

            default:
                throw new InvalidOperationException("The reader has ended.");
        }
    }

    /// <summary>
    /// Whether <paramref name="text"/> is one JSON text no deeper than <paramref name="maximumDepth"/>:
    /// a <see cref="JsonReader"/> read to its end, building nothing.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="maximumDepth">The deepest value the caller accepts.</param>
    /// <param name="failure">Why it is not, when the status is not <see cref="ResultStatus.Completed"/>.</param>
    /// <param name="charge">The caller's meter, charged as the reader charges; <see langword="null"/> for none.</param>
    /// <returns>The status of the reader's last <see cref="Read"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=High; Resources=2; Fingerprint=FB9241
    // Broiler-Falsified-If: the answer differs from reading the text to its end with a reader
    // Broiler-Human:        PENDING
    public static ResultStatus Validate(ReadOnlySpan<char> text, DepthLimit maximumDepth, out Failure failure, WorkCharge? charge = null)
    {
        var reader = new JsonReader(text, maximumDepth, charge);
        while (true)
        {
            var status = reader.Read();
            if (status != ResultStatus.Completed)
            {
                failure = reader.Failure;
                return status;
            }

            if (reader.Token == JsonToken.EndOfText)
            {
                failure = default;
                return ResultStatus.Completed;
            }
        }
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=Medium; Resources=2; Fingerprint=69F7FF
    // Broiler-Human:        PENDING
    private ResultStatus ReadMember()
    {
        SkipSpace();
        if (!Peek('"'))
            return Refuse(ResultStatus.Malformed, position, "json.unexpected");

        var start = position;
        var status = ReadString(out var name);
        if (status != ResultStatus.Completed)
            return status;

        var end = position;
        SkipSpace();
        if (!Peek(':'))
            return Refuse(ResultStatus.Malformed, position, "json.unexpected");

        position++;
        charge?.Invoke(1);
        SkipSpace();
        Token = JsonToken.PropertyName;
        StringValue = name;
        TokenStart = start;
        TokenEnd = end;
        state = State.AfterName;
        return ResultStatus.Completed;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=High; Resources=3; Fingerprint=06C6F1
    // Broiler-Falsified-If: a value deeper than the limit is read, or the unit a value is charged falls after its depth check
    // Broiler-Human:        PENDING
    private ResultStatus ReadValue()
    {
        charge?.Invoke(1);
        if (!maximumDepth.Admits(depth))
            return Refuse(ResultStatus.LimitExceeded, position, "json.too-deep");

        if (position >= text.Length)
            return Refuse(ResultStatus.Malformed, position, "json.unexpected");

        var start = position;
        switch (text[position])
        {
            case '{':
                return Begin(true);

            case '[':
                return Begin(false);

            case '"':
            {
                var status = ReadString(out var value);
                if (status != ResultStatus.Completed)
                    return status;

                StringValue = value;
                return Leaf(JsonToken.String, start);
            }

            case 't':
                return Keyword("true", JsonToken.True);

            case 'f':
                return Keyword("false", JsonToken.False);

            case 'n':
                return Keyword("null", JsonToken.Null);

            case '-':
            case >= '0' and <= '9':
                return ReadNumber();

            default:
                return Refuse(ResultStatus.Malformed, position, "json.unexpected");
        }
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=Medium; Resources=2; Fingerprint=8BE66C
    // Broiler-Human:        PENDING
    private ResultStatus Begin(bool isObject)
    {
        if (depth == containers.Length)
            Array.Resize(ref containers, containers.Length * 2);

        containers[depth++] = isObject;
        TokenStart = position;
        position++;
        TokenEnd = position;
        Token = isObject ? JsonToken.BeginObject : JsonToken.BeginArray;
        state = isObject ? State.AfterBeginObject : State.AfterBeginArray;
        return ResultStatus.Completed;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=Medium; Resources=1; Fingerprint=AD1724
    // Broiler-Human:        PENDING
    private ResultStatus End(JsonToken token)
    {
        depth--;
        TokenStart = position;
        position++;
        TokenEnd = position;
        Token = token;
        state = State.AfterValue;
        return ResultStatus.Completed;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=Low; Resources=1; Fingerprint=427030
    // Broiler-Human:        PENDING
    private ResultStatus Leaf(JsonToken token, int start)
    {
        Token = token;
        TokenStart = start;
        TokenEnd = position;
        state = State.AfterValue;
        return ResultStatus.Completed;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=Medium; Resources=1; Fingerprint=EFF3F1
    // Broiler-Human:        PENDING
    private ResultStatus Keyword(string word, JsonToken token)
    {
        if (!text[position..].StartsWith(word, StringComparison.Ordinal))
            return Refuse(ResultStatus.Malformed, position, "json.unexpected");

        var start = position;
        position += word.Length;
        return Leaf(token, start);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=High; Resources=2; Fingerprint=C7413D
    // Broiler-Falsified-If: a number outside the JSON grammar is read, or one inside it is not StringToNumber of its text
    // Broiler-Human:        PENDING
    private ResultStatus ReadNumber()
    {
        var start = position;
        if (Peek('-'))
            position++;

        if (!PeekDigit())
            return Refuse(ResultStatus.Malformed, position, "json.unexpected");

        if (Peek('0'))
            position++;
        else
            SkipDigits();

        if (Peek('.'))
        {
            position++;
            if (!PeekDigit())
                return Refuse(ResultStatus.Malformed, position, "json.unexpected");

            SkipDigits();
        }

        if (Peek('e') || Peek('E'))
        {
            position++;
            if (Peek('+') || Peek('-'))
                position++;

            if (!PeekDigit())
                return Refuse(ResultStatus.Malformed, position, "json.unexpected");

            SkipDigits();
        }

        NumberValue = NumberParsing.StringToNumber(text[start..position], charge);
        return Leaf(JsonToken.Number, start);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=High; Resources=3; Fingerprint=EE036E
    // Broiler-Falsified-If: a raw control character or an escape JSON does not define is admitted, or an escape is not resolved to its code unit
    // Broiler-Human:        PENDING
    private ResultStatus ReadString(out string value)
    {
        value = string.Empty;
        var start = position;
        position++;
        var run = position;
        StringBuilder? built = null;
        while (true)
        {
            if (position >= text.Length)
                return Refuse(ResultStatus.Malformed, position, "json.unterminated-string");

            var unit = text[position];
            if (unit == '"')
            {
                if (built is null)
                {
                    value = new string(text[run..position]);
                }
                else
                {
                    built.Append(text[run..position]);
                    value = built.ToString();
                }

                position++;
                charge?.Invoke((ulong)(position - start));
                return ResultStatus.Completed;
            }

            if (unit < ' ')
                return Refuse(ResultStatus.Malformed, position, "json.control-character");

            if (unit != '\\')
            {
                position++;
                continue;
            }

            built ??= new StringBuilder();
            built.Append(text[run..position]);
            position++;
            if (position >= text.Length)
                return Refuse(ResultStatus.Malformed, position, "json.unexpected");

            var escape = text[position];
            position++;
            switch (escape)
            {
                case '"':
                case '\\':
                case '/':
                    built.Append(escape);
                    break;
                case 'b':
                    built.Append('\b');
                    break;
                case 'f':
                    built.Append('\f');
                    break;
                case 'n':
                    built.Append('\n');
                    break;
                case 'r':
                    built.Append('\r');
                    break;
                case 't':
                    built.Append('\t');
                    break;
                case 'u':
                {
                    if (position + 4 > text.Length)
                        return Refuse(ResultStatus.Malformed, position, "json.bad-unicode-escape");

                    var code = 0;
                    for (var step = 0; step < 4; step++)
                    {
                        var digit = UriCodec.HexDigitValue(text[position + step]);
                        if (digit < 0)
                            return Refuse(ResultStatus.Malformed, position + step, "json.bad-unicode-escape");

                        code = (code * 16) + digit;
                    }

                    position += 4;
                    built.Append((char)code);
                    break;
                }

                default:
                    return Refuse(ResultStatus.Malformed, position - 1, "json.bad-escape");
            }

            run = position;
        }
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=Medium; Resources=1; Fingerprint=667BD5
    // Broiler-Human:        PENDING
    private void SkipSpace()
    {
        var start = position;
        while (position < text.Length && text[position] is ' ' or '\t' or '\n' or '\r')
            position++;

        if (position > start)
            charge?.Invoke((ulong)(position - start));
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=Low; Resources=1; Fingerprint=087A09
    // Broiler-Human:        PENDING
    private readonly bool Peek(char expected) => position < text.Length && text[position] == expected;

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=Low; Resources=1; Fingerprint=164CFB
    // Broiler-Human:        PENDING
    private readonly bool PeekDigit() => position < text.Length && text[position] is >= '0' and <= '9';

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=Low; Resources=1; Fingerprint=98A0C7
    // Broiler-Human:        PENDING
    private void SkipDigits()
    {
        while (PeekDigit())
            position++;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.1; IP=Low; Security=Low; Resources=1; Fingerprint=F72717
    // Broiler-Human:        PENDING
    private ResultStatus Refuse(ResultStatus status, int at, string messageId)
    {
        Failure = new Failure(status, at, messageId);
        state = State.Ended;
        return status;
    }
}
