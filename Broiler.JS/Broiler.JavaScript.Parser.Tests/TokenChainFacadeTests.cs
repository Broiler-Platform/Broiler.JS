using System.Collections.Generic;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Parser;

namespace Broiler.JavaScript.Parser.Tests;

/// <summary>
/// Window W2 of milestone SJL-14 (bundle <c>sjl-14-001</c>; decision D50, <c>facade-obsolete</c>): <see cref="FastTokenStream"/>
/// and <see cref="FastScanner"/> are facades over the bridge's token chain, the shared syntax library's tokens made into
/// this engine's <see cref="FastToken"/>s. These read the chain as a consumer of the facades does (added in review of W2,
/// which found them untested): the kinds, cooked values and flags, the line-terminator tokens, a refusal at the first read,
/// and the scanner's states.
/// </summary>
public class TokenChainFacadeTests
{
    private static List<FastToken> Tokens(string source)
    {
        var tokens = new List<FastToken>();
        for (var token = new FastTokenStream(new StringSpan(source)).Current; ; token = token.Next)
        {
            tokens.Add(token);
            if (token.Type == TokenTypes.EOF)
                return tokens;
        }
    }

    private static TokenTypes[] Types(string source) => Tokens(source).ConvertAll(t => t.Type).ToArray();

    [Fact]
    public void ALineTerminatorBeforeTheEndOfTheTextIsAToken()
        => Assert.Equal(new[] { TokenTypes.Identifier, TokenTypes.LineTerminator, TokenTypes.EOF }, Types("a\n"));

    [Fact]
    public void ALineTerminatorBetweenTwoTokensIsOneToken_CrLfOne()
    {
        var tokens = Tokens("a = 1;\r\n\r\nb");
        Assert.Equal(
            new[] { TokenTypes.Identifier, TokenTypes.Assign, TokenTypes.Number, TokenTypes.SemiColon, TokenTypes.LineTerminator,
             TokenTypes.Identifier, TokenTypes.EOF },
            tokens.ConvertAll(t => t.Type).ToArray());
        Assert.Equal("\r\n", tokens[4].Span.Value);
        Assert.Equal(3, tokens[5].Start.Line);
        Assert.Equal(1.0, tokens[2].Number);
    }

    [Fact]
    public void NoLineTerminatorTokenStandsWhereNoLineTerminatorDoes()
        => Assert.Equal(new[] { TokenTypes.Identifier, TokenTypes.Plus, TokenTypes.Identifier, TokenTypes.EOF }, Types("a /* c */ + b // d"));

    [Fact]
    public void ATemplateIsItsHeadMiddleAndTailWithTheirCookedValues()
    {
        var tokens = Tokens("x = `a${b}c${d}e`");
        Assert.Equal(
            new[] { TokenTypes.Identifier, TokenTypes.Assign, TokenTypes.TemplateBegin, TokenTypes.Identifier, TokenTypes.TemplatePart,
             TokenTypes.Identifier, TokenTypes.TemplateEnd, TokenTypes.EOF },
            tokens.ConvertAll(t => t.Type).ToArray());
        Assert.Equal(new[] { "a", "c", "e" }, new[] { tokens[2].CookedText, tokens[4].CookedText, tokens[6].CookedText });
    }

    [Fact]
    public void ARegularExpressionLiteralCarriesItsBodyAndFlags()
    {
        var tokens = Tokens("/ab+/gi.test(s)");
        Assert.Equal(
            new[] { TokenTypes.RegExLiteral, TokenTypes.Dot, TokenTypes.Identifier, TokenTypes.BracketStart, TokenTypes.Identifier,
             TokenTypes.BracketEnd, TokenTypes.EOF },
            tokens.ConvertAll(t => t.Type).ToArray());
        Assert.Equal("ab+", tokens[0].CookedText);
        Assert.Equal("gi", tokens[0].Flags);
    }

