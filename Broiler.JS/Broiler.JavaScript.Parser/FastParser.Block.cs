
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SkipNewLines()
    {
        var type = stream.Current.Type;

        while (type == TokenTypes.LineTerminator)
            type = stream.Consume().Type;
    }

    bool Block(out AstBlock node, bool isProgramRoot = false)
    {
        var begin = stream.Current;
        var list = new Sequence<AstStatement>();
        var scope = variableScope.Push(begin, FastNodeType.Block);

        // Only the Script's root StatementList is the script top level; any nested Block
        // (this method called for a block statement, a function body, a try/catch clause, …)
        // is a `using`-permitting container, so clear the flag while parsing its statements.
        var previousScriptTopLevel = atScriptTopLevel;
        atScriptTopLevel = isProgramRoot;

        // A Script/eval program's StatementList ends at EOF; every other Block is
        // brace-delimited (the caller has consumed the opening `{`) and ends at its
        // matching `}`. Accepting the wrong terminator is a syntax error: a stray `}`
        // at the top level (`eval("}")`) or an unterminated block reaching EOF
        // (`eval("{")`) must both throw, rather than parse as an empty/closed program.
        var terminator = isProgramRoot ? TokenTypes.EOF : TokenTypes.CurlyBracketEnd;

        try
        {
            do
            {
                if (stream.CheckAndConsume(terminator))
                    break;

                if (Statement(out var stmt))
                {
                    list.Add(stmt);
                    continue;
                }

                if (stream.CheckAndConsumeWithLineTerminator(TokenTypes.SemiColon))
                    continue;

                if (stream.CheckAndConsume(terminator))
                    break;

                throw stream.Unexpected();
            } while (true);

            node = new AstBlock(begin, PreviousToken, list)
            {
                HoistingScope = scope.GetVariables(),
                AnnexBFunctionNames = scope.GetAnnexBNames()
            };
        }
        finally
        {
            atScriptTopLevel = previousScriptTopLevel;
            scope.Dispose();
        }

        return true;
    }
}
