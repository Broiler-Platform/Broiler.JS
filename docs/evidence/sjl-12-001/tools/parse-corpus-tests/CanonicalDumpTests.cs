using System.Security.Cryptography;
using System.Text;
using Broiler.JavaScript.Ast;

namespace ParseCorpus.Tests;

/// <summary>
/// The canonical dump (decision rule part D): its format, its size and completeness on deep trees,
/// and what a run reports where a dump cannot be completed.
/// </summary>
public sealed class CanonicalDumpTests
{
    // The text of the corpus's deepest inputs: DeeplyNestedSourceTests' conditional fixture at
    // 25 000 levels, the shape of the corpus's J000243 (577 813 bytes), whose dump the first
    // version of the tool could not complete.
    private static string DeepConditional(int levels)
    {
        var source = new StringBuilder("function t(x) { return ", levels * 24 + 64);
        for (var i = 0; i < levels; i++)
            source.Append("x === ").Append(i).Append(" ? ").Append(i).Append(" : (");
        source.Append('0').Append(new string(')', levels)).Append("; } t(7);");
        return source.ToString();
    }

    [Fact]
    public void A_small_program_is_dumped_in_the_documented_format()
    {
        var (text, hash) = OnToolStack(() => Dump("a;"));

        Assert.Equal(
            """
            0 program: Broiler.JavaScript.Ast.AstProgram Program @0..eof IsStatement=true IsBinding=false WasParenthesized=false HoistingScope=[] AnnexBFunctionNames=[] IsSyntheticFunctionStatementBlock=false IsAsync=false
            1 Statements: list 1
            2 [0]: Broiler.JavaScript.Ast.Statements.AstExpressionStatement ExpressionStatement @0..eof IsStatement=true IsBinding=false WasParenthesized=false
            3 Expression: Broiler.JavaScript.Ast.AstIdentifier Identifier @0..1 IsStatement=false IsBinding=false WasParenthesized=false Name="a" inferenceName=null InferenceName="a"

            """.Replace("\r\n", "\n", StringComparison.Ordinal),
            text);
        Assert.Equal(Sha256(text), hash);
    }

    [Fact]
    public void A_non_public_field_is_dumped_and_a_backing_field_is_not_dumped_twice()
    {
        var (text, _) = OnToolStack(() => Dump("x;"));

        // AstIdentifier's private inferenceName, and AstNode.WasParenthesized once, not also as
        // its compiler-generated backing field.
        Assert.Contains(" inferenceName=null ", text, StringComparison.Ordinal);
        Assert.DoesNotContain("k__BackingField", text, StringComparison.Ordinal);
        var nodes = text.Split('\n').Where(line => line.Contains(" @", StringComparison.Ordinal)).ToList();
        Assert.Equal(3, nodes.Count);
        Assert.All(nodes, line => Assert.Equal(2, line.Split(" WasParenthesized=").Length));
    }

    [Fact]
    public void A_tree_as_deep_as_the_corpus_deepest_is_dumped_whole_and_linearly()
    {
        var source = DeepConditional(25_000);
        Assert.Equal(577_813, Encoding.UTF8.GetByteCount(source));

        var (sloppy, sloppyHash) = OnToolStack(() => Dump(source));
        var (strict, strictHash) = OnToolStack(() => Dump("\"use strict\";\n" + source));

        // The whole tree: the last level's literal is in it, and the hash is the text's.
        Assert.Contains("NumericValue=24999 ", sloppy, StringComparison.Ordinal);
        Assert.Equal(Sha256(sloppy), sloppyHash);
        Assert.NotEqual(sloppyHash, strictHash);

        // Linear: a few hundred bytes of dump per source byte would already be quadratic's
        // smallest sign; the first version's text passed 2^31 characters here.
        Assert.True(sloppy.Length < 200L * source.Length, $"the dump is {sloppy.Length} characters for {source.Length}");
        Assert.True(strict.Length > sloppy.Length);
    }

