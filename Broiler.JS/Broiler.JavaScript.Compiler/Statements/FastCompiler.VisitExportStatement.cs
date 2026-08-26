using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void ExtractName(Sequence<StringSpan> list, AstNode node)
    {
        switch (node.Type)
        {
            case FastNodeType.VariableDeclaration:
                var vd = node as AstVariableDeclaration;
                var ve = vd.Declarators.GetFastEnumerator();

                while (ve.MoveNext(out var d))
                    ExtractName(list, d.Identifier);

                return;

            case FastNodeType.Identifier:
                var id = node as AstIdentifier;
                list.Add(id.Start.Span);
                return;

            case FastNodeType.ArrayPattern:
                var ap = node as AstArrayPattern;
                var ae = ap.Elements.GetFastEnumerator();

                while (ae.MoveNext(out var aitem))
                    ExtractName(list, aitem);

                return;

            case FastNodeType.ObjectPattern:
                var op = node as AstObjectPattern;
                var oe = op.Properties.GetFastEnumerator();

                while (oe.MoveNext(out var oitem))
                    ExtractName(list, oitem.Value);

                return;
        }
    }


    static IFastEnumerable<StringSpan> Names(AstNode expression)
    {
        var list = new Sequence<StringSpan>();
        ExtractName(list, expression);
        return list;
    }

    protected override BExpression VisitExportStatement(AstExportStatement exportStatement)
    {
        var exports = scope.Top.GetVariable("exports");
        var top = scope.Top;
        var declaration = exportStatement.Declaration;
        BExpression left;

        // An ExportDeclaration is only legal in module code (its bindings target the
        // host-injected `exports` object). In a plain script `exports` is absent, so
        // an `export` here is an early SyntaxError — the same verdict V8 gives an
        // export in a non-module. Reject it cleanly rather than dereferencing the
        // null `exports` below (which previously surfaced as a NullReferenceException,
        // notably for `export default <expr>`).
        if (exports == null)
            throw new FastParseException(exportStatement.Start, "'export' is only valid inside a module");

        if (exportStatement.IsDefault)
        {
            var defExports = JSValueBuilder.Index(exports.Expression, KeyOfName("default"));
            return BExpression.Assign(defExports, Visit(declaration));
        }

        var list = new Sequence<BExpression>();

        // `export { a, b as c }` — with a `from` clause, the names come from that module; without
        // one, they are LOCAL bindings this module already has, and the clause only publishes
        // them. Neither form declares anything.
        if (exportStatement.Members != null)
            return VisitNamedExports(exportStatement, exports, list);

        // `export * from '…'` carries no declaration at all — the switch below reads
        // Declaration.Type, which is why this used to die with a NullReferenceException.
        // (`export * as ns from '…'` DOES carry one, the namespace identifier, and is handled
        // by the Identifier case.)
        if (exportStatement.ExportAll)
            return VisitExportAll(exportStatement, exports, list);

        try
        {
            switch (exportStatement.Declaration.Type)
            {
                case FastNodeType.VariableDeclaration:
                    var vd = Visit(declaration);
                    var names = Names(declaration);
                    var en = names.GetFastEnumerator();

                    list.Add(vd);

                    while (en.MoveNext(out var name))
                    {
                        left = JSValueBuilder.Index(exports.Expression, KeyOfName(name));
                        var right = top.GetVariable(name);
                        list.Add(BExpression.Assign(left, right.Expression));
                    }

                    return BExpression.Block(list);

                case FastNodeType.Identifier:
                    var id = exportStatement.Declaration as AstIdentifier;
                    left = JSValueBuilder.Index(exports.Expression, KeyOfName(id.Name));

                    if (exportStatement.Source != null)
                    {
                        var tempRequire = BExpression.Parameter(typeof(JSValue));
                        var import = scope.Top.GetVariable("import");
                        var source = VisitExpression((AstExpression)exportStatement.Source);
                        var args = ImportArguments(source, exportStatement.Attributes);

                        return BExpression.Block(
                            tempRequire.AsSequence(),
                            BExpression.Assign(tempRequire, BExpression.Yield(JSFunctionBuilder.InvokeFunction(import.Expression, args))),
                            BExpression.Assign(left, tempRequire));
                    }

                    return left;

                case FastNodeType.FunctionExpression:
                    var fe = Visit(declaration);
                    var fd = declaration as AstFunctionExpression;

                    if (fd.Id != null)
                    {
                        left = JSValueBuilder.Index(exports.Expression, KeyOfName(fd.Id.Name));
                        return BExpression.Assign(left, fe);
                    }

                    break;

                case FastNodeType.ClassStatement:
                    var ce = Visit(declaration);

                    // A class declaration is an AstClassExpression, and its name is `Identifier`.
                    // Casting it to AstFunctionExpression produced null, so reading the name threw
                    // a NullReferenceException — `export class C {}` never worked at all.
                    if (declaration is AstClassExpression { Identifier: { } className })
                    {
                        left = JSValueBuilder.Index(exports.Expression, KeyOfName(className.Name));
                        return BExpression.Assign(left, ce);
                    }

                    break;
            }

            throw new FastParseException(exportStatement.Start, $"Unexpected export type {exportStatement.Declaration.Type}");
        }
        finally
        {
        }
    }

    /// <summary>
    /// Compiles <c>export * from '…'</c>: import the source module once, then republish every one
    /// of its named exports.
    /// </summary>
    /// <remarks>
    /// The names cannot be emitted one by one the way a <c>export { a } from '…'</c> clause's can,
    /// because which names exist is a property of the source module rather than of this one's
    /// text — so the copy is a run-time operation over the imported namespace. <c>default</c> is
    /// excluded there, per the star entry's <c>all-but-default</c> [[ImportName]].
    /// </remarks>
    private BExpression VisitExportAll(AstExportStatement exportStatement, FastFunctionScope.VariableScope exports, Sequence<BExpression> list)
    {
        var imported = BExpression.Parameter(typeof(JSValue));
        var import = scope.Top.GetVariable("import");
        var source = VisitExpression((AstExpression)exportStatement.Source);
        var args = ImportArguments(source, exportStatement.Attributes);

        list.Add(BExpression.Assign(
            imported,
            BExpression.Yield(JSFunctionBuilder.InvokeFunction(import.Expression, args))));
        list.Add(JSModuleExportsBuilder.CopyStarExports(imported, exports.Expression));

        return BExpression.Block(imported.AsSequence(), list);
    }

    /// <summary>
    /// Compiles a NamedExports clause: <c>export { a, b as c }</c>, and the same with a
    /// <c>from</c> clause.
    /// </summary>
    /// <remarks>
    /// Without <c>from</c> each specifier publishes a binding the module already has, so the value
    /// is read through the binding itself — the clause introduces nothing. A local name with no
    /// binding is an early error (ES2024 16.2.3: every ReferencedBindings of a NamedExports must
    /// be declared), which is worth raising here rather than emitting a read of a binding that
    /// does not exist.
    /// <para>
    /// With <c>from</c> the names are not this module's at all: the source module is imported once
    /// into a temporary and each specifier is copied off it, which is the same shape the
    /// <c>export * as ns from</c> path above uses.
    /// </para>
    /// </remarks>
    private BExpression VisitNamedExports(AstExportStatement exportStatement, FastFunctionScope.VariableScope exports, Sequence<BExpression> list)
    {
        var members = exportStatement.Members!;

        if (exportStatement.Source != null)
        {
            var imported = BExpression.Parameter(typeof(JSValue));
            var import = scope.Top.GetVariable("import");
            var source = VisitExpression((AstExpression)exportStatement.Source);
            var args = ImportArguments(source, exportStatement.Attributes);

            list.Add(BExpression.Assign(
                imported,
                BExpression.Yield(JSFunctionBuilder.InvokeFunction(import.Expression, args))));

            var reexported = members.GetFastEnumerator();
            while (reexported.MoveNext(out var member))
            {
                list.Add(BExpression.Assign(
                    JSValueBuilder.Index(exports.Expression, KeyOfName(member.asName)),
                    JSValueBuilder.Index(imported, KeyOfName(member.name))));
            }

            return BExpression.Block(imported.AsSequence(), list);
        }

        var local = members.GetFastEnumerator();
        while (local.MoveNext(out var member))
        {
            var binding = scope.Top.GetVariable(member.name);
            if (binding == null)
            {
                throw new FastParseException(
                    exportStatement.Start,
                    $"Export '{member.name}' is not defined in module");
            }

            list.Add(BExpression.Assign(
                JSValueBuilder.Index(exports.Expression, KeyOfName(member.asName)),
                binding.Expression));
        }

        // An empty clause — `export {}` — is legal and publishes nothing.
        return list.Count == 0 ? JSUndefinedBuilder.Value : BExpression.Block(list);
    }
}
