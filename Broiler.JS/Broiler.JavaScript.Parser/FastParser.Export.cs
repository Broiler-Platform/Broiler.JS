#nullable enable
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;

partial class FastParser
{
    public AstLiteral ExpectStringLiteral()
    {
        var token = stream.Expect(TokenTypes.String);
        return new AstLiteral(TokenTypes.String, token);
    }

    public bool Export(FastToken start, out AstStatement statement)
    {
        stream.Consume();
        var token = stream.Current;

        if (token.IsKeyword)
        {
            switch (token.Keyword)
            {
                case FastKeywords.@default:
                    stream.Consume();
                    if (!Expression(out var argument))
                        throw stream.Unexpected();

                    statement = new AstExportStatement(start, argument, true);
                    return true;

                case FastKeywords.function:
                    if (!FunctionExpression(out var f))
                        throw stream.Unexpected();

                    var fn = f as AstFunctionExpression;
                    if (fn.Id == null)
                        throw new FastParseException(f.Start, "exported function must have a name");

                    statement = new AstExportStatement(start, fn);
                    return true;

                case FastKeywords.@class:
                    // `export [default] class C {}` is a ClassDeclaration: bind C.
                    if (!ClassExpression(out var @class, isStatement: true))
                        throw stream.Unexpected();

                    var c = @class as AstClassExpression;
                    if (c.Identifier == null)
                        throw new FastParseException(c.Start, "exported class must have a name");

                    statement = new AstExportStatement(start, c);
                    return true;

                case FastKeywords.var:
                    if (!VariableDeclaration(out var stmt))
                        throw stream.Unexpected();

                    statement = new AstExportStatement(start, stmt);
                    return true;

                case FastKeywords.let:
                    if (!VariableDeclaration(out stmt, FastVariableKind.Let))
                        throw stream.Unexpected();

                    statement = new AstExportStatement(start, stmt);
                    return true;

                case FastKeywords.@const:
                    if (!VariableDeclaration(out stmt, FastVariableKind.Const))
                        throw stream.Unexpected();

                    statement = new AstExportStatement(start, stmt);
                    return true;
            }
        }

        if (stream.CheckAndConsume(TokenTypes.Multiply))
        {
            if (stream.CheckAndConsumeContextualKeyword(FastKeywords.@as))
            {
                if (!Identitifer(out var namespaceIdentifier))
                    throw stream.Unexpected();

                stream.ExpectContextualKeyword(FastKeywords.from);

                var namespaceSource = ExpectStringLiteral();
                // An ExportDeclaration with a FromClause takes a WithClause exactly as an
                // ImportDeclaration does, and all three `from` forms below accept one too. As on
                // the import side, the attributes are parsed and not yet acted on — nothing reads
                // AstImportStatement.Attributes either — so this makes valid source compile rather
                // than claiming the attribute is enforced.
                ImportAttributes();
                isAsync = true;
                statement = new AstExportStatement(start, namespaceIdentifier, namespaceSource);
                return true;
            }

            stream.ExpectContextualKeyword(FastKeywords.from);

            var literal = ExpectStringLiteral();
            ImportAttributes();
            // Like the `* as ns` form above: the module has to be imported before its names can be
            // republished, and that import is awaited.
            isAsync = true;
            statement = new AstExportStatement(start, null, literal);
            return true;
        }

        // NamedExports — `export { a, b as c }`, with or without a `from` clause.
        //
        // A clause is NOT a declaration: `export { x }` exports a binding that already exists and
        // introduces nothing. This used to be read by AssignmentLeftPattern as an object
        // DESTRUCTURING pattern declaring each name as a `var`, and then required a `from`. Both
        // halves were wrong, and between them they rejected every clause form: `const x = 1;
        // export { x }` failed as "x is already defined in current scope" (the pattern redeclared
        // it), and `var x = 1; export { x }` failed as "Expecting keyword from".
        FastToken? reservedLocalName = null;

        if (ExportNames(out var members))
        {
            if (stream.CheckAndConsumeContextualKeyword(FastKeywords.from))
            {
                var reexportSource = ExpectStringLiteral();
                ImportAttributes();
                isAsync = true;
                statement = new AstExportStatement(start, members!, reexportSource, reexportSource.End);
                return true;
            }

            // Without a `from` clause the local name is an IdentifierReference to a binding in
            // THIS module, so it cannot be a reserved word — unlike the exported name after `as`,
            // and unlike both sides of a re-export, which are ModuleExportNames and name nothing
            // local. `export { null }` is an error where `export { null as x } from 'm'` is not.
            if (reservedLocalName != null)
            {
                throw new FastParseException(
                    reservedLocalName,
                    $"'{reservedLocalName.Span}' is not a valid exported binding name");
            }

            statement = new AstExportStatement(start, members!, null, stream.Current);
            return true;
        }

        throw stream.Unexpected();

        // The export half of FastParser.Import's ImportNames, and deliberately its mirror image:
        // the two clauses share a shape in the grammar. The pair is (local name, exported name).
        bool ExportNames(out IFastEnumerable<(StringSpan, StringSpan)>? names)
        {
            if (!stream.CheckAndConsume(TokenTypes.CurlyBracketStart))
            {
                names = null;
                return false;
            }

            var list = new Sequence<(StringSpan, StringSpan)>();

            while (!stream.CheckAndConsume(TokenTypes.CurlyBracketEnd))
            {
                var localToken = stream.Current;
                if (!ModuleExportName(out var localName))
                    throw stream.Unexpected();

                // Remembered rather than rejected here: whether a reserved word is legal in this
                // position depends on a `from` clause that has not been read yet.
                if (localToken.IsKeyword || IsKeywordPropertyName(localToken.Type))
                    reservedLocalName ??= localToken;

                if (stream.CheckAndConsumeContextualKeyword(FastKeywords.@as))
                {
                    if (!ModuleExportName(out var exportedName))
                        throw stream.Unexpected();

                    list.Add((localName, exportedName));
                }
                else
                {
                    list.Add((localName, localName));
                }

                if (stream.CheckAndConsume(TokenTypes.Comma))
                    continue;

                if (stream.CheckAndConsume(TokenTypes.CurlyBracketEnd))
                    break;

                throw stream.Unexpected();
            }

            names = list;
            return true;
        }

        // A ModuleExportName is an IdentifierName, so ANY reserved word is legal: it names a
        // property of the module namespace rather than a binding. `export { x as default }` is the
        // familiar case, but `export { a as in } from 'm'` is equally well formed.
        bool ModuleExportName(out StringSpan name)
        {
            var current = stream.Current;

            // Two shapes of reserved word: most carry a keyword on an identifier-shaped token,
            // while the handful in IsKeywordPropertyName (`null`, `true`, `false`, `in`,
            // `instanceof`) get a token type of their own from the scanner. A PropertyName has to
            // accept both, and so does a ModuleExportName, for the same reason.
            if (current.IsKeyword || IsKeywordPropertyName(current.Type))
            {
                stream.Consume();
                name = current.Span;
                return true;
            }

            if (Identitifer(out var id))
            {
                name = id.Name;
                return true;
            }

            name = default;
            return false;
        }
    }
}
