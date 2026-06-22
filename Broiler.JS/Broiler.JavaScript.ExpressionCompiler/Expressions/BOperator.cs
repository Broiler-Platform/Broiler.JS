#nullable enable

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public enum BOperator
{
    Add,
    Subtract,
    Multipley,
    Divide,
    Mod,
    Power,

    Xor,
    BitwiseAnd,
    BitwiseOr,
    BooleanAnd,
    BooleanOr,

    Less,
    LessOrEqual,
    Greater,
    GreaterOrEqual,
    Equal,
    NotEqual,

    LeftShift,
    RightShift,
    UnsignedRightShift
}
