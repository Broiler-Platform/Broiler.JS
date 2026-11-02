using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>
/// The limits on the stack the library documents (stage SJL-13.2e; Broiler.VM's JSD-0083, decision D14; the home rule's
/// part G): the deep-nesting corpus, every source ending as its row expects; a chain whose left operand is a parenthesised
/// chain bounded before the stack is (the left-operand gap, class L); and each construct the parser recurses through,
/// accepted at the library's supported maximum nesting and refused one level past it, on a thread of
/// <see cref="ParseLimits.StackBytes"/> - not on the large threads the parser's own tests take.
/// </summary>
/// <remarks>
/// The equality decision D14 asks for, <see cref="ParseLimits.MaximumTreeDepth"/> against Broiler.VM's
/// <c>SliceParseOptions.MaximumTreeDepth</c>, is Broiler.VM's test to hold, at its step 1, when it references the library:
/// this repository references nothing of Broiler.VM (rule SL1). The constants' values are pinned here.
/// </remarks>
public sealed class LimitsTests
{
    private static string RepositoryRoot => Runners.UnicodeDataHome.Root(AppContext.BaseDirectory);

    private static T OnDocumentedStack<T>(Func<T> work)
    {
        T result = default!;
        Exception? failure = null;
        var thread = new Thread(
            () =>
            {
                try
                {
                    result = work();
                }
                catch (Exception e)
                {
                    failure = e;
                }
            },
            ParseLimits.StackBytes);
        thread.Start();
        thread.Join();
        if (failure is not null)
            throw failure;
        return result;
    }

    private static ParseResult ParseV(string text, int nesting) => OnDocumentedStack(() => SyntaxParser.Parse(
        new ParseRequest(new SourceText(text), ParseGoal.Script, Runners.UnicodeDataHome.Tables, NoValidator.Instance)
        {
            Limits = new ParseLimits { NestingDepth = new DepthLimit(nesting) },
        }));

    [Fact]
    public void The_constants_are_the_derivation_s()
    {
        // Broiler.VM's bound, derived as JSD-0022 derived it (design.md's note of 2026-11-02 at SJL-13.2e, and
        // docs/frontend/limits-derivation.md): the library's parse needs no stack per level of tree depth.
        Assert.Equal(10_000, ParseLimits.MaximumTreeDepth);

        // Broiler.VM's supported maximum, confirmed by the measurement, not raised.
        Assert.Equal(512, ParseLimits.MaximumSupportedNestingDepth);

        // The stack Broiler.VM's CompilationStack declares (CompilationStack.CompileStackBytes at Broiler.VM 013e385).
        Assert.Equal(16 * 1024 * 1024, ParseLimits.StackBytes);
    }

    /// <summary>The home rule's part G population, in this process: each row's outcome and diagnostic, and its source's digest.</summary>
    [Fact]
    public void Every_source_of_the_deep_nesting_corpus_ends_as_its_row_expects()
    {
        var folder = Path.Combine(RepositoryRoot, "docs", "evidence", "sjl-13-001", "inputs", "deep");
        var lines = File.ReadAllLines(Path.Combine(folder, "manifest.tsv"));
        var bound = lines.Single(static l => l.StartsWith("#% tree-depth-bound\t", StringComparison.Ordinal)).Split('\t')[1];
        Assert.Equal(ParseLimits.MaximumTreeDepth.ToString(CultureInfo.InvariantCulture), bound);
        var rows = lines.Where(static l => l.Length > 0 && l[0] == 'G').Select(static l => l.Split('\t')).ToList();
        Assert.Equal(54, rows.Count);
        foreach (var row in rows)
        {
            var (id, file, kind, nesting, expect, sha) = (row[0], row[1], row[3], int.Parse(row[5], CultureInfo.InvariantCulture), row[8], row[9]);
            var bytes = File.ReadAllBytes(Path.Combine(folder, file));
            Assert.Equal(sha, Convert.ToHexStringLower(SHA256.HashData(bytes)));
            var result = ParseV(Encoding.UTF8.GetString(bytes), nesting);
            if (expect == "accepted")
            {
                Assert.True(result.Outcome == ParseOutcome.Accepted, id + " " + file + " was refused: " + (result.Diagnostics.Count > 0 ? result.Diagnostics[0].Code : "-"));
                continue;
            }

            Assert.True(result.Outcome == ParseOutcome.Refused, id + " " + file + " was accepted");
            var first = result.Diagnostics[0];
            var (code, argument) = kind == "nesting"
                ? ("JSE9201", nesting.ToString(CultureInfo.InvariantCulture))
                : ("JSE9202", ParseLimits.MaximumTreeDepth.ToString(CultureInfo.InvariantCulture));
            Assert.True((code, argument) == (first.Code, first.Arguments[0]), id + " " + file + " gave " + first.Code + " " + string.Join(",", first.Arguments));
        }
    }

