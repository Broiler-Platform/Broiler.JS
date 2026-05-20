using System;
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
    private static string FormatLiteralPropertyName(AstLiteral literal)
    {
        if (literal.TokenType == TokenTypes.String)
            return literal.StringValue;

        var value = literal.NumericValue;
        if (double.IsNaN(value))
            return nameof(double.NaN);

        if (value == 0)
            return "0";

        if (value > 0 && (uint)value == value)
            return ((uint)value).ToString();

        return value.ToString();
    }

    private static string GetClassMemberFunctionName(AstClassProperty property, string prefix = null)
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

                if (l.TokenType == TokenTypes.String)
                {
                    if (NumberParser.TryGetArrayIndex(l.StringValue, out var ui))
                        return YExpression.Constant(ui);

                    return KeyOfName(l.StringValue);
                }

                return VisitLiteral(l);

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

        var superVar = YExpression.Parameter(JSFunctionBuilder.FunctionType);
        var superPrototypeVar = YExpression.Parameter(typeof(JSObject));

        var stmts = new Sequence<YExpression>(body.Members.Count)
        {
            YExpression.Assign(superVar, YExpression.TypeAs(superExp, JSFunctionBuilder.FunctionType)),
            YExpression.Assign(superPrototypeVar, JSFunctionBuilder.Prototype(superVar))
        };

        YExpression retValue = tempVar.Variable;

        var memberInits = new Sequence<AstClassProperty>();
        AstFunctionExpression constructor = null;

        var en = body.Members.GetFastEnumerator();
        while (en.MoveNext(out var property))
        {
            YExpression name;
            // var el = property.IsStatic ? staticElements : prototypeElements;
            switch (property.Kind)
            {
                case AstPropertyKind.Data:
                    if (property.IsStatic)
                    {
                        name = ValidateStaticPropertyName(property, GetName(property));
                        var value = property.Init == null ? JSUndefinedBuilder.Value : Visit(property.Init);
                        staticElements.Add(JSObjectBuilder.AddValue(name, value, JSPropertyAttributes.ConfigurableValue));
                        break;
                    }
                    memberInits.Add(property);
                    break;

                case AstPropertyKind.Get:
                    name = ValidateStaticPropertyName(property, GetName(property));
                    if (property.IsStatic)
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, superVar, forceStrictMode: true,
                            inferredFunctionName: GetClassMemberFunctionName(property, "get"));
                        staticElements.Add(JSObjectBuilder.AddGetter(name, fx, JSPropertyAttributes.ConfigurableProperty));
                        break;
                    }
                    else
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, superPrototypeVar, forceStrictMode: true,
                            inferredFunctionName: GetClassMemberFunctionName(property, "get"));
                        prototypeElements.Add(JSObjectBuilder.AddGetter(name, fx, JSPropertyAttributes.ConfigurableProperty));
                    }
                    break;

                case AstPropertyKind.Set:
                    name = ValidateStaticPropertyName(property, GetName(property));
                    if (property.IsStatic)
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, superVar, forceStrictMode: true,
                            inferredFunctionName: GetClassMemberFunctionName(property, "set"));
                        staticElements.Add(JSObjectBuilder.AddSetter(name, fx, JSPropertyAttributes.ConfigurableProperty));
                    }
                    else
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, superPrototypeVar, forceStrictMode: true,
                            inferredFunctionName: GetClassMemberFunctionName(property, "set"));
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
                        var fx = CreateFunction(property.Init as AstFunctionExpression, superVar, forceStrictMode: true);
                        staticElements.Add(JSObjectBuilder.AddValue(name, fx, JSPropertyAttributes.ConfigurableValue));
                    }
                    else
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, superPrototypeVar, forceStrictMode: true);
                        prototypeElements.Add(JSObjectBuilder.AddValue(name, fx, JSPropertyAttributes.ConfigurableValue));
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        var className = id?.Name.Value;

        if (constructor != null)
        {
            var fx = CreateFunction(constructor, superVar, true, className, memberInits, true);
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
        else
        {
            stmts.Add(retValue);
        }

        var result = YExpression.Block(new Sequence<YParameterExpression> { superVar, superPrototypeVar }, stmts);
        scope.Dispose();
        return result;
    }
}
