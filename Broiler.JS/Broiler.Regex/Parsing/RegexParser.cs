using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Broiler.Regex.Ast;
using Broiler.Regex.Unicode;

namespace Broiler.Regex.Parsing;

/// <summary>
/// Recursive-descent parser for the ECMAScript regular-expression
/// <c>Pattern</c> grammar (ECMA-262 §22.2.1). Produces a <see cref="RegexNode"/>
/// tree consumed by the matcher.
/// </summary>
public sealed class RegexParser
{
    private readonly string _src;
    private readonly bool _unicode;
    private int _pos;
    private int _captureCount;

    /// <summary>Total number of capturing groups, available after parsing.</summary>
    public int CaptureCount { get; private set; }

    /// <summary>Map of group name → 1-based capture index, available after parsing.</summary>
    public IReadOnlyDictionary<string, int> GroupNames => _groupNames;

    private readonly Dictionary<string, int> _groupNames = new();
    private readonly Dictionary<string, int> _declaredNames = new();
    private int _totalCaptureGroups;

    public RegexParser(string pattern, RegexFlags flags)
    {
        _src = pattern ?? "";
        _unicode = flags.IsUnicodeMode();
    }

    public static RegexNode Parse(string pattern, RegexFlags flags, out int captureCount,
        out IReadOnlyDictionary<string, int> groupNames)
    {
        var parser = new RegexParser(pattern, flags);
        var node = parser.ParsePattern();
        captureCount = parser.CaptureCount;
        groupNames = parser.GroupNames;
        return node;
    }

    private RegexNode ParsePattern()
    {
        // Pre-scan to learn the total capture-group count and the declared group
        // names. ECMAScript needs both up front: in Unicode mode `\1` is always a
        // back-reference (its validity depends on the total count), and a forward
        // `\k<name>` must resolve to a name declared later in the pattern.
        PreScan();

        var node = ParseDisjunction();
        if (_pos != _src.Length)
            throw new RegexSyntaxException($"Unexpected '{_src[_pos]}' in pattern", _pos);

        CaptureCount = _captureCount;
        return node;
    }

    private void PreScan()
    {
        var inClass = false;
        for (var i = 0; i < _src.Length; i++)
        {
            var c = _src[i];
            if (c == '\\')
            {
                i++; // skip escaped char
                continue;
            }
            if (c == '[') { inClass = true; continue; }
            if (c == ']') { inClass = false; continue; }
            if (inClass || c != '(')
                continue;

            // '(' — capturing unless it begins a (?: (?= (?! (?<= (?<! (?flags …) group.
            if (i + 1 < _src.Length && _src[i + 1] == '?')
            {
                // (?<name>…) is capturing+named; (?<=…) / (?<!…) are not.
                if (i + 2 < _src.Length && _src[i + 2] == '<'
                    && i + 3 < _src.Length && _src[i + 3] != '=' && _src[i + 3] != '!')
                {
                    var end = _src.IndexOf('>', i + 3);
                    if (end < 0)
                        throw new RegexSyntaxException("Unterminated group name", i);
                    var name = _src.Substring(i + 3, end - (i + 3));
                    _totalCaptureGroups++;
                    if (!_declaredNames.TryAdd(name, _totalCaptureGroups))
                        throw new RegexSyntaxException($"Duplicate capture group name '{name}'", i);
                }
                continue;
            }

            _totalCaptureGroups++;
        }
    }

    // ----- Disjunction / Alternative -----------------------------------------

    private RegexNode ParseDisjunction()
    {
        var alternatives = new List<RegexNode> { ParseAlternative() };
        while (Peek() == '|')
        {
            _pos++;
            alternatives.Add(ParseAlternative());
        }

        return alternatives.Count == 1 ? alternatives[0] : new DisjunctionNode(alternatives);
    }

    private RegexNode ParseAlternative()
    {
        var terms = new List<RegexNode>();
        while (true)
        {
            var c = Peek();
            if (c is '\0' or '|' or ')')
                break;
            terms.Add(ParseTerm());
        }

        return terms.Count switch
        {
            0 => EmptyNode.Instance,
            1 => terms[0],
            _ => new SequenceNode(terms),
        };
    }

    // ----- Term (Atom + optional Quantifier, or an Assertion) ----------------

    private RegexNode ParseTerm()
    {
        var assertion = TryParseAssertion();
        if (assertion != null)
            return assertion;

        var atom = ParseAtom();
        return TryApplyQuantifier(atom);
    }

