using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Ast.Statements;


public class AstExportStatement : AstStatement
{
    public readonly AstNode? Declaration;
    public readonly bool IsDefault;
    public readonly bool ExportAll;
    public readonly AstNode? Source;

    /// <summary>
    /// The specifiers of a NamedExports clause — <c>export { a, b as c }</c>, and the same with a
    /// <c>from</c> clause. Each pair is (the LOCAL name, the name it is exported under); for a
    /// re-export the local name is the name imported from <see cref="Source"/>. Null for every
    /// other export form.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>AstImportStatement.Members</c> deliberately: the two clauses have the same shape
    /// in the grammar, and reusing it keeps the parser's two clause readers symmetric. A clause is
    /// NOT a declaration — <c>export { x }</c> exports an existing binding and introduces nothing —
    /// which is why it needs its own representation rather than the variable declaration the
    /// parser used to build for it.
    /// </remarks>
    public readonly IFastEnumerable<(StringSpan name, StringSpan asName)>? Members;

    /// <summary>
    /// The <c>with { … }</c> clause of a re-export, or null when there is none.
    /// </summary>
    /// <remarks>
    /// An ExportDeclaration with a FromClause takes a WithClause exactly as an ImportDeclaration
    /// does, and it means the same thing: the module named by <see cref="Source"/> is loaded, so the
    /// attribute constrains that load. It is carried here for the same reason
    /// <c>AstImportStatement.Attributes</c> is — so the compiler can hand it to the module host,
    /// which is what makes the attribute enforced rather than parsed and dropped.
    /// </remarks>
    public readonly IFastEnumerable<(StringSpan key, AstLiteral value)>? Attributes;

    /// <summary>A NamedExports clause, with <paramref name="source"/> for a re-export.</summary>
    public AstExportStatement(FastToken token, IFastEnumerable<(StringSpan, StringSpan)> members, AstLiteral? source, FastToken end,
        IFastEnumerable<(StringSpan, AstLiteral)>? attributes = null)
        : base(token, FastNodeType.ExportStatement, end)
    {
        Attributes = attributes;
        Members = members;
        Source = source;
        Declaration = null;
        IsDefault = false;
        ExportAll = false;
    }

    public AstExportStatement(FastToken token, AstNode argument, bool IsDefault = false) : base(token, FastNodeType.ExportStatement, argument.End)
    {
        Declaration = argument;
        this.IsDefault = IsDefault;
        Source = null;
    }

    public AstExportStatement(FastToken token, AstNode? argument, AstNode source,
        IFastEnumerable<(StringSpan, AstLiteral)>? attributes = null) : base(token, FastNodeType.ExportStatement, source.End)
    {
        Attributes = attributes;
        Declaration = argument;
        IsDefault = false;
        ExportAll = argument == null;
        Source = source;
    }
}
