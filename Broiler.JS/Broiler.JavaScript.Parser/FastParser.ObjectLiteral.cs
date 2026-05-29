using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    FastToken lastObjectPropertyIndex;

    bool ObjectProperty(out AstClassProperty property, bool checkContextualKeyword = true, bool isAsync = false, bool isClass = false)
    {
        PreventStackoverFlow(ref lastObjectPropertyIndex);

        var begin = BeginUndo();
        var current = begin.Token;

        var isStatic = isClass ? stream.CheckAndConsume(FastKeywords.@static) : false;


        // Check for async methods first. `async get foo()` / `async set foo()` remain
        // invalid ECMAScript syntax; `async get()` / `async set()` are async methods
        // whose property names happen to be `get` / `set`.
        if (stream.CheckAndConsume(FastKeywords.async))
        {
            if (ObjectProperty(out property, true, isClass: isClass, isAsync: true))
            {
                if (property.Kind == AstPropertyKind.Get || property.Kind == AstPropertyKind.Set)
                    throw stream.Unexpected();

                property = new AstClassProperty(current, property.End, AstPropertyKind.Method, property.IsPrivate, isStatic, property.Key, property.Computed, property.Init, property.UsesColon, property.UsesAssign);
                return true;
            }

            begin.Reset();
        }

        stream.SkipNewLines();

        var sc = stream.Current;
        var isGet = sc.ContextualKeyword == FastKeywords.get;
        var isSet = sc.ContextualKeyword == FastKeywords.set;

        bool isGenerator = stream.CheckAndConsume(TokenTypes.Multiply);
        if (PropertyName(out var key, out var computed, out var isPrivate, acceptKeywords: true))
        {
            if (checkContextualKeyword && (isSet || isGet))
            {
                if (ObjectProperty(out property, isClass: isClass, isAsync: isAsync))
                {
                    property = new AstClassProperty(current, property.End, isSet ? AstPropertyKind.Set : AstPropertyKind.Get, property.IsPrivate, isStatic, property.Key, property.Computed, property.Init, property.UsesColon, property.UsesAssign);
                    return true;
                }
            }

            stream.SkipNewLines();

            if (stream.CheckAndConsume(TokenTypes.Assign))
            {
                if (!checkContextualKeyword)
                    throw stream.Unexpected();

                if (!Expression(out var value))
                    throw stream.Unexpected();

                property = new AstClassProperty(current, PreviousToken, AstPropertyKind.Data, isPrivate, isStatic, key, computed, value, usesAssign: true);
                stream.CheckAndConsume(TokenTypes.SemiColon);

                return true;
            }

            if (stream.CheckAndConsume(TokenTypes.Colon))
            {
                if (!checkContextualKeyword)
                    throw stream.Unexpected();

                if (!Expression(out var value))
                    throw stream.Unexpected();

                property = new AstClassProperty(current, PreviousToken, AstPropertyKind.Data, isPrivate, isStatic, key, computed, value, usesColon: true);
                return true;
            }
            else if (stream.CheckAndConsume(TokenTypes.BracketStart))
            {
                // add the scope...
                var scope = variableScope.Push(PreviousToken, FastNodeType.FunctionExpression);
                try
                {

                    if (!Parameters(out var parameters, checkForBracketStart: false))
                        throw stream.Unexpected();

                    if (!Statement(out var body))
                        throw stream.Unexpected();

                    if (body.Type != FastNodeType.Block)
                        throw stream.Unexpected();

                    var fx = new AstFunctionExpression(current, PreviousToken, false, isAsync, isGenerator, null, parameters, body);

                    var isConstructor = !computed
                        && !isPrivate
                        && !isStatic
                        && (key is AstIdentifier keyIdentifier && keyIdentifier.Name.Value == "constructor"
                            || key is AstLiteral { TokenType: TokenTypes.String, StringValue: "constructor" });
                    property = new AstClassProperty(current, PreviousToken, isConstructor ? AstPropertyKind.Constructor : AstPropertyKind.Method,
                        isPrivate, isStatic, key, computed, fx);
                    return true;
                }
                finally
                {
                    scope.Dispose();
                }
            }
            else if (stream.Current.Type == TokenTypes.Comma || stream.Current.Type == TokenTypes.CurlyBracketEnd || stream.Current.Type == TokenTypes.EOF)
            {
                if (computed || key is AstLiteral)
                    throw stream.Unexpected();
                property = new AstClassProperty(current, PreviousToken, AstPropertyKind.Data, isPrivate, isStatic, key, computed, key);
                return true;
            }
            else throw stream.Unexpected();
        }

        property = default;
        return begin.Reset();
    }

    bool ObjectLiteral(out AstExpression node)
    {
        var begin = stream.Current;
        node = default;
        stream.Consume();

        var nodes = new Sequence<AstNode>();
        SkipNewLines();

        while (!stream.CheckAndConsumeAny(TokenTypes.CurlyBracketEnd, TokenTypes.EOF))
        {
            SkipNewLines();
            var current = stream.Current;

            if (stream.CheckAndConsume(TokenTypes.TripleDots))
            {
                if (!Expression(out var exp))
                    throw stream.Unexpected();

                nodes.Add(new AstSpreadElement(current, exp.End, exp));
                continue;
            }

            if (ObjectProperty(out var property))
                nodes.Add(property);

            if (stream.CheckAndConsume(TokenTypes.Comma))
                continue;
        }

        node = new AstObjectLiteral(begin, PreviousToken, nodes);
        return true;
    }
}
