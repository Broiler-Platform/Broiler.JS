using System;
using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using System.Reflection;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private static string FormatNumericPropertyName(double value)
    {
        if (double.IsNaN(value))
            return nameof(double.NaN);

        if (value == 0)
            return "0";

        if (value > 0 && (uint)value == value)
            return ((uint)value).ToString();

        return JSValue.NumberToECMAString(value);
    }

    private static string FormatLiteralPropertyName(AstLiteral literal)
    {
        if (literal.TokenType == TokenTypes.String)
            return literal.StringValue;

        return FormatNumericPropertyName(literal.NumericValue);
    }

    private YExpression GetLiteralPropertyKey(AstLiteral literal)
    {
        if (literal.TokenType == TokenTypes.String)
        {
            if (NumberParser.TryGetArrayIndex(literal.StringValue, out var ui))
                return YExpression.Constant(ui);

            return KeyOfName(literal.StringValue);
        }

        if (literal.TokenType == TokenTypes.Number)
        {
            var value = literal.NumericValue;
            if (value == 0 || (value > 0 && value <= uint.MaxValue && value % 1 == 0))
                return YExpression.Constant((uint)value);

            return VisitLiteral(literal);
        }

        throw new NotSupportedException();
    }

    private static string GetPropertyFunctionName(AstClassProperty property, string prefix = null)
    {
        if (property.Computed)
            return null;

        string name = property.Key switch
        {
            AstIdentifier id => id.Name.Value,
            AstLiteral literal when literal.TokenType == TokenTypes.String || literal.TokenType == TokenTypes.Number => FormatLiteralPropertyName(literal),
            _ => null
        };

        if (name == null)
            return null;

        return prefix == null ? name : $"{prefix} {name}";
    }

    private static readonly MethodInfo ValidateClassStaticPropertyNameKeyStringMethod = typeof(JSClassStaticPropertyValidator)
        .PublicMethod(nameof(JSClassStaticPropertyValidator.Validate), KeyStringsBuilder.RefType)
        ?? throw new InvalidOperationException("JSClassStaticPropertyValidator.Validate(KeyString) not found");
    private static readonly MethodInfo ValidateClassStaticPropertyNameJSValueMethod = typeof(JSClassStaticPropertyValidator)
        .PublicMethod(nameof(JSClassStaticPropertyValidator.Validate), typeof(JSValue))
        ?? throw new InvalidOperationException("JSClassStaticPropertyValidator.Validate(JSValue) not found");

    private YExpression GetName(AstClassProperty property)
    {
        var exp = property.Key;
        var computed = property.Computed;

        switch ((exp.Type, exp))
        {
            case (FastNodeType.Identifier, AstIdentifier id):
                if (computed)
                    return VisitIdentifier(id);
                return KeyOfName(id.Name);

            case (FastNodeType.Literal, AstLiteral l):
                if (computed)
                    return VisitLiteral(l);
                return GetLiteralPropertyKey(l);

            default:
                return Visit(exp);
        }
    }

    private static YExpression ValidateStaticPropertyName(AstClassProperty property, YExpression name)
    {
        if (!property.IsStatic)
            return name;

        return name.Type switch
        {
            var type when type == typeof(KeyString) => YExpression.Call(null, ValidateClassStaticPropertyNameKeyStringMethod, name),
            var type when type == typeof(JSValue) => YExpression.Call(null, ValidateClassStaticPropertyNameJSValueMethod, name),
            _ => name,
        };
    }

    private YExpression CreateClass(AstIdentifier id, AstExpression super, AstClassExpression body)
    {
        var scope = pool.NewScope();
        var tempVar = this.scope.Top.GetTempVariable(JSClassBuilder.Type);

        var prototypeElements = new Sequence<YElementInit>();
        var staticElements = new Sequence<YBinding>();
        var staticBlocks = new Sequence<AstClassProperty>();

        // need to save super..
        // create a super variable...
        YExpression superExp;
        if (super != null)
        {
            superExp = VisitExpression(super);
        }
        else
        {
            superExp = JSContextBuilder.Object;
        }

        var superVar = YExpression.Parameter(typeof(JSValue));
        var superPrototypeVar = YExpression.Parameter(typeof(JSObject));

        var stmts = new Sequence<YExpression>(body.Members.Count)
        {
            YExpression.Assign(superVar, superExp),
            YExpression.Assign(superPrototypeVar, JSClassBuilder.ResolveSuperclassPrototype(superVar))
        };

        YExpression retValue = tempVar.Variable;

        var memberInits = new Sequence<AstClassProperty>();
        AstFunctionExpression constructor = null;
        var directEvalPrivateNames = CollectPrivateNames(body.Members);

        var en = body.Members.GetFastEnumerator();
        while (en.MoveNext(out var property))
        {
            var isPrivateName = property.Key is AstIdentifier propertyIdentifier && propertyIdentifier.Name.Value.StartsWith("#");
            YExpression name;
            // var el = property.IsStatic ? staticElements : prototypeElements;
            switch (property.Kind)
            {
                case AstPropertyKind.Init:
                    staticBlocks.Add(property);
                    break;

                case AstPropertyKind.Data:
                    if (property.IsStatic)
                    {
                        name = ValidateStaticPropertyName(property, GetName(property));
                        var value = property.Init == null ? JSUndefinedBuilder.Value : Visit(property.Init);
                        var attributes = isPrivateName
                            ? JSPropertyAttributes.ConfigurableValue
                            : JSPropertyAttributes.EnumerableConfigurableValue;
                        staticElements.Add(JSObjectBuilder.AddValue(name, value, attributes));
                        break;
                    }
                    memberInits.Add(property);
                    break;

                case AstPropertyKind.Get:
                    name = ValidateStaticPropertyName(property, GetName(property));
                    if (property.IsStatic)
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, superVar, forceStrictMode: true,
                            inferredFunctionName: GetPropertyFunctionName(property, "get"), createPrototype: false, directEvalPrivateNames: directEvalPrivateNames);
                        staticElements.Add(JSObjectBuilder.AddGetter(name, fx, JSPropertyAttributes.ConfigurableProperty));
                        break;
                    }
                    else
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, superPrototypeVar, forceStrictMode: true,
                            inferredFunctionName: GetPropertyFunctionName(property, "get"), createPrototype: false, directEvalPrivateNames: directEvalPrivateNames);
                        prototypeElements.Add(JSObjectBuilder.AddGetter(name, fx, JSPropertyAttributes.ConfigurableProperty));
                    }
                    break;

                case AstPropertyKind.Set:
                    name = ValidateStaticPropertyName(property, GetName(property));
                    if (property.IsStatic)
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, superVar, forceStrictMode: true,
                            inferredFunctionName: GetPropertyFunctionName(property, "set"), createPrototype: false, directEvalPrivateNames: directEvalPrivateNames);
                        staticElements.Add(JSObjectBuilder.AddSetter(name, fx, JSPropertyAttributes.ConfigurableProperty));
                    }
                    else
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, superPrototypeVar, forceStrictMode: true,
                            inferredFunctionName: GetPropertyFunctionName(property, "set"), createPrototype: false, directEvalPrivateNames: directEvalPrivateNames);
                        prototypeElements.Add(JSObjectBuilder.AddSetter(name, fx, JSPropertyAttributes.ConfigurableProperty));
                    }
                    break;

                case AstPropertyKind.Constructor:
                    constructor = property.Init as AstFunctionExpression;
                    break;

                case AstPropertyKind.Method:
                    name = ValidateStaticPropertyName(property, GetName(property));
                    if (property.IsStatic)
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, superVar, forceStrictMode: true,
                            inferredFunctionName: GetPropertyFunctionName(property), createPrototype: false, directEvalPrivateNames: directEvalPrivateNames);
                        staticElements.Add(JSObjectBuilder.AddValue(name, fx, isPrivateName ? JSPropertyAttributes.ConfigurableReadonlyValue : JSPropertyAttributes.ConfigurableValue));
                    }
                    else
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, superPrototypeVar, forceStrictMode: true,
                            inferredFunctionName: GetPropertyFunctionName(property), createPrototype: false, directEvalPrivateNames: directEvalPrivateNames);
                        prototypeElements.Add(JSObjectBuilder.AddValue(name, fx, isPrivateName ? JSPropertyAttributes.ConfigurableReadonlyValue : JSPropertyAttributes.ConfigurableValue));
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        var className = id?.Name.Value;

        if (constructor != null)
        {
            var fx = CreateFunction(constructor, superVar, true, className, memberInits, true, directEvalPrivateNames: directEvalPrivateNames);
            staticElements.Add(JSClassBuilder.AddConstructor(fx));
        }
        else
        {
            if (memberInits.Any())
            {
                using var s = this.scope.Push(new FastFunctionScope(null, null, memberInits: memberInits));
                var args = s.Arguments;
                var @this = s.ThisExpression;
                var inits = new Sequence<YExpression>() { };

                inits.AddRange(s.InitList);
                inits.Add(YExpression.Assign(@this, JSFunctionBuilder.InvokeFunction(superVar, args)));

                InitMembers(inits, s);
                inits.Add(@this);

                var lambda = YExpression.Lambda<JSFunctionDelegate>(className, YExpression.Block(s.VariableParameters.AsSequence(), inits), args);
                var fx = JSFunctionBuilder.New(lambda, StringSpanBuilder.New(className), StringSpanBuilder.Empty, 1);

                staticElements.Add(JSClassBuilder.AddConstructor(fx));
            }
        }

        var _new = JSClassBuilder.New(null, superVar, className);

        if (prototypeElements.Any())
            staticElements.Add(new YMemberElementInit(JSFunctionBuilder._prototype, prototypeElements));

        YExpression retVal = staticElements.Any() ? YExpression.MemberInit(_new, staticElements) : _new;

        stmts.Add(YExpression.Assign(retValue, retVal));

        if (id?.Name != null)
        {
            var v = this.scope.Top.CreateVariable(id.Name);
            stmts.Add(YExpression.Assign(v.Expression, retValue));
        }

        if (staticBlocks.Any())
        {
            var staticBlockEnumerator = staticBlocks.GetFastEnumerator();
            while (staticBlockEnumerator.MoveNext(out var staticBlock))
            {
                var function = staticBlock.Init as AstFunctionExpression;
                var fx = CreateFunction(function, superVar, forceStrictMode: true, createPrototype: false, directEvalPrivateNames: directEvalPrivateNames);
                stmts.Add(JSFunctionBuilder.InvokeFunction(fx, ArgumentsBuilder.NewEmpty(retValue)));
            }
        }

        stmts.Add(retValue);

        var result = YExpression.Block(new Sequence<YParameterExpression> { superVar, superPrototypeVar }, stmts);
        scope.Dispose();
        return result;
    }

    private static string[] CollectPrivateNames(IFastEnumerable<AstClassProperty> members)
    {
        var privateNames = new List<string>();
        var enumerator = members.GetFastEnumerator();
        while (enumerator.MoveNext(out var member))
        {
            if (member.IsPrivate && member.Key is AstIdentifier identifier)
                privateNames.Add(identifier.Name.Value);
        }

        return privateNames.Count == 0 ? null : [.. privateNames];
    }
}