    private RegexNode? TryParseAssertion()
    {
        var c = Peek();
        switch (c)
        {
            case '^': _pos++; return new AnchorNode(AnchorKind.StartOfInput);
            case '$': _pos++; return new AnchorNode(AnchorKind.EndOfInput);
        }

        if (c == '\\' && PeekAt(1) is 'b' or 'B')
        {
            var kind = PeekAt(1) == 'b' ? AnchorKind.WordBoundary : AnchorKind.NonWordBoundary;
            _pos += 2;
            return new AnchorNode(kind);
        }

        // Look-around groups (?= (?! (?<= (?<!  — these are assertions, not atoms,
        // and a quantifier on them is a syntax error in Unicode mode.
        if (c == '(' && PeekAt(1) == '?')
        {
            var third = PeekAt(2);
            if (third is '=' or '!')
            {
                _pos += 3;
                var body = ParseDisjunction();
                Expect(')');
                return new LookaroundNode(body, behind: false, negative: third == '!');
            }
            if (third == '<' && PeekAt(3) is '=' or '!')
            {
                var negative = PeekAt(3) == '!';
                _pos += 4;
                var body = ParseDisjunction();
                Expect(')');
                return new LookaroundNode(body, behind: true, negative: negative);
            }
        }

        return null;
    }

    private RegexNode TryApplyQuantifier(RegexNode atom)
    {
        var c = Peek();
        int min, max;
        switch (c)
        {
            case '*': min = 0; max = QuantifierNode.Unbounded; _pos++; break;
            case '+': min = 1; max = QuantifierNode.Unbounded; _pos++; break;
            case '?': min = 0; max = 1; _pos++; break;
            case '{':
                if (!TryParseBraceQuantifier(out min, out max))
                    return atom; // a literal '{' that is not a valid quantifier
                break;
            default:
                return atom;
        }

        var greedy = true;
        if (Peek() == '?')
        {
            greedy = false;
            _pos++;
        }

        return new QuantifierNode(atom, min, max, greedy);
    }

    private bool TryParseBraceQuantifier(out int min, out int max)
    {
        min = max = 0;
        var save = _pos;
        _pos++; // consume '{'

        if (!TryReadDecimal(out min))
        {
            _pos = save;
            return false;
        }

        max = min;
        if (Peek() == ',')
        {
            _pos++;
            if (Peek() == '}')
            {
                max = QuantifierNode.Unbounded;
            }
            else if (!TryReadDecimal(out max))
            {
                _pos = save;
                return false;
            }
        }

        if (Peek() != '}')
        {
            _pos = save;
            return false;
        }
        _pos++; // consume '}'

        if (max != QuantifierNode.Unbounded && max < min)
            throw new RegexSyntaxException("Quantifier {n,m} with m < n", save);

        return true;
    }

    // ----- Atom ---------------------------------------------------------------

    private RegexNode ParseAtom()
    {
        var c = Peek();
        switch (c)
        {
            case '.':
                _pos++;
                return AnyCharNode.Instance;
            case '(':
                return ParseGroup();
            case '[':
                return new CharClassNode(ParseCharacterClass());
            case '\\':
                return ParseAtomEscape();
            case ')':
            case '|':
            case '\0':
                throw new RegexSyntaxException("Unexpected end of atom", _pos);
            case '*':
            case '+':
            case '?':
                throw new RegexSyntaxException($"Nothing to repeat before '{c}'", _pos);
        }

        // PatternCharacter — a single source character (or astral code point in u-mode).
        var cp = ReadSourceCodePoint();
        return new CharNode(cp);
    }

    private RegexNode ParseGroup()
    {
        _pos++; // consume '('
        if (Peek() == '?')
        {
            var second = PeekAt(1);

            // Non-capturing (?:…)
            if (second == ':')
            {
                _pos += 2;
                var body = ParseDisjunction();
                Expect(')');
                return new GroupNode(body, captureIndex: 0, name: null);
            }

            // Named capturing (?<name>…)
            if (second == '<' && PeekAt(2) != '=' && PeekAt(2) != '!')
            {
                _pos += 2; // consume '?<'
                var name = ReadGroupName();
                var index = ++_captureCount;
                _groupNames[name] = index;
                var body = ParseDisjunction();
                Expect(')');
                return new GroupNode(body, index, name);
            }

            // Inline modifier group (?ims-ims:…)
            if (second is 'i' or 'm' or 's' or '-')
                return ParseModifierGroup();

            throw new RegexSyntaxException("Invalid group", _pos);
        }

        // Plain capturing group
        var captureIndex = ++_captureCount;
        var child = ParseDisjunction();
        Expect(')');
        return new GroupNode(child, captureIndex, null);
    }

