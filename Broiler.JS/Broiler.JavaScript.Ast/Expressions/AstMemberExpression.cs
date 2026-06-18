using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Expressions;

public class AstMemberExpression(AstExpression target, AstExpression node, bool computed = false, bool coalesce = false, bool inOptionalChain = false) :
    AstExpression(target.End, FastNodeType.MemberExpression, node.End)
{
    public readonly AstExpression Object = target;
    public readonly AstExpression Property = node;
    public readonly bool Computed = computed;

    // Coalesce: this specific link is a `?.` (short-circuits on a nullish base).
    // InOptionalChain: this link sits inside an optional chain (some link — possibly this
    // one — used `?.`), so it must propagate an in-flight short-circuit even when it is not
    // itself optional. That distinction is what makes `a?.b.c` short-circuit to undefined
    // when `a` is nullish yet throw when `a.b` is a genuine undefined.
    public readonly bool Coalesce = coalesce;
    public readonly bool InOptionalChain = inOptionalChain || coalesce;

    public override string ToString()
    {
        if (Computed)
            return $"{Object}[{Property}]";
    
        if (Coalesce)
            return $"{Object}?.{Property}";
        
        return $"{Object}.{Property}";
    }
}
