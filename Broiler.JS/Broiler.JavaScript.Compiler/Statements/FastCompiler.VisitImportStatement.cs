using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitImportStatement(AstImportStatement importStatement)
    {
        var tempRequire = BExpression.Parameter(typeof(JSValue));
        var require = scope.Top.GetVariable("import");
        var source = VisitExpression(importStatement.Source);

        var args = ImportArguments(source, importStatement.Attributes);

        var stmts = new Sequence<BExpression>
        {
            BExpression.Assign(tempRequire, BExpression.Yield(JSFunctionBuilder.InvokeFunction(require.Expression, args)))
        };

        FastFunctionScope.VariableScope imported;
        var all = importStatement.All;

        // An ImportedBinding is immutable (ES2024 16.2.1.5: the environment record's binding for an
        // imported name is created immutable). The engine seeds it once from the loaded namespace,
        // then marks it read-only so a later `x = …` throws in the strict module code rather than
        // silently overwriting the local — the same runtime TypeError a reassigned `const` gives.
        // (The spec makes assignment to an import an early SyntaxError; this engine cannot raise that
        // without whole-module scope analysis across deferred function bodies, so it matches its own
        // `const` treatment — a runtime read-only write — instead of leaving the write to succeed.)
        void SealImport(FastFunctionScope.VariableScope binding)
            => stmts.Add(JSVariableBuilder.SetReadOnly(binding.Variable, true));

        if (all != null)
        {
            imported = scope.Top.CreateVariable(all.Name);
            stmts.Add(BExpression.Assign(imported.Expression, tempRequire));
            SealImport(imported);
        }

        if (importStatement.Default != null)
        {
            imported = scope.Top.CreateVariable(importStatement.Default.Name);
            var prop = JSValueBuilder.Index(tempRequire, KeyOfName("default"));
            stmts.Add(BExpression.Assign(imported.Expression, prop));
            SealImport(imported);
        }

        if (importStatement.Members != null)
        {
            var ve = importStatement.Members.GetFastEnumerator();
            while (ve.MoveNext(out var item))
            {
                imported = scope.Top.CreateVariable(item.asName);
                var prop = JSValueBuilder.Index(tempRequire, KeyOfName(item.name));
                stmts.Add(BExpression.Assign(imported.Expression, prop));
                SealImport(imported);
            }
        }

        var importExp = BExpression.Block(tempRequire.AsSequence(), stmts);
        return importExp;
    }

    /// <summary>
    /// The argument list for a call to the host's module loader: the specifier, and a static
    /// declaration's <c>with { … }</c> clause when it has one.
    /// </summary>
    /// <remarks>
    /// Shared by <c>import</c> declarations and by the three <c>export … from</c> forms, which load
    /// a module for exactly the same reason and take the same clause. The loader's argument list is
    /// shared with dynamic <c>import()</c>, which puts its runtime options object in slot 1; a static
    /// declaration has no options object, so its attributes travel in slot 2 and the host validates
    /// both shapes through one path. Passing them rather than dropping them is what lets
    /// <c>with { type: 'json' }</c> be enforced instead of parsed and ignored.
    /// </remarks>
    private static BExpression ImportArguments(
        BExpression source, IFastEnumerable<(StringSpan key, AstLiteral value)> attributes)
    {
        var pairs = ImportAttributePairs(attributes);
        return pairs == null
            ? ArgumentsBuilder.New(JSUndefinedBuilder.Value, source)
            : ArgumentsBuilder.New(JSUndefinedBuilder.Value,
                new BExpression[] { source, JSUndefinedBuilder.Value, pairs });
    }

    /// <summary>
    /// A static import's attribute clause as a flat JS array of alternating key/value strings, or
    /// <see langword="null"/> when there is no clause and nothing to pass.
    /// </summary>
    /// <remarks>
    /// Flat rather than an object because every part of it is a string literal the grammar already
    /// fixed — <c>AttributeKey : IdentifierName | StringLiteral</c> and a StringLiteral value — so
    /// there is nothing to evaluate and nothing an object would carry that this does not. It also
    /// keeps the pairs in source order, which is what lets the host report the *first* offending key
    /// rather than whichever one a property table happened to yield first.
    /// </remarks>
    private static BExpression ImportAttributePairs(IFastEnumerable<(StringSpan key, AstLiteral value)> attributes)
    {
        if (attributes == null)
            return null;

        var inits = new Sequence<BElementInit>();
        var e = attributes.GetFastEnumerator();
        while (e.MoveNext(out var attribute))
        {
            inits.Add(BExpression.ElementInit(JSArrayBuilder._Add,
                [JSStringBuilder.New(BExpression.Constant(attribute.key.ToString()))]));
            inits.Add(BExpression.ElementInit(JSArrayBuilder._Add,
                [JSStringBuilder.New(BExpression.Constant(attribute.value.StringValue.ToString()))]));
        }

        // `with { }` is legal and means no attributes; it must still be distinguishable from no
        // clause at all only in that both are unconstrained, so an empty array is fine to skip.
        return inits.Count == 0 ? null : JSArrayBuilder.New(inits);
    }
}