    [Theory]
    [InlineData("nested arrays", 200_000)]
    [InlineData("nested conditionals", 50_000)]
    [InlineData("nested functions", 20_000)]
    [InlineData("nested objects", 100_000)]
    [InlineData("a member chain", 300_000)]
    [InlineData("a call chain", 300_000)]
    [InlineData("nested templates", 20_000)]
    public void A_deep_tree_of_any_shape_is_dumped_whole(string shape, int levels)
    {
        var source = shape switch
        {
            "nested arrays" => new string('[', levels) + new string(']', levels) + ";\n",
            "nested conditionals" => Repeat("x?", levels) + "1" + Repeat(":0", levels) + ";\n",
            "nested functions" => Repeat("function f(){", levels) + new string('}', levels) + "\n",
            "nested objects" => "x=" + Repeat("{a:", levels) + "1" + new string('}', levels) + ";\n",
            "a member chain" => "a" + Repeat(".b", levels) + ";\n",
            "a call chain" => "f" + Repeat("()", levels) + ";\n",
            _ => Repeat("`${", levels) + "1" + Repeat("}`", levels) + ";\n",
        };

        var (text, hash) = OnToolStack(() => Dump(source));

        Assert.StartsWith("0 program: ", text, StringComparison.Ordinal);
        Assert.Equal(Sha256(text), hash);
        Assert.True(text.Length < 400L * source.Length, $"the dump is {text.Length} characters for {source.Length}");
    }

    [Fact]
    public void A_dump_that_cannot_be_completed_is_reported_and_never_hashed_in_part()
    {
        var directory = Directory.CreateTempSubdirectory("parse-corpus-tests-");
        try
        {
            var textPath = Path.Combine(directory.FullName, "dumps.txt");
            string failed, whole;
            using (var sink = new Program.TextSink(textPath))
            using (var dumper = new Program.Dumper(sink, "1"))
            {
                failed = dumper.Dump("X000001 sloppy 1 parser", writer =>
                {
                    writer.Append("0 program: partial");
                    throw new InvalidOperationException("an enumeration that throws");
                });
                whole = dumper.Dump("X000002 sloppy 1 parser", writer => writer.Append("0 program: whole\n"));
            }

            Assert.Equal(Program.Dumper.Failed + "System.InvalidOperationException", failed);
            Assert.Equal(Sha256("0 program: whole\n"), whole);
            Assert.Equal(
                "=== X000001 sloppy 1 parser\n0 program: partial\n!dump-failed System.InvalidOperationException \"an enumeration that throws\"\n"
                + "=== X000002 sloppy 1 parser\n0 program: whole\n",
                File.ReadAllText(textPath));
            Assert.Equal([textPath], Directory.GetFiles(directory.FullName));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void The_exception_a_pass_throws_is_dumped_in_its_place()
    {
        using var writer = new DumpWriter();
        writer.Start(null);
        CanonicalDump.WriteException(writer, new InvalidOperationException("no \"loop\"\n"));
        Assert.Equal(Sha256("throws System.InvalidOperationException \"no \\\"loop\\\"\\n\"\n"), writer.Finish());
    }

    [Theory]
    [InlineData("{\"a\": 1}", "refused")]
    [InlineData("[1, 2, {\"b\": null}]", "accepted")]
    [InlineData("\"text\"", "accepted")]
    public void A_json_row_takes_the_entry_points_of_every_goal_but_module(string json, string outcome)
    {
        var (atP, atK) = OnToolStack(() =>
        {
            using var context = Entries.NewContext();
            return (Entries.Parse(json, module: false, out _), Entries.Compile(json, module: false, "/parse-1/x.json"));
        });

        Assert.Equal(outcome, atP.Kind);
        Assert.Equal(outcome, atK.Kind);
        if (outcome == "refused")
        {
            Assert.Equal("Broiler.JavaScript.Ast.Misc.FastParseException", atP.Type);
            Assert.Equal("Broiler.JavaScript.Runtime.JSException:SyntaxError", atK.Type);
        }
    }

    private static (string Text, string Hash) Dump(string source)
    {
        var parse = Entries.Parse(source, module: false, out var program);
        Assert.Equal(Outcome.Accepted, parse.Kind);

        using var copy = new MemoryStream();
        using var writer = new DumpWriter();
        writer.Start(copy);
        CanonicalDump.Write(writer, program!);
        var hash = writer.Finish();
        return (Encoding.UTF8.GetString(copy.ToArray()), hash);
    }

    private static string Repeat(string text, int count) => new StringBuilder(text.Length * count).Insert(0, text, count).ToString();

    private static string Sha256(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    // The tool parses on threads of 256 MB (rule part C); a test thread's stack is far smaller.
    private static T OnToolStack<T>(Func<T> work)
    {
        T result = default!;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = work();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        }, Program.StackBytes);
        thread.Start();
        thread.Join();
        if (failure != null)
            throw new InvalidOperationException("the work failed on the tool's stack", failure);
        return result;
    }
}
