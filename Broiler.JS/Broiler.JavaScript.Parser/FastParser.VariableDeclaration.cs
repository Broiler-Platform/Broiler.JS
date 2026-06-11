

using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    bool VariableDeclaration(out AstStatement node, FastVariableKind kind = FastVariableKind.Var)
    {
        var begin = stream.Current;

        node = default;
        stream.Consume();

        if (!Parameters(out var declarators, TokenTypes.SemiColon, false, kind))
            throw stream.Unexpected();

        var declaration = new AstVariableDeclaration(begin, PreviousToken, declarators, kind);
        ValidateDeclaratorInitializers(declaration);
        node = declaration;
        return true;
    }

    /// <summary>
    /// Enforces the LexicalBinding / VariableDeclaration early errors that require an
    /// Initializer: every <c>const</c> binding, and every binding that uses a
    /// destructuring BindingPattern, must have one. for-in / for-of ForBindings are
    /// exempt and are validated separately (ValidateForInOfDeclaration), so this is
    /// only applied to plain declarations and C-style for-head declarations.
    /// </summary>
    internal void ValidateDeclaratorInitializers(AstVariableDeclaration declaration)
    {
        var e = declaration.Declarators.GetFastEnumerator();
        while (e.MoveNext(out var declarator))
        {
            if (declarator.Init != null)
                continue;

            if (declaration.Kind == FastVariableKind.Const)
                throw new FastParseException(declaration.Start, "Missing initializer in const declaration");

            if (declarator.Identifier.Type is FastNodeType.ArrayPattern or FastNodeType.ObjectPattern)
                throw new FastParseException(declarator.Identifier.Start, "Missing initializer in destructuring declaration");
        }
    }

    bool VariableDeclarationStatement(out AstVariableDeclaration node, FastVariableKind kind = FastVariableKind.Var)
    {
        var begin = stream.Current;

        node = default;
        stream.Consume();

        if (!Parameters(out var declarators, TokenTypes.SemiColon, false, kind))
            throw stream.Unexpected();

        node = new AstVariableDeclaration(begin, PreviousToken, declarators, kind);
        return true;
    }


}
