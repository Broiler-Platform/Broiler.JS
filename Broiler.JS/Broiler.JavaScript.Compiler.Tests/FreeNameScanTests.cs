using System.Linq;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Parser;

namespace Broiler.JavaScript.Compiler.Tests;

// The free-name walk item 1-1's remaining half needs: which names a function body references but
// does not itself bind (docs/performance-roadmap.md item 1-1).
//
// The walk exists because a captured name's index in the enclosing lambda's `Box[]` is decided by
// `LambdaRewriter` FROM THE TREE, and a deferred body has no tree. Its whole value is that it can
// tell a free reference from a locally bound one — the existing `NestedFunctionScanner` collects
// every identifier a nested function MENTIONS, which cannot distinguish `function (x) { return x; }`
// from `function () { return x; }`, and boxing on a mention costs the enclosing function its
// numeric tier for a name nobody captured.
//
// **So every fixture here is a pair or a negative.** A scanner that returned "every identifier"
// would pass any test that only checks a free name is present; what these check is that the bound
// ones are ABSENT.
public sealed class FreeNameScanTests
{
    private static (string[] Free, bool Dynamic) Scan(string source)
    {
        var program = new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram();
        var function = FindFirstFunction(program) ?? throw new System.InvalidOperationException("no function in source");
        var scan = FreeNameScan.Of(function);
        return (scan.Free.OrderBy(n => n, System.StringComparer.Ordinal).ToArray(), scan.Dynamic);
    }

    private static AstFunctionExpression FindFirstFunction(Broiler.JavaScript.Ast.AstNode node)
    {
        var finder = new Finder();
        finder.Visit(node);
        return finder.Found;
    }

    private sealed class Finder : Broiler.JavaScript.Ast.AstReduce
    {
        public AstFunctionExpression Found;

        protected override Broiler.JavaScript.Ast.AstNode VisitFunctionExpression(AstFunctionExpression function)
        {
            Found ??= function;
            return function;
        }

        protected override Broiler.JavaScript.Ast.VariableDeclarator VisitVariableDeclarator(
            Broiler.JavaScript.Ast.VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            if (declarator.Init != null)
                Visit(declarator.Init);
            return declarator;
        }
    }

    /// <summary>Every function's free set from the one-pass form, keyed by source order.</summary>
    private static string[][] ScanAll(string source)
    {
        var program = new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram();
        var all = FreeNameScan.ForProgram(program);
        var ordered = new System.Collections.Generic.List<string[]>();
        var finder = new AllFinder(all, ordered);
        finder.Visit(program);
        return [.. ordered];
    }

    private sealed class AllFinder(
        System.Collections.Generic.Dictionary<AstFunctionExpression, FreeNameScan> all,
        System.Collections.Generic.List<string[]> ordered) : Broiler.JavaScript.Ast.AstReduce
    {
        protected override Broiler.JavaScript.Ast.AstNode VisitFunctionExpression(AstFunctionExpression function)
        {
            if (all.TryGetValue(function, out var scan))
                ordered.Add([.. scan.Free.OrderBy(n => n, System.StringComparer.Ordinal)]);

            return base.VisitFunctionExpression(function);
        }

        protected override Broiler.JavaScript.Ast.VariableDeclarator VisitVariableDeclarator(
            Broiler.JavaScript.Ast.VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            if (declarator.Init != null)
                Visit(declarator.Init);
            return declarator;
        }
    }

    [Theory]
    [InlineData("var f = function (x) { return x; };")]
    [InlineData("var f = function () { return x; };")]
    [InlineData("var f = function () { { var x = 1; } return x; };")]
    [InlineData("var f = function () { return function () { return outer; }; };")]
    [InlineData("var f = function () { return function (outer) { return outer; }; };")]
    [InlineData("var f = function (a) { return function () { return function () { return a + b; }; }; };")]
    [InlineData("var f = function () { try { } catch (q) { } return e; };")]
    [InlineData("var f = function ({ a, b = fallback }) { return a + b; };")]
    [InlineData("var f = function (o) { return { a: 1, b: o.c }; };")]
    [InlineData("var o = function () { f(); function f() { return z; } };")]
    [InlineData("var f = function (a) { return function (b) { return function (c) { return a + b + c + d; }; }; };")]
    public void TheOnePassFormAgreesWithAnIndependentOracle(string source)
    {
        // **The engine ships one pass; this is the naive definition it has to match.** The one-pass
        // form composes — a function's free set is its own references plus its children's, minus
        // what it binds — and that rule is exactly the kind of thing that is right on a flat
        // program and wrong three levels down. The oracle here computes each function's set from
        // scratch, so the two share no code and a defect cannot hide between them.
        //
        // It matters because the naive shape is what the FIRST measurement priced, and it cost
        // **47.7% of Box2D's body tree construction against 5.8% on Closure** — superlinear in
        // nesting depth, because scanning a function re-walks every function inside it. One pass
        // is linear; it is only worth having if it gives the same answers.
        var program = new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram();
        var all = FreeNameScan.ForProgram(program);
        Assert.NotEmpty(all);

        foreach (var (function, onePass) in all)
        {
            Assert.Equal(
                NaiveFreeNames(function).OrderBy(n => n, System.StringComparer.Ordinal),
                onePass.Free.OrderBy(n => n, System.StringComparer.Ordinal));
        }
    }

