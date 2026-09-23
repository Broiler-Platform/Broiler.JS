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
                    statement = new AstExportStatement(start, ExportDefaultDeclaration(), true);
                    return true;

                case FastKeywords.function:
                    // `export function f() {}` is a HoistableDeclaration: parsed as a declaration
                    // so its name is bound (and hoisted) in the module scope exactly as the bare
                    // declaration's would be. As an expression the name bound nothing, so
                    // `export function f() {} export { f as g }` could not find `f`.
                    if (!FunctionExpression(out var f, isStatement: true))
                        throw stream.Unexpected();

                    var fn = f as AstFunctionExpression;
                    if (fn.Id == null)
                        throw new FastParseException(f.Start, "exported function must have a name");

                    statement = new AstExportStatement(start, fn);
                    return true;

                case FastKeywords.async:
                    if (!AsyncFunctionDeclarationFollows())
                        break;

                    if (!FunctionExpression(out var asyncFunction, isAsync: true, isStatement: true))
                        throw stream.Unexpected();

                    if ((asyncFunction as AstFunctionExpression).Id == null)
                        throw new FastParseException(asyncFunction.Start, "exported function must have a name");

                    statement = new AstExportStatement(start, asyncFunction);
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
                // `export * as ns from 'm'` names a ModuleExportName, so a reserved word or a
                // string literal is as legal as an identifier. A string name is carried as the
                // literal itself.
                AstNode namespaceIdentifier;
                if (stream.Current.Type == TokenTypes.String)
                {
                    var nameToken = stream.Current;
                    ModuleExportNameLiteral();
                    namespaceIdentifier = new AstLiteral(TokenTypes.String, nameToken);
                }
                else if (IsKeywordPropertyName(stream.Current.Type))
                {
                    namespaceIdentifier = new AstIdentifier(stream.Current);
                    stream.Consume();
                }
                else if (Identitifer(out var namespaceId))
                    namespaceIdentifier = namespaceId;
                else
                    throw stream.Unexpected();

                stream.ExpectContextualKeyword(FastKeywords.from);

                var namespaceSource = ExpectStringLiteral();
                // An ExportDeclaration with a FromClause takes a WithClause exactly as an
                // ImportDeclaration does, and all three `from` forms below accept one too. The
                // clause constrains the load of the module named by the FromClause, so it is
                // carried to the compiler and handed to the host exactly as an import's is.
                var namespaceAttrs = ImportAttributes();
                ExpectEndOfModuleItem();
                statement = new AstExportStatement(start, namespaceIdentifier, namespaceSource, namespaceAttrs);
                return true;
            }

            stream.ExpectContextualKeyword(FastKeywords.from);

            var literal = ExpectStringLiteral();
            var starAttrs = ImportAttributes();
            ExpectEndOfModuleItem();
            statement = new AstExportStatement(start, null, literal, starAttrs);
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
        FastToken? stringLocalName = null;

        if (ExportNames(out var members))
        {
            if (stream.CheckAndConsumeContextualKeyword(FastKeywords.from))
            {
                var reexportSource = ExpectStringLiteral();
                var reexportAttrs = ImportAttributes();
                ExpectEndOfModuleItem();
                statement = new AstExportStatement(start, members!, reexportSource, reexportSource.End, reexportAttrs);
                return true;
            }

            // Without a `from` clause the local name is an IdentifierReference to a binding in
            // THIS module, so it cannot be a reserved word — unlike the exported name after `as`,
            // and unlike both sides of a re-export, which are ModuleExportNames and name nothing
            // local. `export { null }` is an error where `export { null as x } from 'm'` is not.
            // `export { "a" }` / `export { "a" as b }` without `from`: a string names no local binding.
            if (stringLocalName != null)
                throw new FastParseException(stringLocalName, "A string literal cannot be used as an exported binding without 'from'");

            if (reservedLocalName != null)
            {
                throw new FastParseException(
                    reservedLocalName,
                    $"'{reservedLocalName.Span}' is not a valid exported binding name");
            }

            ExpectEndOfModuleItem();
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

                if (localToken.Type == TokenTypes.String)
                    stringLocalName ??= localToken;

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

            if (current.Type == TokenTypes.String)
            {
                name = ModuleExportNameLiteral();
                return true;
            }

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

    /// <summary>
    /// The operand of <c>export default</c>: a HoistableDeclaration (a function, generator, async
    /// function or async generator declaration, named or not), a ClassDeclaration (named or not),
    /// or else an AssignmentExpression, which the enclosing statement terminates like any other.
    /// </summary>
    /// <remarks>
    /// ES2024 16.2.3: <c>export default [lookahead &#8713; { function, async function, class }]
    /// AssignmentExpression ;</c>. The declaration forms are therefore declarations, not
    /// expressions: <c>export default function f() {}</c> binds <c>f</c>, is hoisted, and ends at
    /// its closing brace, so a following <c>export</c> starts a new statement. Parsing them as
    /// expressions made the next statement a continuation of the expression.
    /// </remarks>
    private AstNode ExportDefaultDeclaration()
    {
        var current = stream.Current;
        if (current.Keyword == FastKeywords.function)
        {
            if (!FunctionExpression(out var function, isStatement: true))
                throw stream.Unexpected();

            return function;
        }

        if (current.Keyword == FastKeywords.async && AsyncFunctionDeclarationFollows())
        {
            if (!FunctionExpression(out var asyncFunction, isAsync: true, isStatement: true))
                throw stream.Unexpected();

            return asyncFunction;
        }

        if (current.Keyword == FastKeywords.@class)
        {
            if (!ClassExpression(out var @class, isStatement: true))
                throw stream.Unexpected();

            return @class;
        }

        if (!Expression(out var argument))
            throw stream.Unexpected();

        ExpectEndOfModuleItem();
        return argument;
    }

    /// <summary>
    /// Whether the current <c>async</c> begins an async function declaration: <c>async</c>
    /// [no LineTerminator here] <c>function</c>. On success <c>async</c> has been consumed and its
    /// token recorded as the start of the function's source text; otherwise nothing is consumed.
    /// </summary>
    private bool AsyncFunctionDeclarationFollows()
    {
        var asyncToken = stream.Current;
        stream.Consume();
        if (stream.Current.Keyword == FastKeywords.function)
        {
            pendingAsyncStart = asyncToken;
            return true;
        }

        stream.Reset(asyncToken);
        return false;
    }

    /// <summary>
    /// An import or export that does not end in a declaration ends with a semicolon, which only
    /// automatic semicolon insertion may supply: before a line terminator, a closing brace or the
    /// end of the module. <c>export * from 'm' null;</c> and <c>export default null, null;</c>
    /// are SyntaxErrors.
    /// </summary>
    private void ExpectEndOfModuleItem()
    {
        if (stream.Previous.Type == TokenTypes.SemiColon)
            return;

        var next = stream.Current.Type;
        if (next is TokenTypes.SemiColon or TokenTypes.LineTerminator or TokenTypes.EOF or TokenTypes.CurlyBracketEnd)
            return;

        throw stream.Unexpected();
    }
}
