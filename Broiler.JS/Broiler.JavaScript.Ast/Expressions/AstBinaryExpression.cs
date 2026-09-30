using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Expressions;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=5; Fingerprint=TBF
// Broiler-Human:        PENDING
public class AstBinaryExpression(AstExpression node, TokenTypes type, AstExpression right) : AstExpression(node.Start, FastNodeType.BinaryExpression, right.End)
{
    public readonly AstExpression Left = node;
    public readonly TokenTypes Operator = type;
    public readonly AstExpression Right = right;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static string OperatorToString(TokenTypes type) => type switch
    {
        TokenTypes.BooleanAnd => "&&",
        TokenTypes.BooleanOr => "||",
        TokenTypes.BitwiseAnd => "&",
        TokenTypes.BitwiseOr => "|",
        TokenTypes.Plus => "+",
        TokenTypes.Minus => "-",
        TokenTypes.Mod => "%",
        TokenTypes.Multiply => "*",
        TokenTypes.NotEqual => "!=",
        TokenTypes.Equal => "==",
        TokenTypes.StrictlyNotEqual => "!==",
        TokenTypes.StrictlyEqual => "===",
        TokenTypes.Assign => "=",
        TokenTypes.AssignBooleanAnd => "&&=",
        TokenTypes.AssignBooleanOr => "||=",
        TokenTypes.AssignCoalesce => "??=",
        _ => type.ToString(),
    };

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=5; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public override string ToString() => $"({Left} {OperatorToString(Operator)} {Right})";
}