    [Fact]
    public void ADivisionIsADivision()
        => Assert.Equal(new[] { TokenTypes.Identifier, TokenTypes.Divide, TokenTypes.Identifier, TokenTypes.Divide, TokenTypes.Identifier, TokenTypes.EOF },
            Types("a / b / c"));

    [Fact]
    public void APrivateNameIsAHashAndAName()
    {
        var tokens = Tokens("class C { #p = 1; m() { return this.#p; } }");
        var names = new List<string>();
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Type == TokenTypes.Hash)
            {
                Assert.Equal(TokenTypes.Identifier, tokens[i + 1].Type);
                names.Add(tokens[i + 1].Span.Value);
            }
        }

        Assert.Equal(new[] { "p", "p" }, names.ToArray());
    }

    [Fact]
    public void ABigIntAndADecimalLiteralHaveTheirOwnKinds()
        => Assert.Equal(new[] { TokenTypes.BigInt, TokenTypes.Plus, TokenTypes.Decimal, TokenTypes.EOF }, Types("1n + 2m"));

    [Fact]
    public void ANumbersValueIsRead()
        => Assert.Equal(16.0, Tokens("0x10")[0].Number);

    [Fact]
    public void ATextTheLibraryRefusesThrowsAtTheFirstRead()
    {
        var stream = new FastTokenStream(new StringSpan("a +"));
        Assert.Throws<FastParseException>(() => stream.Current);
    }

    [Fact]
    public void ExpectAndConsumeWalkTheChain()
    {
        var stream = new FastTokenStream(new StringSpan("a = 1;"));
        Assert.Equal("a", stream.Expect(TokenTypes.Identifier).Span.Value);
        Assert.Equal(TokenTypes.Assign, stream.Current.Type);
        Assert.Equal(TokenTypes.Number, stream.Consume().Type);
        var error = Assert.Throws<FastParseException>(() => stream.Expect(TokenTypes.String));
        Assert.Equal("Expecting String at 1, 5", error.Message);
    }

    [Fact]
    public void ExpectSkipsLineTerminators()
    {
        var stream = new FastTokenStream(new StringSpan("a\n\nb"));
        stream.Expect(TokenTypes.Identifier);
        Assert.Equal(TokenTypes.LineTerminator, stream.Current.Type);
        Assert.Equal("b", stream.Expect(TokenTypes.Identifier).Span.Value);
        Assert.Equal(TokenTypes.EOF, stream.Current.Type);
        Assert.Equal(TokenTypes.EOF, stream.Consume().Type);
    }

    [Fact]
    public void TheScannersStateCommitsTheTextScannedAndReturnsTheScannerWhenDisposed()
    {
        var scanner = new FastScanner(new FastPool(), new StringSpan("ab + cd"));
        Assert.Equal("ab", scanner.Token.Span.Value);
        var state = scanner.Push();
        scanner.ConsumeToken();
        Assert.Equal("+", scanner.Token.Span.Value);
        Assert.Equal("ab ", state.Scanned().ToString());
        var committed = state.Commit(TokenTypes.Identifier);
        Assert.Equal("ab ", committed.Span.Value);
        Assert.Equal(TokenTypes.Identifier, committed.Type);
        state.Dispose();
        Assert.Equal("ab", scanner.Token.Span.Value);
        scanner.ConsumeToken();
        scanner.ConsumeToken();
        Assert.Equal("cd", scanner.Token.Span.Value);
        scanner.ConsumeToken();
        Assert.Equal(TokenTypes.EOF, scanner.Token.Type);
        scanner.ConsumeToken();
        Assert.Equal(TokenTypes.EOF, scanner.Token.Type);
    }

    [Fact]
    public void TheScannerOverARefusedTextThrowsAtTheFirstRead()
    {
        var scanner = new FastScanner(new FastPool(), new StringSpan("a +"));
        Assert.Throws<FastParseException>(() => scanner.Token);
    }
}