    private RegexNode ParseModifierGroup()
    {
        _pos++; // consume '?'
        var added = ReadModifierFlags();
        var removed = RegexFlags.None;
        if (Peek() == '-')
        {
            _pos++;
            removed = ReadModifierFlags();
        }

        if (added == RegexFlags.None && removed == RegexFlags.None)
            throw new RegexSyntaxException("Modifier group must add or remove at least one flag", _pos);
        if ((added & removed) != 0)
            throw new RegexSyntaxException("A modifier flag may not be both added and removed", _pos);

        Expect(':');
        var body = ParseDisjunction();
        Expect(')');
        return new ModifierGroupNode(body, added, removed);
    }

    private RegexFlags ReadModifierFlags()
    {
        var flags = RegexFlags.None;
        while (true)
        {
            var flag = Peek() switch
            {
                'i' => RegexFlags.IgnoreCase,
                'm' => RegexFlags.Multiline,
                's' => RegexFlags.DotAll,
                _ => RegexFlags.None,
            };
            if (flag == RegexFlags.None)
                break;
            if ((flags & flag) != 0)
                throw new RegexSyntaxException("Repeated flag in modifier group", _pos);
            flags |= flag;
            _pos++;
        }
        return flags;
    }

    // ----- AtomEscape (outside a character class) -----------------------------

    private RegexNode ParseAtomEscape()
    {
        _pos++; // consume '\'
        var c = Peek();

        // Decimal back-reference \1 .. \n
        if (c is >= '1' and <= '9')
        {
            var start = _pos;
            TryReadDecimal(out var num);
            if (num <= _totalCaptureGroups)
                return new BackreferenceNode(num);
            // In Unicode mode an over-large numeric escape is a syntax error;
            // in non-Unicode it is a legacy octal/identity escape.
            if (_unicode)
                throw new RegexSyntaxException($"Back-reference \\{num} to a non-existent group", start);
            _pos = start;
            return new CharNode(ReadLegacyOctalOrIdentity());
        }

        // Named back-reference \k<name>
        if (c == 'k')
        {
            _pos++;
            if (Peek() != '<')
                throw new RegexSyntaxException("Expected '<' after \\k", _pos);
            _pos++;
            var name = ReadGroupName();
            return new BackreferenceNode(name);
        }

        // Class escapes that are valid as standalone atoms.
        var classEscape = TryReadClassEscape();
        if (classEscape.HasValue)
        {
            var set = new CharSet();
            set.AddEscape(classEscape.Value);
            return new CharClassNode(set);
        }

        // Unicode property escape \p{…} / \P{…}
        if (c is 'p' or 'P' && _unicode)
            return new CharClassNode(ParsePropertyEscape());

        // Otherwise a single escaped code point (\n, \xHH, \uHHHH, \u{…}, identity).
        return new CharNode(ReadCharacterEscape());
    }

    // ----- Character class [ ... ] -------------------------------------------

    private CharSet ParseCharacterClass()
    {
        _pos++; // consume '['
        var set = new CharSet();
        if (Peek() == '^')
        {
            set.Negated = true;
            _pos++;
        }

        while (true)
        {
            var c = Peek();
            if (c == '\0')
                throw new RegexSyntaxException("Unterminated character class", _pos);
            if (c == ']')
            {
                _pos++;
                return set;
            }

            // v-mode set operators are recognised but not yet evaluated.
            if (c is '&' or '-' && PeekAt(1) == c)
            {
                set.UsesSetOperations = true;
                _pos += 2;
                continue;
            }

            var first = ReadClassAtom(set, out var firstIsLiteral);

            // A range "a-z": only when '-' is followed by another class atom (not
            // the closing ']') and both ends are literal code points.
            if (firstIsLiteral && Peek() == '-' && PeekAt(1) != ']' && PeekAt(1) != '\0')
            {
                _pos++; // consume '-'
                var second = ReadClassAtom(set, out var secondIsLiteral);
                if (firstIsLiteral && secondIsLiteral)
                    set.AddRange(first, second);
                else
                {
                    // One side was a class escape (\d etc.) — '-' is then a literal.
                    set.AddCodePoint(first);
                    set.AddCodePoint('-');
                    set.AddCodePoint(second);
                }
            }
            else if (firstIsLiteral)
            {
                set.AddCodePoint(first);
            }
        }
    }

