using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast;


// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
// Broiler-Falsified-If: a string literal containing an escape such as `'\x41'` reads back from StringValue as its raw source text instead of `A`
// Broiler-Human:        PENDING
public class AstLiteral(TokenTypes tokenType, FastToken token) : AstExpression(token, FastNodeType.Literal, token)
{
    public readonly TokenTypes TokenType = tokenType;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a numeric literal reads back a value other than the one its token parsed, such as `0x10` yielding 0
    // Broiler-Human:        PENDING
    public double NumericValue => Start.Number;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: a string literal containing an escape such as `'\x41'` reads back from StringValue as its raw source text instead of `A`
    // Broiler-Human:        PENDING
    public string StringValue => Start.CookedText ?? Start.Span.Value;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a regex literal `/a+/g` yields a pattern other than `a+` or flags other than `g`, such as the pattern with its delimiting slashes
    // Broiler-Human:        PENDING
    public (string Pattern, string Flags) Regex => (Start.CookedText, Start.Flags);

    public override string ToString() => TokenType.ToString();
}

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
// Broiler-Falsified-If: a `super` node reports a Type other than FastNodeType.Super, so `super.x` outside a method is not rejected as an early SyntaxError
// Broiler-Human:        PENDING
public class AstSuper(FastToken token) : AstExpression(token, FastNodeType.Super, token)
{
    public readonly TokenTypes TokenType;

    public override string ToString() => "super";
}
