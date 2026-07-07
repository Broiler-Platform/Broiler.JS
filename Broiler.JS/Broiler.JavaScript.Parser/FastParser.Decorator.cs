using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Parser;

partial class FastParser
{
    // Parses (and discards) a DecoratorList preceding a class declaration / class
    // expression / class element. Returns true if at least one Decorator was consumed.
    //
    // Decorator runtime semantics (calling the decorator, replacing the value,
    // addInitializer, accessor/field/method contexts) are NOT implemented; the syntax
    // is accepted so that decorated classes and class elements parse and evaluate as
    // their undecorated form. The grammar is enforced precisely so that invalid
    // decorator syntax still raises a SyntaxError.
    //
    //   Decorator :
    //     @ DecoratorMemberExpression
    //     @ DecoratorParenthesizedExpression
    //     @ DecoratorCallExpression
    //   DecoratorMemberExpression :
    //     IdentifierReference
    //     DecoratorMemberExpression . IdentifierName
    //     DecoratorMemberExpression . PrivateIdentifier
    //   DecoratorCallExpression :
    //     DecoratorMemberExpression Arguments
    //   DecoratorParenthesizedExpression :
    //     ( Expression )
    bool Decorators()
    {
        if (stream.Current.Type != TokenTypes.At)
            return false;

        while (stream.Current.Type == TokenTypes.At)
        {
            stream.Consume(); // @
            stream.SkipNewLines();
            Decorator();
            stream.SkipNewLines();
        }

        return true;
    }

    void Decorator()
    {
        // @ DecoratorParenthesizedExpression
        if (stream.CheckAndConsume(TokenTypes.BracketStart))
        {
            if (!Expression(out _))
                throw stream.Unexpected();

            stream.Expect(TokenTypes.BracketEnd);
            return;
        }

        // @ DecoratorMemberExpression  ( IdentifierReference, then `.` chains )
        if (!Identitifer(out _))
            throw stream.Unexpected();

        while (stream.Current.Type == TokenTypes.Dot)
        {
            stream.Consume(); // .

            if (stream.CheckAndConsume(TokenTypes.Hash))
            {
                // . PrivateIdentifier
                if (!Identitifer(out _))
                    throw stream.Unexpected();

                continue;
            }

            // . IdentifierName (any identifier, including reserved words)
            var name = stream.Current;
            if (name.Type is TokenTypes.Identifier or TokenTypes.In or TokenTypes.InstanceOf
                    or TokenTypes.Null or TokenTypes.True or TokenTypes.False
                || name.IsKeyword)
            {
                stream.Consume();
                continue;
            }

            throw stream.Unexpected();
        }

        // @ DecoratorCallExpression  ( a single trailing Arguments list )
        if (stream.Current.Type == TokenTypes.BracketStart)
        {
            stream.Consume(); // (
            if (!ArrayExpression(out _))
                throw stream.Unexpected();
        }
    }
}
