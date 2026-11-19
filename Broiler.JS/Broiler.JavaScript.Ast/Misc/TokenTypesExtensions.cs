using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Ast.Misc;

// Stays in Broiler.JavaScript.Ast, obsolete with the tree, when TokenTypes moves to Broiler.JavaScript.Expressions at window
// W2 of milestone SJL-14 (dispositions.md: "TokenTypesExtensions stays in Ast, obsolete with the tree: nothing outside
// the parser and the compiler calls it"). It was declared beside TokenTypes until then.
[System.Obsolete(ObsoleteMarks.Ast, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public static class TokenTypesExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOperator(this TokenTypes type) => type > TokenTypes.BeginOperators && type < TokenTypes.EndOperators;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAssignmentOperator(this TokenTypes type) => type > TokenTypes.BeginAssignTokens && type < TokenTypes.EndAssignTokens;
}