    /// <summary>
    /// The left-operand gap closed (decision D14, class L): a binary chain whose left operand is a parenthesised chain is
    /// charged the left operand's spine and its own nodes together, however the bound's nodes fall between the two, and at
    /// every level of parentheses; one node past the bound is refused with the tree-depth diagnostic before the stack is
    /// reached, and far past it as soon as the bound is passed.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2_500)]
    [InlineData(5_000)]
    [InlineData(9_999)]
    public void A_chain_whose_left_operand_is_a_parenthesised_chain_is_bounded_before_the_stack_is(int inside)
    {
        string Text(int outside) => "x = (a" + string.Concat(Enumerable.Repeat("+a", inside)) + ")" + string.Concat(Enumerable.Repeat("+b", outside)) + ";";
        Assert.Equal(ParseOutcome.Accepted, ParseV(Text(ParseLimits.MaximumTreeDepth - inside), 64).Outcome);
        var refused = ParseV(Text(ParseLimits.MaximumTreeDepth - inside + 1), 64);
        Assert.Equal(ParseOutcome.Refused, refused.Outcome);
        Assert.Equal(("JSE9202", "limit.tree.too-deep", "10000"), (refused.Diagnostics[0].Code, refused.Diagnostics[0].MessageId, refused.Diagnostics[0].Arguments[0]));
    }

    [Fact]
    public void Parenthesised_left_operands_nested_in_one_another_are_charged_together()
    {
        // ((a+...+a)+b+...+b)+c+...+c, each chain a third of the bound: within it at the bound, refused one past it.
        static string Text(int last) => "x = ((a" + string.Concat(Enumerable.Repeat("+a", 3_333)) + ")" + string.Concat(Enumerable.Repeat("+b", 3_333)) + ")"
            + string.Concat(Enumerable.Repeat("+c", last)) + ";";
        Assert.Equal(ParseOutcome.Accepted, ParseV(Text(ParseLimits.MaximumTreeDepth - 6_666), 64).Outcome);
        Assert.Equal("JSE9202", ParseV(Text(ParseLimits.MaximumTreeDepth - 6_665), 64).Diagnostics[0].Code);

        // Far past the bound on both sides: refused inside the parenthesis, as soon as its chain passes the bound.
        var far = ParseV("x = (a" + string.Concat(Enumerable.Repeat("+a", 200_000)) + ")" + string.Concat(Enumerable.Repeat("+b", 200_000)) + ";", 64);
        Assert.Equal(("JSE9202", 1), (far.Diagnostics[0].Code, far.Diagnostics[0].Span.Line));
        Assert.True(far.Diagnostics[0].Span.Offset < 200_000 * 2, "refused past the parenthesis");
    }

    /// <summary>
    /// Each construct the parser recurses through, nested at the library's supported maximum, is accepted on the documented
    /// stack, and one level more is refused with the contract's nesting diagnostic: the constructs the derivation measured.
    /// </summary>
    [Theory]
    [InlineData("x = ", "[", "1", "]", ";")]
    [InlineData("x = ", "(", "1", ")", ";")]
    [InlineData("", "{", "", "}", "")]
    [InlineData("", "function f() {", "", "}", "")]
    [InlineData("x = ", "`${", "1", "}`", ";")]
    [InlineData("x = ", "{a: ", "1", "}", ";")]
    [InlineData("x = ", "f(", "1", ")", ";")]
    [InlineData("", "a = ", "1", "", ";")]
    [InlineData("x = ", "a ? b : ", "c", "", ";")]
    [InlineData("x = ", "y => ", "y", "", ";")]
    [InlineData("x = ", "!", "a", "", ";")]
    [InlineData("x = ", "new ", "a", "", ";")]
    [InlineData("x = ", "2 ** ", "2", "", ";")]
    [InlineData("var ", "[", "a", "]", " = b;")]
    [InlineData("x = ", "class extends ", "Object", "{}", ";")]
    [InlineData("", "if (a) ", "b;", "", "")]
    [InlineData("", "while (a) ", "b;", "", "")]
    [InlineData("x = ", "() => {return ", "1", "}", ";")]
    [InlineData("x = ", "{[", "a", "]: 1}", ";")]
    [InlineData("x = ", "class { m() { return ", "1", "} }", ";")]
    [InlineData("x = ", "[...", "a", "]", ";")]
    [InlineData("function* g() { ", "yield ", "1", "", " }")]
    [InlineData("x = ", "import(", "a", ")", ";")]
    [InlineData("x = ", "async () => ", "1", "", ";")]
    [InlineData("var ", "{a: ", "b", "}", " = c;")]
    public void Each_recursive_construct_is_accepted_at_the_supported_maximum_on_the_documented_stack(string prefix, string open, string core, string close, string suffix)
    {
        string Text(int levels) => prefix + string.Concat(Enumerable.Repeat(open, levels)) + core + string.Concat(Enumerable.Repeat(close, levels)) + suffix;
        var bound = ParseLimits.MaximumSupportedNestingDepth;

        // The deepest source of the shape the bound admits: the levels before the first refusal, by bisection, each
        // construct charging one to three levels of nesting per level of source.
        var (low, high) = (0, bound + 1);
        while (high - low > 1)
        {
            var middle = (low + high) / 2;
            if (ParseV(Text(middle), bound).Outcome == ParseOutcome.Refused)
                high = middle;
            else
                low = middle;
        }

        var refusedAt = high;
        Assert.True(refusedAt >= bound / 3, "refused at " + refusedAt.ToString(CultureInfo.InvariantCulture) + " levels");
        Assert.Equal(ParseOutcome.Accepted, ParseV(Text(refusedAt - 1), bound).Outcome);
        var refused = ParseV(Text(refusedAt), bound);
        Assert.Equal(("JSE9201", "512"), (refused.Diagnostics[0].Code, refused.Diagnostics[0].Arguments[0]));
        Assert.Equal("JSE9201", ParseV(Text(100_000), bound).Diagnostics[0].Code);
    }
}