    /// <summary>
    /// Reads one class atom. Adds class escapes (\d…) directly to <paramref name="set"/>
    /// and returns their sentinel as a non-literal; returns a literal code point otherwise.
    /// </summary>
    private int ReadClassAtom(CharSet set, out bool isLiteral)
    {
        var c = Peek();
        if (c == '\\')
        {
            _pos++;
            var classEscape = TryReadClassEscape();
            if (classEscape.HasValue)
            {
                set.AddEscape(classEscape.Value);
                isLiteral = false;
                return -1;
            }
            if (Peek() is 'p' or 'P' && _unicode)
            {
                var prop = ParsePropertyEscape();
                foreach (var (lo, hi) in prop.Ranges)
                    set.AddRange(lo, hi);
                foreach (var esc in prop.Escapes)
                    set.AddEscape(esc);
                isLiteral = false;
                return -1;
            }
            if (Peek() == 'b') { _pos++; isLiteral = true; return '\b'; } // \b is backspace in a class
            isLiteral = true;
            return ReadCharacterEscape();
        }

        isLiteral = true;
        return ReadSourceCodePoint();
    }

    // ----- Shared escape readers ---------------------------------------------

    private ClassEscape? TryReadClassEscape()
    {
        switch (Peek())
        {
            case 'd': _pos++; return ClassEscape.Digit;
            case 'D': _pos++; return ClassEscape.NonDigit;
            case 'w': _pos++; return ClassEscape.Word;
            case 'W': _pos++; return ClassEscape.NonWord;
            case 's': _pos++; return ClassEscape.Space;
            case 'S': _pos++; return ClassEscape.NonSpace;
            default: return null;
        }
    }

    /// <summary>Reads a CharacterEscape (the '\' already consumed): \n \r \xHH \uHHHH \u{…} \cX \0 identity.</summary>
    private int ReadCharacterEscape()
    {
        var c = Peek();
        switch (c)
        {
            case 'n': _pos++; return '\n';
            case 'r': _pos++; return '\r';
            case 't': _pos++; return '\t';
            case 'f': _pos++; return '\f';
            case 'v': _pos++; return '\v';
            case '0' when !char.IsDigit(PeekAt(1)): _pos++; return 0;
            case 'x': return ReadHexEscape(2);
            case 'u': return ReadUnicodeEscape();
            case 'c': return ReadControlEscape();
        }

        // IdentityEscape: in Unicode mode only syntax characters and '/' may be
        // escaped; outside Unicode mode any non-IdentifierPart char is allowed.
        if (_unicode && !IsSyntaxChar(c) && c != '/')
            throw new RegexSyntaxException($"Invalid escape \\{c} in Unicode mode", _pos);

        return ReadSourceCodePoint();
    }

    private int ReadLegacyOctalOrIdentity()
    {
        // Legacy octal escape \ooo (non-Unicode only); fall back to identity.
        var c = Peek();
        if (c is >= '0' and <= '7')
        {
            var value = 0;
            var count = 0;
            while (count < 3 && Peek() is >= '0' and <= '7')
            {
                var next = value * 8 + (Peek() - '0');
                if (next > 0xFF) break;
                value = next;
                _pos++;
                count++;
            }
            return value;
        }
        return ReadSourceCodePoint();
    }

    private int ReadControlEscape()
    {
        _pos++; // consume 'c'
        var c = Peek();
        if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
        {
            _pos++;
            return c % 32;
        }
        // Not a valid control letter: treat '\c' as a literal backslash-c in legacy mode.
        if (_unicode)
            throw new RegexSyntaxException("Invalid \\c escape", _pos);
        return '\\';
    }

    private int ReadHexEscape(int digits)
    {
        _pos++; // consume 'x' or 'u'
        var value = 0;
        for (var i = 0; i < digits; i++)
        {
            var d = HexValue(Peek());
            if (d < 0)
                throw new RegexSyntaxException("Invalid hexadecimal escape", _pos);
            value = value * 16 + d;
            _pos++;
        }
        return value;
    }

