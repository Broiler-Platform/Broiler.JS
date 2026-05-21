using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Parser;

namespace Broiler.JavaScript.Parser.Tests;

public class ParserTests
{
    [Fact]
    public void ParseProgram_SimpleExpression_ReturnsProgram()
    {
        var stream = new FastTokenStream(new StringSpan("42;"));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();
        Assert.NotNull(program);
        Assert.Equal(FastNodeType.Program, program.Type);
    }

    [Fact]
    public void ParseProgram_VariableDeclaration_Succeeds()
    {
        var stream = new FastTokenStream(new StringSpan("var x = 10;"));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_FunctionDeclaration_Succeeds()
    {
        var stream = new FastTokenStream(new StringSpan("function add(a, b) { return a + b; }"));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_ArrowFunction_Succeeds()
    {
        var stream = new FastTokenStream(new StringSpan("const f = (x) => x * 2;"));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Theory]
    [InlineData("const f = ([,]) => 1;")]
    [InlineData("const f = ([[,] = []]) => 1;")]
    [InlineData("const f = ([...[,]]) => 1;")]
    public void ParseProgram_ArrowFunction_ArrayDestructuringElisions_Succeed(string source)
    {
        var stream = new FastTokenStream(new StringSpan(source));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_GeneratorFunction_BareYield_Succeeds()
    {
        var stream = new FastTokenStream(new StringSpan("""
            function* g() {
                yield;
            }

            var f = ([,]) => 1;
            """));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_InvalidSyntax_ThrowsFastParseException()
    {
        var stream = new FastTokenStream(new StringSpan("function { }"));
        var parser = new FastParser(stream);
        Assert.Throws<FastParseException>(() => parser.ParseProgram());
    }

    [Theory]
    [InlineData("({ async get foo() { return 1; } });")]
    [InlineData("({ async set foo(value) { } });")]
    [InlineData("class C { async get foo() { return 1; } }")]
    [InlineData("class C { async set foo(value) { } }")]
    public void ParseProgram_InvalidAsyncAccessorSyntax_ThrowsFastParseException(string source)
    {
        var stream = new FastTokenStream(new StringSpan(source));
        var parser = new FastParser(stream);
        Assert.Throws<FastParseException>(() => parser.ParseProgram());
    }

    [Fact]
    public void ParseProgram_ObjectLiteral_Allows_AsyncMethods_Named_Get_And_Set()
    {
        var stream = new FastTokenStream(new StringSpan("({ async get() { return 1; }, async set(value) { return value; } });"));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();

        var statement = Assert.IsType<AstExpressionStatement>(Assert.Single(program.Statements.ToArray()));
        var objectLiteral = Assert.IsType<AstObjectLiteral>(statement.Expression);
        var properties = objectLiteral.Properties.ToArray();

        Assert.Equal(2, properties.Length);
        AssertAsyncMethod(properties[0], "get");
        AssertAsyncMethod(properties[1], "set");
    }

    [Fact]
    public void ParseProgram_ClassBody_Allows_AsyncMethods_Named_Get_And_Set()
    {
        var stream = new FastTokenStream(new StringSpan("class C { async get() { return 1; } async set(value) { return value; } }"));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();

        var statement = Assert.IsType<AstExpressionStatement>(Assert.Single(program.Statements.ToArray()));
        var classExpression = Assert.IsType<AstClassExpression>(statement.Expression);
        var properties = classExpression.Members.ToArray();

        Assert.Equal(2, properties.Length);
        AssertAsyncMethod(properties[0], "get");
        AssertAsyncMethod(properties[1], "set");
    }

    [Theory]
    [InlineData("class C { static *#m([]) { return 1; } }", false)]
    [InlineData("class C { static async *#m([]) { return 1; } }", true)]
    public void ParseProgram_ClassBody_Allows_StaticPrivateGeneratorMethods(string source, bool isAsync)
    {
        var stream = new FastTokenStream(new StringSpan(source));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();

        var statement = Assert.IsType<AstExpressionStatement>(Assert.Single(program.Statements.ToArray()));
        var classExpression = Assert.IsType<AstClassExpression>(statement.Expression);
        var property = Assert.IsType<AstClassProperty>(Assert.Single(classExpression.Members.ToArray()));
        var key = Assert.IsType<AstIdentifier>(property.Key);
        var function = Assert.IsType<AstFunctionExpression>(property.Init);

        Assert.Equal(AstPropertyKind.Method, property.Kind);
        Assert.Equal("#m", key.ToString());
        Assert.True(property.IsStatic);
        Assert.True(property.IsPrivate);
        Assert.True(function.Generator);
        Assert.Equal(isAsync, function.Async);
    }

    [Fact]
    public void ParseProgram_ForAwaitOf_In_AsyncFunction_Succeeds()
    {
        var stream = new FastTokenStream(new StringSpan("async function run() { for await (const value of source) { value; } }"));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_ForAwaitIn_ThrowsFastParseException()
    {
        var stream = new FastTokenStream(new StringSpan("async function run() { for await (value in source) { } }"));
        var parser = new FastParser(stream);
        Assert.Throws<FastParseException>(() => parser.ParseProgram());
    }

    [Theory]
    [InlineData("var obj = {}; obj.\\u0063onst = 42;", "const")]
    [InlineData("var groups = {}; groups.\\u03C0;", "π")]
    [InlineData("var groups = {}; groups._\\u200C;", "_\u200C")]
    public void ParseProgram_MemberExpression_Allows_UnicodeEscapedIdentifierNames(string source, string expectedName)
    {
        var stream = new FastTokenStream(new StringSpan(source));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();

        var statement = Assert.IsType<AstExpressionStatement>(program.Statements.ToArray().Last());
        var expression = statement.Expression is AstBinaryExpression binary
            ? binary.Left
            : statement.Expression;
        var member = Assert.IsType<AstMemberExpression>(expression);
        var property = Assert.IsType<AstIdentifier>(member.Property);

        Assert.Equal(expectedName, property.Name.Value);
    }

    [Theory]
    [InlineData("class C { static #\\u{6F}() {} }", "#o")]
    [InlineData("class C { static #\\u2118() {} }", "#℘")]
    [InlineData("class C { static #ZW_\\u200C_NJ() {} }", "#ZW_\u200C_NJ")]
    public void ParseProgram_ClassBody_Allows_UnicodeEscapedPrivateIdentifiers(string source, string expectedName)
    {
        var stream = new FastTokenStream(new StringSpan(source));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();

        var statement = Assert.IsType<AstExpressionStatement>(Assert.Single(program.Statements.ToArray()));
        var classExpression = Assert.IsType<AstClassExpression>(statement.Expression);
        var property = Assert.IsType<AstClassProperty>(Assert.Single(classExpression.Members.ToArray()));
        var key = Assert.IsType<AstIdentifier>(property.Key);

        Assert.True(property.IsPrivate);
        Assert.Equal(expectedName, key.Name.Value);
    }

    [Fact]
    public void ParseProgram_ClassBody_Allows_PrivateMemberAccess()
    {
        var stream = new FastTokenStream(new StringSpan("class C { #m() { return 1; } get method() { return this.#m; } }"));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();

        var statement = Assert.IsType<AstExpressionStatement>(Assert.Single(program.Statements.ToArray()));
        var classExpression = Assert.IsType<AstClassExpression>(statement.Expression);
        var properties = classExpression.Members.ToArray();
        var getter = Assert.IsType<AstClassProperty>(properties[1]);
        var function = Assert.IsType<AstFunctionExpression>(getter.Init);
        var body = Assert.IsType<AstBlock>(function.Body);
        var returnStatement = Assert.IsType<AstReturnStatement>(Assert.Single(body.Statements.ToArray()));
        var member = Assert.IsType<AstMemberExpression>(returnStatement.Argument);
        var property = Assert.IsType<AstIdentifier>(member.Property);

        Assert.False(member.Computed);
        Assert.Equal("#m", property.Name.Value);
    }

    [Theory]
    [InlineData(@"/^\p{Any}+$/u;")]
    [InlineData(@"/\P{ASCII}/u;")]
    [InlineData(@"/[\p{Alphabetic}\P{ASCII_Hex_Digit}]/v;")]
    public void ParseProgram_RegExpLiteral_Allows_UnicodePropertyEscapes(string source)
    {
        var stream = new FastTokenStream(new StringSpan(source));
        var parser = new FastParser(stream);
        var program = parser.ParseProgram();

        Assert.NotNull(program);
    }

    private static void AssertAsyncMethod(AstNode node, string expectedName)
    {
        var property = Assert.IsType<AstClassProperty>(node);
        var key = Assert.IsType<AstIdentifier>(property.Key);
        var function = Assert.IsType<AstFunctionExpression>(property.Init);

        Assert.Equal(AstPropertyKind.Method, property.Kind);
        Assert.Equal(expectedName, key.ToString());
        Assert.True(function.Async);
    }
}
