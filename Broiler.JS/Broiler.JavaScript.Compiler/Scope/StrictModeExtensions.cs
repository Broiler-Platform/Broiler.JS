using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Compiler;

internal static class StrictModeExtensions
{
    public static void VerifyIdentifierForUpdate(this AstIdentifier id, bool isStrictMode)
    {
        if (!isStrictMode)
            return;

        if (id.Name.Equals("arguments") || id.Name.Equals("eval") || id.Name.Equals("this"))
            throw new FastParseException(id.Start, $"Invalid left-hand side expression for update");
    }

    public static void VerifyIdentifierForUpdate(this AstExpression expression, bool isStrictMode)
    {
        switch (expression.Type)
        {
            case FastNodeType.Identifier:
                VerifyIdentifierForUpdate(expression as AstIdentifier, isStrictMode);
                return;
        }
    }
}
