using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Expressions;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=High; Resources=5; Fingerprint=TBF
// Broiler-Falsified-If: an update expression whose operand is not a valid assignment target, such as `++this` in sloppy code, is accepted and executed instead of raising the early SyntaxError
// Broiler-Human:        PENDING
public class AstUnaryExpression : AstExpression
{
    public readonly AstExpression Argument;
    public readonly UnaryOperator Operator;
    public readonly bool Prefix;

    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 s13.4.1; IP=Medium; Security=High; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: `++this` or `this++` in sloppy code passes the Identifier case and compiles to an increment of the this binding instead of raising the early SyntaxError
    // Broiler-Human:        PENDING
    public AstUnaryExpression(FastToken token, AstExpression argument, UnaryOperator tokenType, bool prefix = true)
        : base(token, FastNodeType.UnaryExpression, argument.End)
    {
        switch (tokenType)
        {
            case UnaryOperator.Increment:
            case UnaryOperator.Decrement:
                switch (argument.Type)
                {
                    case FastNodeType.Identifier:
                    case FastNodeType.MemberExpression:
                    case FastNodeType.CallExpression:
                        break;
                    default:
                        throw new FastParseException(token, $"Invalid expression for update");
                }
                break;
        }

        Argument = argument;
        Operator = tokenType;
        Prefix = prefix;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=5; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public override string ToString()
    {
        if (Prefix)
            return $"{Operator} {Argument}";

        return $"{Argument} {Operator}";
    }
}
