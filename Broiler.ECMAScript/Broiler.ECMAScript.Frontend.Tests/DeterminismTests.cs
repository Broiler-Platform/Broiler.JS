using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>
/// Determinism over the parse corpus (stage SJL-13.2e; the contract's "the same request gives the same output, across runs
/// and threads", rule SL9's question and the home rule's part C): for every input the parse corpus stores in this
/// repository, under each of its variants in both configurations of the home rule (V, Broiler.VM's request, and J,
/// Broiler.JS's) and, for an eval-family input, as eval code, the canonical dump of the parse (<see cref="SyntaxDump"/>:
/// the tree with every node's kind, span and raw facts, the token stream, the ordered diagnostics and the side tables) is
/// the same bytes in a repeated parse, on another thread, in the reverse order, on two threads at once, and under the
/// cultures tr-TR, lt-LT and the invariant culture, current and UI alike.
/// </summary>
/// <remarks>
/// The test262 inputs of the corpus are not stored here; the Native AOT probe's <c>--digest-corpus</c> mode runs the same
/// dump over the whole corpus, test262 included, under the JIT and from the Native AOT image, and its two outputs are
/// compared byte for byte (design.md's note of 2026-11-02 at SJL-13.2e).
/// </remarks>
public sealed class DeterminismTests
{
    private sealed record Item(string Id, string Variant, string Configuration, string Text);

    private static readonly Lazy<IReadOnlyList<Item>> Population = new(Load);

    private static IReadOnlyList<Item> Load()
    {
        var root = Path.Combine(UnicodeDataHome.Root(AppContext.BaseDirectory), "conformance", "parse");
        var items = new List<Item>();
        foreach (var line in File.ReadAllLines(Path.Combine(root, "corpus.manifest")))
        {
            if (line.Length == 0 || line[0] == '#' || line.StartsWith("id\t", StringComparison.Ordinal))
                continue;
            var c = line.Split('\t');
            if (c[1] == "test262")
                continue;
            var bytes = File.ReadAllBytes(Path.Combine(root, c[2]));
            Assert.Equal(c[3], Convert.ToHexStringLower(SHA256.HashData(bytes)));
            var text = new UTF8Encoding(false, false).GetString(bytes);
            foreach (var variant in c[5].Split(','))
            {
                items.Add(new Item(c[0], variant, "V", text));
                items.Add(new Item(c[0], variant, "J", text));
            }

            if (c[7] == "yes")
            {
                items.Add(new Item(c[0], "E0", "V", text));
                items.Add(new Item(c[0], "E3", "V", text));
            }
        }

        return items;
    }

    private static ParseRequest Request(Item item)
    {
        if (item.Variant[0] == 'E')
        {
            var strict = item.Variant == "E3";
            return new ParseRequest(new SourceText(item.Text), ParseGoal.Eval, UnicodeDataHome.Tables, NoValidator.Instance)
            {
                Switches = ParseSwitches.For(ParseGoal.Eval) with { ForcedStrict = strict },
                Context = new InheritedContext { InFunction = strict },
                Limits = new ParseLimits { NestingDepth = new DepthLimit(64) },
            };
        }

        var goal = item.Variant == "module" ? ParseGoal.Module : ParseGoal.Script;
        var j = item.Configuration == "J";
        return new ParseRequest(new SourceText(item.Variant == "strict" ? "\"use strict\";\n" + item.Text : item.Text), goal, UnicodeDataHome.Tables, NoValidator.Instance)
        {
            Switches = ParseSwitches.For(goal),
            Proposals = j ? ProposalFlags.Decorators | ProposalFlags.AutoAccessors | ProposalFlags.ImportSourceCalls | ProposalFlags.ImportDeferCalls : ProposalFlags.None,
            Extensions = j ? LexerExtensions.DecimalSuffix : LexerExtensions.None,
            Limits = new ParseLimits { NestingDepth = new DepthLimit(j ? ParseLimits.MaximumSupportedNestingDepth : 64) },
        };
    }

