// Moved from Broiler.JavaScript.Ast with its namespace at window W2 of milestone SJL-14 (decision D50: a non-tree type,
// whose new home is Broiler.JavaScript.Expressions); Ast keeps a type forwarder to it, and it is not marked obsolete. It is
// compiled in a nullable context, as Ast compiled it, so that its public signatures read as they did.
#nullable enable

namespace Broiler.JavaScript.Ast.Misc;

/// <summary>
/// Tokens should be arranged in order of precedence
/// </summary>
public enum TokenTypes
{
    Empty = 0,
    SquareBracketStart,
    SquareBracketEnd,
    CurlyBracketStart,
    CurlyBracketEnd,
    TemplateBegin,
    TemplatePart,
    TemplateEnd,
    LineTerminator,
    None,
    SemiColon,
    EOF,
    Identifier,
    Number,
    BigInt,
    Decimal,
    String,
    BracketEnd,
    BracketStart,
    Lambda,

    //not used
    BeginAssignTokens,

    Assign,
    AssignMultiply,
    AssignDivide,
    AssignMod,
    AssignAdd,
    AssignXor,
    AssignSubtract,
    AssignUnsignedRightShift,
    AssignBitwideAnd,
    AssignBitwideOr,
    AssignRightShift,
    AssignLeftShift,
    AssignCoalesce,
    AssignBooleanAnd,
    AssignBooleanOr,
    AssignPower,

    // not used...
    EndAssignTokens,

    BeginOperators,

    Negate,
    Power,
    Multiply,
    Divide,
    Mod,
    Plus,
    Minus,
    LeftShift,
    RightShift,
    UnsignedRightShift,
    Less,
    LessOrEqual,
    Greater,
    GreaterOrEqual,
    In,
    InstanceOf,
    Equal,
    NotEqual,
    StrictlyEqual,
    StrictlyNotEqual,
    Coalesce,
    BitwiseAnd,
    Xor,
    BitwiseOr,
    BooleanAnd,
    BooleanOr,
    QuestionMark,
    Colon,
    BitwiseNot,
    QuestionDot,
    Dot,
    TripleDots,

    EndOperators,

    Increment,
    Decrement,

    Comma,

    Null,
    False,
    True,
    Hash,
    RegExLiteral,
    OptionalCall,
    OptionalIndex,
    At,
}
