using System;
using System.Reflection;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private static readonly MethodInfo PrepareAnonymousFunctionNameForPropertyUIntMethod = typeof(JSVariable)
        .GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForProperty), [typeof(JSValue), typeof(uint)])
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForProperty(JSValue, uint) not found");
    private static readonly MethodInfo PrepareAnonymousFunctionNameForPropertyKeyStringMethod = typeof(JSVariable)
        .GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForProperty), [typeof(JSValue), typeof(KeyString)])
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForProperty(JSValue, KeyString) not found");
    private static readonly MethodInfo PrepareAnonymousFunctionNameForPropertyJSValueMethod = typeof(JSVariable)
        .GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForProperty), [typeof(JSValue), typeof(JSValue)])
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForProperty(JSValue, JSValue) not found");

    private static bool IsObjectLiteralProtoSetter(AstClassProperty property)
    {
        if (property.Computed || !property.UsesColon || property.Kind != AstPropertyKind.Data)
            return false;

        return property.Key switch
        {
            AstIdentifier identifier => identifier.Name.Equals("__proto__"),
            AstLiteral literal when literal.TokenType == TokenTypes.String => literal.StringValue == "__proto__",
            _ => false
        };
    }

    protected override YExpression VisitObjectLiteral(AstObjectLiteral objectExpression)
    {
        static YExpression PrepareAnonymousFunctionName(YExpression value, YExpression key)
        {
            if (key.Type == typeof(uint))
                return YExpression.Call(null, PrepareAnonymousFunctionNameForPropertyUIntMethod, value, key);

            if (key.Type.IsJSValueType())
                return YExpression.Call(null, PrepareAnonymousFunctionNameForPropertyJSValueMethod, value, key);

            return YExpression.Call(null, PrepareAnonymousFunctionNameForPropertyKeyStringMethod, value, key);
        }

        var properties = objectExpression.Properties;
        bool hasProtoSetter = false;
        var protoScan = properties.GetFastEnumerator();
        while (protoScan.MoveNext(out var propertyNode))
        {
            if (propertyNode is AstClassProperty property && IsObjectLiteralProtoSetter(property))
            {
                hasProtoSetter = true;
                break;
            }
        }

        if (!hasProtoSetter)
        {
            var elements = new Sequence<YElementInit>();
            var en = properties.GetFastEnumerator();

            while (en.MoveNext(out var pn))
            {
                switch (pn.Type)
                {
                    case FastNodeType.SpreadElement:
                        var spread = pn as AstSpreadElement;
                        elements.Add(new YElementInit(JSObjectBuilder._FastAddRange, Visit(spread.Argument)));
                        continue;

                    case FastNodeType.ClassProperty:
                        break;

                    default:
                        throw new FastParseException(pn.Start, $"Invalid token {pn.Start} in object literal");
                }

                AstClassProperty p = pn as AstClassProperty;

                YExpression key = null;
                YExpression value = null;
                var pKey = p.Key;

                value = p.Kind switch
                {
                    AstPropertyKind.Get when p.Init is AstFunctionExpression function =>
                        CreateFunction(function, inferredFunctionName: GetPropertyFunctionName(p, "get"), createPrototype: false),
                    AstPropertyKind.Set when p.Init is AstFunctionExpression function =>
                        CreateFunction(function, inferredFunctionName: GetPropertyFunctionName(p, "set"), createPrototype: false),
                    AstPropertyKind.Method or AstPropertyKind.Constructor when p.Init is AstFunctionExpression function =>
                        CreateFunction(function, createPrototype: false),
                    _ => VisitExpression(p.Init)
                };

                if (p.Computed)
                {
                    // there is a possibility of numeric index
                    var keyExp = pKey.IsUIntLiteral(out var num) ? YExpression.Constant(num) : Visit(pKey);
                    if (p.Kind is AstPropertyKind.Data or AstPropertyKind.Method or AstPropertyKind.Constructor
                        && IsAnonymousFunctionDefinition(p.Init))
                    {
                        value = PrepareAnonymousFunctionName(value, keyExp);
                    }

                    if (p.Kind == AstPropertyKind.Get)
                    {
                        elements.Add(JSObjectBuilder.AddGetter(keyExp, value));
                        continue;
                    }

                    if (p.Kind == AstPropertyKind.Set)
                    {
                        elements.Add(JSObjectBuilder.AddSetter(keyExp, value));
                        continue;
                    }

                    elements.Add(JSObjectBuilder.AddValue(keyExp, value));
                    continue;
                }

                switch (pKey.Type)
                {
                    case FastNodeType.Identifier:
                        var id = pKey as AstIdentifier;
                        if (!p.Computed)
                        {
                            key = KeyOfName(id.Name);
                        }
                        else
                        {
                            key = scope.Top.GetVariable(id.Name).Expression;
                        }
                        break;

                    case FastNodeType.Literal:
                        var l = pKey as AstLiteral;
                        key = GetLiteralPropertyKey(l);
                        break;

                    default:
                        throw new NotSupportedException();
                }

                    if (p.Kind is AstPropertyKind.Data or AstPropertyKind.Method or AstPropertyKind.Constructor
                        && IsAnonymousFunctionDefinition(p.Init))
                    {
                        value = PrepareAnonymousFunctionName(value, key);
                    }

                    switch (p.Kind)
                    {
                        case AstPropertyKind.Get:
                            elements.Add(JSObjectBuilder.AddGetter(key, value));
                        break;

                    case AstPropertyKind.Set:
                        elements.Add(JSObjectBuilder.AddSetter(key, value));
                        break;

                    default:
                        elements.Add(JSObjectBuilder.AddValue(key, value));
                        break;
                }
            }

            if (elements.Any())
            {
                var r = JSObjectBuilder.New(elements);
                return r;
            }

            return JSObjectBuilder.New();
        }

        using var temp = scope.Top.GetTempVariable(typeof(JSObject));
        var statements = new Sequence<YExpression>
        {
            YExpression.Assign(temp.Variable, JSObjectBuilder.New())
        };

        var enWithProto = properties.GetFastEnumerator();
        while (enWithProto.MoveNext(out var pn))
        {
            switch (pn.Type)
            {
                case FastNodeType.SpreadElement:
                    var spread = pn as AstSpreadElement;
                    statements.Add(JSObjectBuilder.AddRange(temp.Variable, Visit(spread.Argument)));
                    continue;

                case FastNodeType.ClassProperty:
                    break;

                default:
                    throw new FastParseException(pn.Start, $"Invalid token {pn.Start} in object literal");
            }

            AstClassProperty p = pn as AstClassProperty;

            YExpression key = null;
            YExpression value = p.Kind switch
            {
                AstPropertyKind.Get when p.Init is AstFunctionExpression function =>
                    CreateFunction(function, inferredFunctionName: GetPropertyFunctionName(p, "get"), createPrototype: false),
                AstPropertyKind.Set when p.Init is AstFunctionExpression function =>
                    CreateFunction(function, inferredFunctionName: GetPropertyFunctionName(p, "set"), createPrototype: false),
                AstPropertyKind.Method or AstPropertyKind.Constructor when p.Init is AstFunctionExpression function =>
                    CreateFunction(function, createPrototype: false),
                _ => VisitExpression(p.Init)
            };
            var pKey = p.Key;

            if (p.Computed)
            {
                var keyExp = pKey.IsUIntLiteral(out var num) ? YExpression.Constant(num) : Visit(pKey);
                if (p.Kind is AstPropertyKind.Data or AstPropertyKind.Method or AstPropertyKind.Constructor
                    && IsAnonymousFunctionDefinition(p.Init))
                {
                    value = PrepareAnonymousFunctionName(value, keyExp);
                }

                if (p.Kind == AstPropertyKind.Get)
                {
                    statements.Add(JSObjectBuilder.AddGetter(temp.Variable, keyExp, value));
                    continue;
                }

                if (p.Kind == AstPropertyKind.Set)
                {
                    statements.Add(JSObjectBuilder.AddSetter(temp.Variable, keyExp, value));
                    continue;
                }

                statements.Add(JSObjectBuilder.AddValue(temp.Variable, keyExp, value));
                continue;
            }

            switch (pKey.Type)
            {
                case FastNodeType.Identifier:
                    var id = pKey as AstIdentifier;
                    key = KeyOfName(id.Name);
                    break;

                case FastNodeType.Literal:
                    var l = pKey as AstLiteral;
                    key = GetLiteralPropertyKey(l);
                    break;

                default:
                    throw new NotSupportedException();
            }

            if (IsObjectLiteralProtoSetter(p))
            {
                statements.Add(YExpression.Assign(JSValueBuilder.Index(temp.Variable, key), value));
                continue;
            }

            if (p.Kind is AstPropertyKind.Data or AstPropertyKind.Method or AstPropertyKind.Constructor
                && IsAnonymousFunctionDefinition(p.Init))
            {
                value = PrepareAnonymousFunctionName(value, key);
            }

            switch (p.Kind)
            {
                case AstPropertyKind.Get:
                    statements.Add(JSObjectBuilder.AddGetter(temp.Variable, key, value));
                    break;

                case AstPropertyKind.Set:
                    statements.Add(JSObjectBuilder.AddSetter(temp.Variable, key, value));
                    break;

                default:
                    statements.Add(JSObjectBuilder.AddValue(temp.Variable, key, value));
                    break;
            }
        }

        statements.Add(temp.Variable);
        return YExpression.Block(new Sequence<YParameterExpression> { temp.Variable }, statements);
    }
}