    // The digests of every item's dump, parsed on a thread of the documented stack under the named culture, in the order
    // given; with twice, each item is parsed a second time at once and the two dumps compared.
    private static string[] Digests(IReadOnlyList<Item> items, string culture, bool reverse, bool twice)
    {
        var digests = new string[items.Count];
        Exception? failure = null;
        var thread = new Thread(
            () =>
            {
                try
                {
                    var info = CultureInfo.GetCultureInfo(culture);
                    CultureInfo.CurrentCulture = info;
                    CultureInfo.CurrentUICulture = info;
                    for (var k = 0; k < items.Count; k++)
                    {
                        var i = reverse ? items.Count - 1 - k : k;
                        var dump = SyntaxDump.Dump(SyntaxParser.Parse(Request(items[i])));
                        if (twice)
                        {
                            var again = SyntaxDump.Dump(SyntaxParser.Parse(Request(items[i])));
                            if (!dump.AsSpan().SequenceEqual(again))
                                throw new InvalidOperationException(items[i].Id + " " + items[i].Variant + " " + items[i].Configuration + ": a repeated parse differs");
                        }

                        digests[i] = Convert.ToHexStringLower(SHA256.HashData(dump));
                    }
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
        return digests;
    }

    private static void AssertSame(IReadOnlyList<Item> items, string[] expected, string[] actual, string what)
    {
        for (var i = 0; i < items.Count; i++)
            Assert.True(expected[i] == actual[i], items[i].Id + " " + items[i].Variant + " " + items[i].Configuration + ": " + what + " differs");
    }

    [Fact]
    public void The_cultures_are_real_ones()
    {
        // Not vacuous: each culture the test parses under has its own casing and collation data, which a culture-reading
        // parse would see.
        Assert.Equal("\u0130", "i".ToUpper(CultureInfo.GetCultureInfo("tr-TR")));
        Assert.Equal("lt-LT", CultureInfo.GetCultureInfo("lt-LT").Name);
        Assert.True(Population.Value.Count > 4_000, "the stored corpus gives " + Population.Value.Count.ToString(CultureInfo.InvariantCulture) + " parses");
        Assert.Contains(Population.Value, static i => i.Variant == "module");
        Assert.Contains(Population.Value, static i => i.Variant[0] == 'E');
    }

    [Fact]
    public void A_repeated_parse_gives_the_same_tree_tokens_diagnostics_and_side_tables()
    {
        var items = Population.Value;
        var first = Digests(items, string.Empty, reverse: false, twice: true);
        AssertSame(items, first, Digests(items, string.Empty, reverse: false, twice: false), "a second run");

        // Not vacuous: accepted and refused parses are both among them.
        var outcomes = items.Take(400).Select(static i => SyntaxParser.Parse(Request(i)).Outcome).Distinct().Count();
        Assert.Equal(2, outcomes);
    }

    [Fact]
    public void The_parse_is_the_same_under_tr_TR_lt_LT_and_the_invariant_culture_and_in_reverse_order()
    {
        var items = Population.Value;
        var invariant = Digests(items, string.Empty, reverse: false, twice: false);
        AssertSame(items, invariant, Digests(items, "tr-TR", reverse: true, twice: false), "tr-TR in reverse order");
        AssertSame(items, invariant, Digests(items, "lt-LT", reverse: false, twice: false), "lt-LT");
    }

    [Fact]
    public void The_parse_is_the_same_on_two_threads_at_once()
    {
        var items = Population.Value;
        var alone = Digests(items, string.Empty, reverse: false, twice: false);
        string[]? a = null;
        string[]? b = null;
        var first = new Thread(() => a = Digests(items, "tr-TR", reverse: false, twice: false));
        var second = new Thread(() => b = Digests(items, "lt-LT", reverse: false, twice: false));
        first.Start();
        second.Start();
        first.Join();
        second.Join();
        AssertSame(items, alone, a!, "the first of two threads");
        AssertSame(items, alone, b!, "the second of two threads");
    }

    [Fact]
    public void The_dump_tells_two_parses_apart()
    {
        // Not vacuous: a span, a raw fact, a token and a diagnostic each change the dump.
        static string Of(string text) => Convert.ToHexStringLower(SHA256.HashData(SyntaxDump.Dump(SyntaxParser.Parse(
            new ParseRequest(new SourceText(text), ParseGoal.Script, UnicodeDataHome.Tables, NoValidator.Instance)))));
        var digests = new[] { Of("a + b;"), Of("a  + b;"), Of("a + 0x1;"), Of("a + 1;"), Of("'\\x61';"), Of("'a';"), Of("a +;"), Of("a + );") };
        Assert.Equal(digests.Length, digests.Distinct(StringComparer.Ordinal).Count());
    }
}
