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
        // docs/frontend/limits/derivation.md): the library's parse needs no stack per level of tree depth.
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

    /// <summary>
    /// A decorator's member chain and its call, under the decorators proposal's flag (decision D17), are charged as any
    /// other chain: at the bound accepted, one link past it refused with the tree-depth diagnostic, never built.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_decorator_s_member_chain_is_bounded_by_the_tree_depth_constant(bool call)
    {
        string Text(int links) => "@a" + string.Concat(Enumerable.Repeat(".a", links)) + (call ? "()" : string.Empty) + " class C {}";
        ParseResult Parse(string text) => OnDocumentedStack(() => SyntaxParser.Parse(
            new ParseRequest(new SourceText(text), ParseGoal.Script, Runners.UnicodeDataHome.Tables, NoValidator.Instance)
            {
                Proposals = ProposalFlags.Decorators,
                Limits = new ParseLimits { NestingDepth = new DepthLimit(64) },
            }));
        var atBound = ParseLimits.MaximumTreeDepth - (call ? 1 : 0);
        Assert.Equal(ParseOutcome.Accepted, Parse(Text(atBound)).Outcome);
        foreach (var links in new[] { atBound + 1, 100_000 })
        {
            var refused = Parse(Text(links));
            Assert.Equal(ParseOutcome.Refused, refused.Outcome);
            Assert.Equal(("JSE9202", "10000"), (refused.Diagnostics[0].Code, refused.Diagnostics[0].Arguments[0]));
        }
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
    /// The shapes of decision D14's charge as the owner decided it on 2026-11-02 (the derivation's section 5 and its note of
    /// that day): a chain whose left operand, or one of whose operands, holds a chain anywhere below it, not along the left
    /// spine alone. Each shape is a source whose tree depth is <c>inner + outer + extra</c>: a chain of <c>inner</c> nodes in
    /// the place the shape names, under or beside a chain of <c>outer</c> nodes, with <c>extra</c> the chain nodes the shape
    /// adds itself (the call, the member or the operator the inner chain hangs under).
    /// </summary>
    public static TheoryData<string, string, string, string, string, int> ResidualShapes => new()
    {
        // A right operand of a chain's early node, by precedence and parenthesised.
        { "right operand by precedence", "x = 1 + 2", "*2", "", "+3", 1 },
        { "right operand parenthesised", "x = a + (b", "+b", ")", "+c", 1 },
        // A chain beginning with an expression whose spine stage 4c did not name.
        { "prefix operator's operand", "x = -(a", "+a", ")", "+b", 0 },
        { "await's operand", "async function f() { x = await (a", "+a", ")", "+b", 0 },
        { "yield's operand", "function* g() { x = (yield a", "+a", ")", "+b", 0 },
        { "postfix update's operand", "x = (a", ".a", "++)", "+b", 0 },
        { "prefix update's operand", "x = (--a", ".a", ")", "+b", 0 },
        { "array literal's element", "x = [a", "+a", "]", ".b", 0 },
        { "object literal's value", "x = ({p: a", "+a", "})", ".b", 0 },
        { "object literal's computed key", "x = ({[a", "+a", "]: 1})", ".b", 0 },
        { "spread element", "x = [...(a", "+a", ")]", ".b", 0 },
        { "call's argument", "x = f(a", "+a", ")", "+b", 1 },
        { "call's spread argument", "x = f(...(a", "+a", "))", "+b", 1 },
        { "new's argument", "x = new C(a", "+a", ")", "+b", 0 },
        { "import call's argument", "x = import(a", "+a", ")", ".b", 0 },
        { "computed member's key", "x = o[a", "+a", "]", "+b", 1 },
        { "optional chain's computed key", "x = o?.[a", "+a", "]", ".b", 1 },
        { "tagged template's substitution", "x = t`${a", "+a", "}`", "+b", 1 },
        // A parenthesised expression that is not a chain, holding one.
        { "sequence", "x = (0, a", "+a", ")", "+b", 0 },
        { "conditional", "x = (c ? 0 : a", "+a", ")", "+b", 0 },
        { "assignment", "x = (y = a", "+a", ")", "+b", 0 },
        { "arrow function's body", "x = (() => a", "+a", ")", "+b", 0 },
        { "function expression's body", "x = (function () { return a", "+a", "; })", "+b", 0 },
        { "class heritage", "x = (class extends (a", "+a", ") {})", "+b", 0 },
        { "class field's initialiser", "x = (class { p = a", "+a", "; })", "+b", 0 },
        { "private in's right operand", "x = class { #x; m() { return #x in a", "<<a", "", "||b", 0 },
    };

    /// <summary>
    /// Every residual shape is charged its whole tree depth (decision D14, the owner's decision of 2026-11-02): at the
    /// bound accepted, one node past it refused with the tree-depth diagnostic, and far past it refused, on the documented
    /// stack. Stage 4e's charge, along a chain's left spine alone, accepted every one of these sources past the bound.
    /// </summary>
    [Theory]
    [MemberData(nameof(ResidualShapes))]
    public void Every_shape_of_the_tree_is_charged_its_depth(string shape, string prefix, string innerLink, string close, string outerLink, int extra)
    {
        const int inner = 5_000;
        var suffix = shape == "private in's right operand" ? "; } };" : prefix.StartsWith("async function", StringComparison.Ordinal)
            || prefix.StartsWith("function*", StringComparison.Ordinal) ? "; }" : ";";
        string Text(int outer) => prefix + string.Concat(Enumerable.Repeat(innerLink, inner)) + close
            + string.Concat(Enumerable.Repeat(outerLink, outer)) + suffix;
        var atBound = ParseLimits.MaximumTreeDepth - inner - extra;

        var accepted = ParseV(Text(atBound), 64);
        Assert.True(accepted.Outcome == ParseOutcome.Accepted, shape + " at the bound: " + (accepted.Diagnostics.Count > 0 ? accepted.Diagnostics[0].Code : "-"));
        foreach (var outer in new[] { atBound + 1, 100_000 })
        {
            var refused = ParseV(Text(outer), 64);
            Assert.True(refused.Outcome == ParseOutcome.Refused, shape + " with " + outer.ToString(CultureInfo.InvariantCulture) + " outer nodes was accepted");
            Assert.Equal(("JSE9202", "limit.tree.too-deep", "10000"), (refused.Diagnostics[0].Code, refused.Diagnostics[0].MessageId, refused.Diagnostics[0].Arguments[0]));
        }
    }

    /// <summary>
    /// A decorator's expression, under the decorators proposal's flag (decision D17), is a residual shape too: a call's
    /// argument chain in a class expression's decorator, the class the object of a member chain.
    /// </summary>
    [Fact]
    public void A_decorator_s_expression_is_charged_its_depth()
    {
        const int inner = 5_000;
        static string Text(int outer) => "x = (@d(a" + string.Concat(Enumerable.Repeat("+a", inner)) + ") class {})"
            + string.Concat(Enumerable.Repeat(".b", outer)) + ";";
        ParseResult Parse(string text) => OnDocumentedStack(() => SyntaxParser.Parse(
            new ParseRequest(new SourceText(text), ParseGoal.Script, Runners.UnicodeDataHome.Tables, NoValidator.Instance)
            {
                Proposals = ProposalFlags.Decorators,
                Limits = new ParseLimits { NestingDepth = new DepthLimit(64) },
            }));

        // The call over the argument chain adds one: inner + 1 + outer.
        var atBound = ParseLimits.MaximumTreeDepth - inner - 1;
        var accepted = Parse(Text(atBound));
        Assert.True(accepted.Outcome == ParseOutcome.Accepted, "at the bound: " + (accepted.Diagnostics.Count > 0 ? accepted.Diagnostics[0].Code : "-"));
        foreach (var outer in new[] { atBound + 1, 100_000 })
        {
            var refused = Parse(Text(outer));
            Assert.Equal(ParseOutcome.Refused, refused.Outcome);
            Assert.Equal(("JSE9202", "10000"), (refused.Diagnostics[0].Code, refused.Diagnostics[0].Arguments[0]));
        }
    }

    /// <summary>
    /// The charge counts the tree's depth and nothing more (the review of stage SJL-13.2f): a node's operands, a chain's
    /// arguments and keys, beside a deep left operand, are charged their deepest, not their sum, so a tree within the
    /// constant is accepted however its chains stand, and one node past it is refused. Stage 4e charged a right operand
    /// read inside a chain from the depth the chain had reached, and the charge this stage first made charged an argument
    /// or a right operand from the chain's first operand's height as well; each refused a source marked so below, whose
    /// tree is within the constant.
    /// </summary>
    [Fact]
    public void A_tree_within_the_constant_is_accepted_however_its_chains_stand()
    {
        static string Chain(string head, string link, int nodes) => head + string.Concat(Enumerable.Repeat(link, nodes));
        var max = ParseLimits.MaximumTreeDepth;
        var cases = new (string Text, bool Accepted)[]
        {
            // Depth 5,002: a member chain of 5,000 and a call over a chain of 5,000, under one + (refused by the first charge).
            ("x = " + Chain("a", ".a", 5_000) + " + f(" + Chain("b", "+b", 5_000) + ");", true),
            // Depth 9,001: an array whose element is a chain of 9,000, beside a parenthesised chain of 2,000 (refused by the
            // first charge).
            ("x = [" + Chain("a", "+a", 9_000) + "] + (" + Chain("b", "+b", 2_000) + ");", true),
            // Depth 5,002: a chain of 5,001 whose last right operand is a parenthesised chain of 5,001 (refused at stage 4e and
            // by the first charge).
            ("x = " + Chain("a", "+a", 5_000) + " + (" + Chain("b", "+b", 5_001) + ");", true),
            // Both operands of one node at the bound's depth less one: the tree at the bound (refused at stage 4e and by the
            // first charge), then one past it on either side.
            ("x = (" + Chain("a", "+a", max - 1) + ") + (" + Chain("b", "+b", max - 1) + ");", true),
            ("x = (" + Chain("a", "+a", max - 1) + ") + (" + Chain("b", "+b", max) + ");", false),
            ("x = (" + Chain("a", "+a", max) + ") + (" + Chain("b", "+b", max - 1) + ");", false),
            // A call's argument and a computed key beside a deep member chain: at the bound (refused at stage 4e and by the
            // first charge), and one past it.
            ("x = " + Chain("a", ".a", 5_000) + "[" + Chain("b", "+b", max - 2) + "](" + Chain("c", "+c", max - 2) + ");", true),
            ("x = " + Chain("a", ".a", 5_000) + "[" + Chain("b", "+b", max - 2) + "](" + Chain("c", "+c", max) + ");", false),
        };
        foreach (var (text, expected) in cases)
        {
            var result = ParseV(text, 64);
            var label = text[..Math.Min(text.Length, 24)] + " (" + text.Length.ToString(CultureInfo.InvariantCulture) + " characters)";
            if (expected)
            {
                Assert.True(result.Outcome == ParseOutcome.Accepted, label + " was refused: " + (result.Diagnostics.Count > 0 ? result.Diagnostics[0].Code : "-"));
                continue;
            }

            Assert.True(result.Outcome == ParseOutcome.Refused, label + " was accepted");
            Assert.Equal(("JSE9202", "10000"), (result.Diagnostics[0].Code, result.Diagnostics[0].Arguments[0]));
        }
    }

    /// <summary>
    /// The charge counts the tree's depth, not the source's length: chains side by side, in a call's arguments, an array's
    /// elements or a sequence, each within the bound, are accepted however many there are, since none hangs under another.
    /// </summary>
    [Fact]
    public void Chains_side_by_side_are_charged_their_deepest_not_their_sum()
    {
        var chain = "a" + string.Concat(Enumerable.Repeat("+a", 9_998));
        foreach (var text in new[] { "x = f(" + chain + ", " + chain + ", " + chain + ");", "x = [" + chain + ", " + chain + "];",
            "x = (" + chain + ", " + chain + ");", chain + "; " + chain + ";" })
        {
            var result = ParseV(text, 64);
            Assert.True(result.Outcome == ParseOutcome.Accepted, text[..20] + " was refused: " + (result.Diagnostics.Count > 0 ? result.Diagnostics[0].Code : "-"));
        }
    }

    /// <summary>
    /// One operator of each binary precedence from <c>||</c> to <c>*</c>: a source that climbs the whole precedence ladder
    /// before each level of a construct, which the parser reads with an explicit stack and does not charge (the
    /// derivation's section 3).
    /// </summary>
    private const string Ladder = "a || a && a | a ^ a & a == a < a << a + a * ";

    /// <summary>The ladder with <c>#x in</c> at the relational precedence, whose right operand the parser reads by a recursion of its own.</summary>
    private const string PrivateInLadder = "a || a && a | a ^ a & a == #x in a << a + a * ";

    /// <summary>
    /// Each construct the parser recurses through, nested at the library's supported maximum, is accepted on the documented
    /// stack, and one level more is refused with the contract's nesting diagnostic: the constructs the derivation measured,
    /// alone and after the precedence ladder.
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
    [InlineData("x = ", Ladder + "{[", "a", "]: 1}", ";")]
    [InlineData("x = ", Ladder + "(", "1", ")", ";")]
    [InlineData("x = ", Ladder + "{a: ", "1", "}", ";")]
    [InlineData("x = ", Ladder + "f(", "1", ")", ";")]
    [InlineData("x = ", Ladder + "[", "1", "]", ";")]
    [InlineData("x = ", Ladder + "`${", "1", "}`", ";")]
    [InlineData("x = ", Ladder + "class { m() { return ", "1", "} }", ";")]
    [InlineData("x = ", Ladder + "import(", "a", ")", ";")]
    [InlineData("x = class { #x; m() { return ", PrivateInLadder + "(", "1", ")", "; } };")]
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