    private int ReadUnicodeEscape()
    {
        // Caller is positioned at 'u'.
        if (PeekAt(1) == '{')
        {
            _pos += 2; // consume 'u{'
            var value = 0;
            var any = false;
            while (HexValue(Peek()) >= 0)
            {
                value = value * 16 + HexValue(Peek());
                if (value > 0x10FFFF)
                    throw new RegexSyntaxException("Unicode code point out of range", _pos);
                _pos++;
                any = true;
            }
            if (!any || Peek() != '}')
                throw new RegexSyntaxException("Invalid \\u{…} escape", _pos);
            _pos++; // consume '}'
            return value;
        }

        var hi = ReadHexEscape(4);

        // In Unicode mode, a \uHHHH high surrogate followed by a \uHHHH low
        // surrogate is a single astral code point.
        if (_unicode && hi >= 0xD800 && hi <= 0xDBFF && Peek() == '\\' && PeekAt(1) == 'u')
        {
            var save = _pos;
            _pos++; // consume '\'
            var lo = ReadHexEscape(4);
            if (lo >= 0xDC00 && lo <= 0xDFFF)
                return char.ConvertToUtf32((char)hi, (char)lo);
            _pos = save; // not a low surrogate; leave it for the next atom
        }

        return hi;
    }

    private CharSet ParsePropertyEscape()
    {
        var negated = Peek() == 'P';
        _pos++; // consume 'p'/'P'
        if (Peek() != '{')
            throw new RegexSyntaxException("Expected '{' after \\p", _pos);
        _pos++;
        var sb = new StringBuilder();
        while (Peek() != '}' && Peek() != '\0')
            sb.Append(_src[_pos++]);
        if (Peek() != '}')
            throw new RegexSyntaxException("Unterminated \\p{…}", _pos);
        _pos++;

        var spec = sb.ToString();
        string name;
        string? value = null;
        var eq = spec.IndexOf('=');
        if (eq >= 0)
        {
            name = spec.Substring(0, eq);
            value = spec.Substring(eq + 1);
        }
        else
        {
            name = spec;
        }

        // Resolution is currently a stub (throws NotSupportedException). We still
        // build the node so callers see a clear, located error rather than a
        // mis-match. See UnicodeCharSets.ResolveProperty.
        _ = UnicodeCharSets.ResolveProperty(name, value);
        var set = new CharSet { Negated = negated };
        return set;
    }

    private string ReadGroupName()
    {
        var sb = new StringBuilder();
        while (Peek() != '>' && Peek() != '\0')
        {
            // RegExpIdentifierName allows \u escapes; decode them so the stored
            // name matches a JS string key.
            if (Peek() == '\\' && PeekAt(1) == 'u')
            {
                _pos++; // consume '\'
                sb.Append(char.ConvertFromUtf32(ReadUnicodeEscape()));
                continue;
            }
            sb.Append(_src[_pos++]);
        }
        if (Peek() != '>')
            throw new RegexSyntaxException("Unterminated group name", _pos);
        _pos++; // consume '>'
        var name = sb.ToString();
        if (name.Length == 0)
            throw new RegexSyntaxException("Empty group name", _pos);
        return name;
    }

    // ----- Low-level cursor helpers ------------------------------------------

    private char Peek() => _pos < _src.Length ? _src[_pos] : '\0';
    private char PeekAt(int offset) => _pos + offset < _src.Length ? _src[_pos + offset] : '\0';

    private void Expect(char c)
    {
        if (Peek() != c)
            throw new RegexSyntaxException($"Expected '{c}'", _pos);
        _pos++;
    }

    /// <summary>Reads one source code point, combining a surrogate pair in Unicode mode.</summary>
    private int ReadSourceCodePoint()
    {
        var c = _src[_pos++];
        if (_unicode && char.IsHighSurrogate(c) && _pos < _src.Length && char.IsLowSurrogate(_src[_pos]))
        {
            var lo = _src[_pos++];
            return char.ConvertToUtf32(c, lo);
        }
        return c;
    }

    private bool TryReadDecimal(out int value)
    {
        value = 0;
        if (!char.IsAsciiDigit(Peek()))
            return false;
        long acc = 0;
        while (char.IsAsciiDigit(Peek()))
        {
            acc = acc * 10 + (Peek() - '0');
            if (acc > int.MaxValue)
                acc = int.MaxValue;
            _pos++;
        }
        value = (int)acc;
        return true;
    }

    private static int HexValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };

    private static bool IsSyntaxChar(char c)
        => c is '^' or '$' or '\\' or '.' or '*' or '+' or '?' or '(' or ')'
            or '[' or ']' or '{' or '}' or '|';
}
