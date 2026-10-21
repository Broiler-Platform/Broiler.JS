using Broiler.JavaScript.Runtime.Unicode;
using Broiler.Unicode.Algorithms;

namespace Broiler.JavaScript.Ast.Tests;

/// <summary>
/// The engine's adapter over Broiler.Unicode's lexical classes (milestone SJL-7.5): its ASCII cache is the
/// owner's answers, read from it, and every other code point is the owner's own answer.
/// </summary>
public sealed class JSUnicodeTests
{
    private static readonly EcmaScriptLexical Owner = new(new UnicodeProperties(JSUnicode.Tables));

    [Fact]
    public void The_ascii_cache_equals_the_owner_on_every_ascii_code_point()
    {
        for (var codePoint = 0; codePoint < 0x80; codePoint++)
        {
            Assert.Equal(Owner.IsIdentifierStart(codePoint), JSUnicode.IsIdentifierStart(codePoint));
            Assert.Equal(Owner.IsIdentifierPart(codePoint), JSUnicode.IsIdentifierPart(codePoint));
            Assert.Equal(Owner.IsWhiteSpace(codePoint), JSUnicode.IsWhiteSpace(codePoint));
            Assert.Equal(EcmaScriptLexical.IsLineTerminator(codePoint), JSUnicode.IsLineTerminator(codePoint));
        }
    }

    [Fact]
    public void Every_code_point_is_answered_as_the_owner_answers_it()
    {
        for (var codePoint = -1; codePoint <= 0x110000; codePoint++)
        {
            if (JSUnicode.IsIdentifierStart(codePoint) != Owner.IsIdentifierStart(codePoint)
                || JSUnicode.IsIdentifierPart(codePoint) != Owner.IsIdentifierPart(codePoint)
                || JSUnicode.IsWhiteSpace(codePoint) != Owner.IsWhiteSpace(codePoint))
            {
                Assert.Fail($"U+{codePoint:X4} is answered otherwise than the owner answers it");
            }
        }
    }

    [Theory]
    [InlineData('$', true, true)]
    [InlineData('_', true, true)]
    [InlineData('a', true, true)]
    [InlineData('0', false, true)]
    [InlineData(0x2E2F, false, false)] // Lm and Pattern_Syntax (correspondence row I1)
    [InlineData(0x0870, true, true)]
    [InlineData(0x16D40, true, true)]
    [InlineData(0x11B60, false, true)]
    [InlineData(0x200C, false, true)]
    [InlineData(0xD800, false, false)]
    public void Identifier_classes(int codePoint, bool start, bool part)
    {
        Assert.Equal(start, JSUnicode.IsIdentifierStart(codePoint));
        Assert.Equal(part, JSUnicode.IsIdentifierPart(codePoint));
    }

    [Theory]
    [InlineData(0x09, true)]
    [InlineData(0x0B, true)]
    [InlineData(0x0C, true)]
    [InlineData(0x20, true)]
    [InlineData(0xA0, true)]
    [InlineData(0xFEFF, true)]
    [InlineData(0x3000, true)]
    [InlineData(0x0085, false)] // correspondence row W1
    [InlineData(0x180E, false)]
    [InlineData(0x0A, false)]
    [InlineData(0x2028, false)]
    public void White_space(int codePoint, bool expected) => Assert.Equal(expected, JSUnicode.IsWhiteSpace(codePoint));
}
