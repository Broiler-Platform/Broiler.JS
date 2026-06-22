using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private BExpression CreatePropertyKeyExpression(AstExpression property, bool computed)
    {
        switch (property.Type)
        {
            case FastNodeType.Identifier:
                var id = (AstIdentifier)property;
                if (computed)
                    return VisitIdentifier(id);
                // `.#x` is a private member reference; key it in the private namespace.
                return id.Name.Length > 0 && id.Name.Value[0] == '#'
                    ? KeyOfPrivateName(id.Name)
                    : KeyOfName(id.Name);

            case FastNodeType.Literal:
                var l = (AstLiteral)property;
                switch (l.TokenType)
                {
                    case TokenTypes.True:
                        return BExpression.Constant(1);

                    case TokenTypes.False:
                        return BExpression.Constant(0);

                    case TokenTypes.String:
                        return computed ? VisitLiteral(l) : KeyOfName(l.Start.CookedText);

                    case TokenTypes.Number:
                        if (l.NumericValue >= 0 && (l.NumericValue % 1 == 0))
                            return BExpression.Constant((uint)l.NumericValue);

                        return VisitLiteral(l);

                    case TokenTypes.BigInt:
                        // A BigInt destructuring key (`let { 1n: a } = o`) keys by the
                        // canonical value of its numeric part.
                        return BigIntPropertyKey(l.StringValue);

                    default:
                        throw new NotImplementedException();
                }

            case FastNodeType.MemberExpression:
                var se = (AstMemberExpression)property;
                return Visit(se.Property);
        }

        if (computed)
            return Visit(property);

        throw new NotImplementedException();
    }

    private BExpression CreateMemberExpression(BExpression target, AstExpression property, bool computed)
    {
        var key = CreatePropertyKeyExpression(property, computed);
        if (key.Type == typeof(KeyString) || key.Type == typeof(uint) || key.Type == typeof(int) || key.Type.IsJSValueType())
            return JSValueBuilder.Index(target, key);

        throw new NotImplementedException();
    }
}