    /// <summary>
    /// The definition, computed directly: every identifier the function's own code references,
    /// plus every free name of each function nested in it, minus everything it binds.
    /// </summary>
    private static System.Collections.Generic.HashSet<string> NaiveFreeNames(AstFunctionExpression function)
    {
        var bound = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        var referenced = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);

        if (!function.IsArrowFunction)
            bound.Add("arguments");
        if (function.Id != null)
            bound.Add(function.Id.Name.Value);

        var parameters = function.Params.GetFastEnumerator();
        while (parameters.MoveNext(out var parameter))
            CollectBound(parameter.Identifier, bound, referenced);

        // A `var` or function declaration anywhere in the body binds from function entry; a
        // `let`/`const` binds only in its block, so the oracle collects the first eagerly and
        // leaves the second to the walk. Same rules as the implementation, different structure —
        // the structure is what is under test.
        new NaiveHoister(bound).Visit(function.Body);
        new NaiveCollector(bound, referenced).Visit(function.Body);
        referenced.ExceptWith(bound);
        return referenced;
    }

    private static void CollectBound(
        Broiler.JavaScript.Ast.AstNode target,
        System.Collections.Generic.HashSet<string> bound,
        System.Collections.Generic.HashSet<string> referenced)
    {
        switch (target)
        {
            case Broiler.JavaScript.Ast.AstIdentifier id:
                bound.Add(id.Name.Value);
                return;
            case Broiler.JavaScript.Ast.Patterns.AstArrayPattern array:
                var elements = array.Elements.GetFastEnumerator();
                while (elements.MoveNext(out var element))
                    CollectBound(element, bound, referenced);
                return;
            case Broiler.JavaScript.Ast.Patterns.AstObjectPattern pattern:
                var properties = pattern.Properties.GetFastEnumerator();
                while (properties.MoveNext(out var property))
                {
                    CollectBound(property.Value ?? property.Key, bound, referenced);

                    // `{ b = fallback }` carries the default in Init, not in a binary node, so a
                    // pattern walker that only follows Value never sees it. The second oracle bug
                    // this comparison found.
                    if (property.Init != null && !ReferenceEquals(bound, referenced))
                        new NaiveCollector(bound, referenced).Visit(property.Init);
                }

                return;
            case AstBinaryExpression { Operator: Broiler.JavaScript.Ast.Misc.TokenTypes.Assign } withDefault:
                // The target binds and the DEFAULT references — the oracle missed the second half
                // on its first version, which is what a cross-check is for.
                CollectBound(withDefault.Left, bound, referenced);
                new NaiveCollector(bound, referenced).Visit(withDefault.Right);
                return;
            case AstUnaryExpression spread:
                CollectBound(spread.Argument, bound, referenced);
                return;
        }
    }

    /// <summary>Binds this body's own `var`s and function declarations, not nested ones'.</summary>
    private sealed class NaiveHoister(System.Collections.Generic.HashSet<string> bound)
        : Broiler.JavaScript.Ast.AstReduce
    {
        protected override Broiler.JavaScript.Ast.AstNode VisitFunctionExpression(AstFunctionExpression nested)
        {
            if (nested.IsStatement && nested.Id != null)
                bound.Add(nested.Id.Name.Value);

            return nested;
        }

        protected override Broiler.JavaScript.Ast.AstNode VisitVariableDeclaration(
            Broiler.JavaScript.Ast.AstVariableDeclaration declaration)
        {
            if (declaration.Kind != Broiler.JavaScript.Ast.FastVariableKind.Var)
                return declaration;

            var declarators = declaration.Declarators.GetFastEnumerator();
            while (declarators.MoveNext(out var declarator))
                CollectBound(declarator.Identifier, bound, bound);

            return declaration;
        }

        protected override Broiler.JavaScript.Ast.Misc.Case VisitCase(Broiler.JavaScript.Ast.Misc.Case @case)
        {
            var statements = @case.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
                Visit(statement);
            return @case;
        }
    }

    private sealed class NaiveCollector(
        System.Collections.Generic.HashSet<string> bound,
        System.Collections.Generic.HashSet<string> referenced) : Broiler.JavaScript.Ast.AstReduce
    {
        protected override Broiler.JavaScript.Ast.AstNode VisitIdentifier(Broiler.JavaScript.Ast.AstIdentifier id)
        {
            referenced.Add(id.Name.Value);
            return id;
        }

        protected override Broiler.JavaScript.Ast.AstNode VisitFunctionExpression(AstFunctionExpression nested)
        {
            // A nested function contributes its FREE names, not its identifiers, and its own name
            // binds here when it is a declaration.
            if (nested.IsStatement && nested.Id != null)
                bound.Add(nested.Id.Name.Value);

            foreach (var name in NaiveFreeNames(nested))
                referenced.Add(name);

            return nested;
        }

        protected override Broiler.JavaScript.Ast.AstNode VisitMemberExpression(
            Broiler.JavaScript.Ast.Expressions.AstMemberExpression member)
        {
            Visit(member.Object);
            if (member.Computed)
                Visit(member.Property);
            return member;
        }

        protected override Broiler.JavaScript.Ast.AstNode VisitObjectLiteral(Broiler.JavaScript.Ast.AstObjectLiteral literal)
        {
            var properties = literal.Properties.GetFastEnumerator();
            while (properties.MoveNext(out var node))
            {
                if (node is not Broiler.JavaScript.Ast.AstClassProperty property)
                {
                    Visit(node);
                    continue;
                }

                if (property.Computed || property.Init == null)
                    Visit(property.Key);
                if (property.Init != null)
                    Visit(property.Init);
            }

            return literal;
        }

        protected override Broiler.JavaScript.Ast.AstNode VisitVariableDeclaration(
            Broiler.JavaScript.Ast.AstVariableDeclaration declaration)
        {
            var declarators = declaration.Declarators.GetFastEnumerator();
            while (declarators.MoveNext(out var declarator))
            {
                // `var` was bound by the hoist pass. A `let`/`const` binds in its own block, and
                // the oracle does not model blocks — so it binds nothing here and the block-scoped
                // cases are asserted by their own fixtures instead of by this comparison.
                if (declaration.Kind == Broiler.JavaScript.Ast.FastVariableKind.Var)
                    CollectBound(declarator.Identifier, bound, referenced);

                if (declarator.Init != null)
                    Visit(declarator.Init);
            }

            return declaration;
        }

        protected override Broiler.JavaScript.Ast.AstNode VisitTryStatement(
            Broiler.JavaScript.Ast.Statements.AstTryStatement statement)
        {
            Visit(statement.Block);
            if (statement.Catch != null)
            {
                if (statement.CatchParam != null)
                    CollectBound(statement.CatchParam, bound, referenced);
                Visit(statement.Catch);
            }

            if (statement.Finally != null)
                Visit(statement.Finally);
            return statement;
        }

        protected override Broiler.JavaScript.Ast.VariableDeclarator VisitVariableDeclarator(
            Broiler.JavaScript.Ast.VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            if (declarator.Init != null)
                Visit(declarator.Init);
            return declarator;
        }

        protected override Broiler.JavaScript.Ast.Misc.Case VisitCase(Broiler.JavaScript.Ast.Misc.Case @case)
        {
            if (@case.Test != null)
                Visit(@case.Test);
            var statements = @case.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
                Visit(statement);
            return @case;
        }
    }

    [Fact(Timeout = 600000)]
    public void TheOnePassFormGivesEachNestedFunctionItsOwnSet()
    {
        // The per-function form cannot answer this at all — it reports one set for whatever it was
        // handed — and the deferral needs one per site, because each deferred body boxes its own
        // captures.
        var sets = ScanAll("var f = function (a) { return function (b) { return a + b + c; }; };");

        Assert.Equal(2, sets.Length);
        Assert.Equal(["c"], sets[0]);        // outer: `a` is its own parameter, `c` is free
        Assert.Equal(["a", "c"], sets[1]);   // inner: `b` is its own, `a` and `c` come from above
    }

    [Fact(Timeout = 600000)]
    public void AParameterBindsAndAnUndeclaredNameDoesNot()
    {
        // The pair the whole walk exists for, and the one a mention-collector cannot answer.
        Assert.Empty(Scan("var f = function (x) { return x; };").Free);
        Assert.Equal(["x"], Scan("var f = function () { return x; };").Free);
    }

    [Fact(Timeout = 600000)]
    public void AVarIsFunctionScopedAndALetIsBlockScoped()
    {
        // `{ var x; } x` is bound and `{ let x; } x` is free — the difference the scope stack
        // exists to model, and the one case where "collect the declarations" without scoping gives
        // the wrong answer in the UNSAFE direction.
        Assert.Empty(Scan("var f = function () { { var x = 1; } return x; };").Free);
        Assert.Equal(["x"], Scan("var f = function () { { let x = 1; } return x; };").Free);
    }

    [Fact(Timeout = 600000)]
    public void AHoistedDeclarationBindsAReferenceThatPrecedesIt()
    {
        // `f(); function f(){}` and `x; var x;` are both bound: hoisting is why the walk needs a
        // pass before the walk. Without it these read free, and the deferral would box a name the
        // body declares itself.
        Assert.Empty(Scan("var o = function () { f(); function f() {} };").Free);
        Assert.Empty(Scan("var o = function () { return x; var x = 1; };").Free);
    }

    [Fact(Timeout = 600000)]
    public void ANamedFunctionExpressionBindsItsOwnName()
    {
        Assert.Empty(Scan("var f = function g () { return g; };").Free);
    }

    [Fact(Timeout = 600000)]
    public void ArgumentsIsBoundByAFunctionAndNotByAnArrow()
    {
        Assert.Empty(Scan("var f = function () { return arguments; };").Free);
        Assert.Equal(["arguments"], Scan("var f = () => arguments;").Free);
    }

    [Fact(Timeout = 600000)]
    public void CaptureIsTransitiveThroughANestedFunction()
    {
        // A body two levels down reaching a name two levels up is the shape `RelayRewriteTests`
        // exists for, and the deferral has to box it at the top.
        Assert.Equal(["outer"], Scan("var f = function () { return function () { return outer; }; };").Free);

        // ...and the inner function's own binding stops it.
        Assert.Empty(Scan("var f = function () { return function (outer) { return outer; }; };").Free);
    }

    [Fact(Timeout = 600000)]
    public void ACatchParameterBindsOnlyInsideItsHandler()
    {
        Assert.Empty(Scan("var f = function () { try { } catch (e) { return e; } };").Free);
        Assert.Equal(["e"], Scan("var f = function () { try { } catch (q) { } return e; };").Free);
    }

    [Fact(Timeout = 600000)]
    public void DestructuringBindsItsTargetsAndReadsItsDefaults()
    {
        // The two sides of one declarator go opposite ways, which is the easiest thing here to get
        // backwards: `{ a: b } = o` binds b and reads o.
        Assert.Equal(["fallback"], Scan("var f = function ({ a, b = fallback }) { return a + b; };").Free);
        Assert.Equal(["src"], Scan("var f = function () { var { p: q } = src; return q; };").Free);
        Assert.Equal(["src"], Scan("var f = function () { var [m, n] = src; return m + n; };").Free);
    }

    [Fact(Timeout = 600000)]
    public void APropertyNameIsNotAReference()
    {
        // `{ a: 1 }` does not read `a`, and `o.a` does not either — but `o[a]` does. A scanner that
        // counted property names would box a binding for every object literal key in the program.
        Assert.Empty(Scan("var f = function (o) { return { a: 1, b: o.c }; };").Free);
        Assert.Equal(["k"], Scan("var f = function (o) { return o[k]; };").Free);
    }

    [Fact(Timeout = 600000)]
    public void ADirectEvalOrWithMakesTheBodyUndeferrable()
    {
        // Neither can be described by a free-name set — they reach bindings that are never
        // mentioned — so the answer is not a bigger set, it is "do not defer this one".
        Assert.True(Scan("var f = function () { eval('x = 1'); };").Dynamic);
        Assert.True(Scan("var f = function (o) { with (o) { y; } };").Dynamic);
        Assert.False(Scan("var f = function (o) { return o.x; };").Dynamic);
    }

    [Fact(Timeout = 600000)]
    public void ALoopVariableAndItsBodyResolveTogether()
    {
        Assert.Equal(["limit"], Scan("var f = function () { for (var i = 0; i < limit; i++) { } return i; };").Free);
        Assert.Equal(["items"], Scan("var f = function () { for (const it of items) { it; } };").Free);
    }
}
