// Moved from Broiler.JavaScript.Ast with its namespace at window W2 of milestone SJL-14 (decision D50: a non-tree type,
// whose new home is Broiler.JavaScript.Expressions); Ast keeps a type forwarder to it, and it is not marked obsolete. It is
// compiled in a nullable context, as Ast compiled it, so that its public signatures read as they did.
#nullable enable

namespace Broiler.JavaScript.Ast.Misc;

public enum UnaryOperator
{
    Plus,
    Minus,
    Increment,
    Decrement,
    Negate,
    BitwiseNot,
    @typeof,
    @delete,
    @void,
    @new,
    None
}
