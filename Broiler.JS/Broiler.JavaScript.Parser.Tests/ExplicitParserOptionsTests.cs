using System.Reflection;
using Broiler.JavaScript.Ast.Misc;
using Broiler.Unicode.Algorithms;

namespace Broiler.JavaScript.Parser.Tests;

// The scanner, the token stream and the parser take their options when they are made (milestone
// SJL-12.5 item 1; Broiler.VM's JSD-0080, decision D19): the goal, the top-level-await permission and
// the Unicode provider reach a parse through its options and from nowhere else.
public class ExplicitParserOptionsTests
{
    private static FastParser Parser(string source, FastParserOptions options)
        => new(new FastTokenStream(new StringSpan(source), null, options), options);

    [Fact]
    public void The_constructors_without_options_parse_a_script()
    {
        var stream = new FastTokenStream(new StringSpan("a;"));
        Assert.Equal(FastParserOptions.Script, stream.Options);
        Assert.Equal(FastParserOptions.Script, new FastParser(stream).Options);
        Assert.Equal(FastParserOptions.Script, new FastScanner(new FastPool(), new StringSpan("a;")).Options);
    }

    [Fact]
    public void A_parser_takes_its_streams_options()
    {
        var options = FastParserOptions.Module with { AllowTopLevelAwait = false };
        var stream = new FastTokenStream(new StringSpan("a;"), null, options);
        Assert.Same(options, stream.Options);
        Assert.Same(options, new FastParser(stream).Options);
    }

    [Fact]
    public void A_parser_refuses_a_stream_made_with_other_options()
    {
        var stream = new FastTokenStream(new StringSpan("a;"), null, FastParserOptions.Script);
        var refusal = Assert.Throws<ArgumentException>(() => new FastParser(stream, FastParserOptions.Module));
        Assert.Equal("options", refusal.ParamName);

        // Equal options are the same options, whichever instance carries them.
        Assert.NotNull(new FastParser(stream, new FastParserOptions()).ParseProgram());
    }

    [Fact]
    public void Null_options_are_refused()
    {
        Assert.Throws<ArgumentNullException>(() => new FastScanner(new FastPool(), new StringSpan("a;"), null, null!));
        Assert.Throws<ArgumentNullException>(() => new FastTokenStream(new StringSpan("a;"), null, null!));
        Assert.Throws<ArgumentNullException>(() => new FastTokenStream(new FastPool(), new StringSpan("a;"), null, null!));
        Assert.Throws<ArgumentNullException>(() => new FastParser(new FastTokenStream(new StringSpan("a;")), null!));
        Assert.Throws<ArgumentNullException>(() => FastParser.ParseModule(new StringSpan("a;"), null!));
    }

    // Annex B.1.1: the goal the stream is made with decides how its scanner reads `<!--`, which it
    // does for its first tokens before any parser exists.
    [Theory]
    [InlineData("<!-- a comment in a script\nvar a = 1;")]
    [InlineData("var a = 1;\n--> a comment in a script")]
    public void The_streams_goal_decides_whether_html_like_comments_are_comments(string source)
    {
        Assert.NotNull(Parser(source, FastParserOptions.Script).ParseProgram());
        Assert.Throws<FastParseException>(() => Parser(source, FastParserOptions.Module).ParseProgram());
        Assert.Throws<FastParseException>(() => FastParser.ParseModule(new StringSpan(source)));
    }

    [Theory]
    [InlineData("await 1;")]
    [InlineData("for await (const v of []) {}")]
    public void Top_level_await_is_the_options_permission(string source)
    {
        Assert.Throws<FastParseException>(() => Parser(source, FastParserOptions.Script).ParseProgram());

        var script = Parser(source, FastParserOptions.Script with { AllowTopLevelAwait = true }).ParseProgram();
        Assert.True(script.IsAsync);

        var module = FastParser.ParseModule(new StringSpan(source));
        Assert.True(module.IsAsync);

        // A module made without the permission parses `await` at its top level as an
        // identifier, which the Module goal reserves.
        Assert.Throws<FastParseException>(() => FastParser.ParseModule(new StringSpan(source), FastParserOptions.Module with { AllowTopLevelAwait = false }));
    }

    [Fact]
    public void In_a_script_without_the_permission_a_top_level_await_is_an_identifier()
    {
        var program = Parser("await(1);", FastParserOptions.Script).ParseProgram();
        Assert.False(program.IsAsync);
    }

    [Fact]
    public void ParseModule_refuses_options_whose_goal_is_the_script()
    {
        var refusal = Assert.Throws<ArgumentException>(() => FastParser.ParseModule(new StringSpan("a;"), FastParserOptions.Script));
        Assert.Equal("options", refusal.ParamName);
    }

    // The provider is read through: a non-ASCII code point's class comes from its tables. Over the
    // engine's own tables, read through the provider, the parser answers as it does without one.
    [Theory]
    [InlineData("var été = 1;", true)]
    [InlineData("var a　= 1;", true)]
    [InlineData("var ⸯ = 1;", false)]
    public void The_scanner_reads_its_lexical_classes_from_the_options_provider(string source, bool accepted)
    {
        var tables = new CountingTables(EngineTables());
        var options = new FastParserOptions { UnicodeTables = tables };
        Assert.Same(tables, options.UnicodeTables);

        var before = tables.LexicalReads;
        if (accepted)
        {
            Assert.NotNull(Parser(source, options).ParseProgram());
            Assert.NotNull(Parser(source, FastParserOptions.Script).ParseProgram());
        }
        else
        {
            Assert.Throws<FastParseException>(() => Parser(source, options).ParseProgram());
            Assert.Throws<FastParseException>(() => Parser(source, FastParserOptions.Script).ParseProgram());
        }

        Assert.True(tables.LexicalReads > before, "The scanner did not read the provider's lexical classes.");
    }

    [Fact]
    public void Options_with_a_provider_are_equal_only_with_the_same_provider()
    {
        var tables = EngineTables();
        Assert.Equal(new FastParserOptions { UnicodeTables = tables }, new FastParserOptions { UnicodeTables = tables });
        Assert.NotEqual(new FastParserOptions { UnicodeTables = tables }, FastParserOptions.Script);
        Assert.NotEqual(new FastParserOptions { UnicodeTables = tables }, new FastParserOptions { UnicodeTables = new CountingTables(tables) });
    }

    // The engine's own tables, which an assembly the parser references holds as an internal type;
    // found by its simple name, so that the test follows the tables when they move.
    private static IUnicodeTables EngineTables()
    {
        foreach (var name in typeof(FastParser).Assembly.GetReferencedAssemblies())
        {
            var type = Assembly.Load(name).GetTypes().SingleOrDefault(t => t.Name == "JSUnicodeTables" && typeof(IUnicodeTables).IsAssignableFrom(t));
            if (type?.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) is IUnicodeTables tables)
                return tables;
        }

        throw new InvalidOperationException("No assembly the parser references holds the engine's JSUnicodeTables.");
    }

    private sealed class CountingTables(IUnicodeTables inner) : IUnicodeTables
    {
        private int lexicalReads;

        public int LexicalReads => Volatile.Read(ref lexicalReads);

        public string UnicodeVersion => inner.UnicodeVersion;

        public ReadOnlySpan<byte> Table(UnicodeTable table)
        {
            if (table == UnicodeTable.LexicalTrie)
                Interlocked.Increment(ref lexicalReads);
            return inner.Table(table);
        }
    }
}
