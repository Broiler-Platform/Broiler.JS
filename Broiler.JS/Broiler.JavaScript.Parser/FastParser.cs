
using Broiler.JavaScript.Ast.Misc;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Parser;


public partial class FastParser(FastTokenStream stream) : IParser
{
    private readonly FastTokenStream stream = stream;

    public readonly FastScope variableScope = new FastScope();

    /// <summary>
    /// Disable this inside for brackets...
    /// </summary>
    private bool considerInOfAsOperators = true;
    private bool isAsync = false;

    // True while parsing the immediate top-level StatementList of a Script (the program's
    // root block). A `using` / `await using` declaration is an early SyntaxError there: it
    // is only permitted when contained — directly or indirectly — within a Block,
    // ForStatement, ForInOfStatement, or a function/class body. Entering any nested Block
    // (a block statement, function body, try/catch/finally, …) clears it. A Module top-level
    // does permit `using`, but the parser only parses the Script goal here.
    private bool atScriptTopLevel = false;

    // Tracks whether the parser is inside a generator / async function body, which
    // makes `yield` / `await` keywords. Assigning these also informs the scanner so
    // it disambiguates a following `/` as a regex literal (keyword form) rather than
    // division (identifier form) — see FastScanner.YieldIsKeyword / AwaitIsKeyword.
    private bool _inGeneratorBody = false;
    private bool inGeneratorBody
    {
        get => _inGeneratorBody;
        set { _inGeneratorBody = value; stream.YieldIsKeyword = value; }
    }

    private bool inAsyncFunctionBody = false;

    // Set while parsing the FormalParameters of the current function (cleared on
    // entry to its body and on entry to any nested function's parameters/body). The
    // FormalParameters of a generator must not contain a YieldExpression, and those
    // of an async function must not contain an AwaitExpression — both are early
    // (Syntax) errors. Combined with inGeneratorBody / inAsyncFunctionBody (which a
    // nested non-generator/non-async function resets), this fires only for a yield/
    // await that genuinely belongs to the enclosing generator/async parameter list.
    private bool inFormalParameters = false;

    // The `async` keyword token of an async function declaration/expression whose
    // `async` was just consumed by the caller, so FunctionExpression can anchor its
    // source span (Function.prototype.toString) at `async` rather than `function`.
    // Only consulted when the function being built is itself async (set right before
    // the matching FunctionExpression call), and cleared on entry to FunctionExpression.
    private FastToken pendingAsyncStart = null;

    private int functionDepth = 0;

    // Set while parsing a FunctionDeclaration that is the sole statement of an
    // `if` clause (Annex B.3.4). Such a declaration is scoped to its own implicit
    // block, so its name must not register a lexical binding in — or conflict
    // with one in — the enclosing block/switch scope.
    private bool nestedFunctionClause = false;

    // Set while parsing the body of an `if`/`while`/`do`/`for`/`with`/label clause,
    // whose grammar is a single Statement (not a StatementListItem). A LexicalDeclaration
    // (`let`/`const`/`class`) is not a Statement, so it is a SyntaxError there — and a
    // `let` followed by `{`/identifier across a line terminator is just an `IdentifierReference`
    // expression statement (ASI), not a declaration. Consumed (cleared) by Statement.
    private bool singleStatementContext = false;

    public StreamLocation BeginUndo() => new(this, stream.Current);

    public StreamLocation Location
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new StreamLocation(this, stream.Current);
    }

    public FastToken PreviousToken
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stream.Previous;
    }

    public readonly struct StreamLocation(FastParser parser, FastToken token)
    {
        public readonly FastToken Token = token;

        public bool Reset()
        {
            parser.stream.Reset(Token);
            return false;
        }
    }

    public AstProgram ParseProgram()
    {
        if (Program(out var p))
            return p;

        throw stream.Unexpected();
    }

    bool EndOfLine()
    {
        var token = stream.Current;

        if (token.Type == TokenTypes.LineTerminator)
        {
            stream.Consume();
            return true;
        }

        return false;
    }

    bool EndOfStatement()
    {
        var token = stream.Current;

        switch (token.Type)
        {
            case TokenTypes.SemiColon:
            case TokenTypes.EOF:
            case TokenTypes.LineTerminator:
                stream.Consume();
                return true;

            // since Block will expect curly bracket
            // to be present, we will not consume this..
            case TokenTypes.CurlyBracketEnd:
                return true;
        }

        return false;
    }
}
