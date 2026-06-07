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
                // A `#x` class element is an IdentifierName starting with '#'; a
                // public `"#x"` element is an AstLiteral and never reaches here.
                if (id.Name.Length > 0 && id.Name.Value[0] == '#')
                    return KeyOfPrivateName(id.Name);
                return KeyOfName(id.Name);

            case (FastNodeType.Literal, AstLiteral l):
                if (computed)
                    return VisitLiteral(l);
                return GetLiteralPropertyKey(l);

            default:
                return Visit(exp);
        }
    }


    private YExpression GetClassElementName(AstClassProperty property)
    {
        var name = GetName(property);
        return property.Computed && name.Type.IsJSValueType()
            ? YExpression.Call(null, NormalizePropertyKeyMethod, name)
            : name;
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
        var hasSuperClass = super != null;

        var prototypeElements = new Sequence<YElementInit>();
        var staticElements = new Sequence<YBinding>();
        var staticBlocks = new Sequence<AstClassProperty>();

        // need to save super..
        // create a super variable...
        YExpression superExp;
        if (hasSuperClass)
        {
            // All parts of a class definition are strict mode code, including the
            // heritage (ClassHeritage) expression. A function expression there
            // (`class extends function () { ... }`) must therefore be strict — so
            // its `arguments`/`caller` are poison pills and `arguments.callee`
            // inside it throws.
            var previousStrictMode = IsStrictMode;
            IsStrictMode = true;
            try
            {
                superExp = VisitExpression(super);
            }
            finally
            {
                IsStrictMode = previousStrictMode;
            }
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
        var computedMemberNames = new Dictionary<AstClassProperty, YExpression>();
        var classScopeVariables = new Sequence<YParameterExpression> { superVar, superPrototypeVar };
        AstFunctionExpression constructor = null;
        var directEvalPrivateNames = CombinePrivateNames(this.scope.Top.DirectEvalPrivateNames, CollectPrivateNames(body.Members));

        // The class name is an immutable (const-like) binding scoped to the class
        // body: the constructor, methods, accessors and static blocks close over
        // it, and assigning to it (`class C { m() { C = 1; } }`) is a TypeError.
        // It lives in a dedicated class scope pushed here so it does not leak to
        // the enclosing scope and does not collide with the outer, mutable
        // declaration binding, which is (re)created after this scope is popped.
        FastFunctionScope classNameScope = null;
        FastFunctionScope.VariableScope innerNameVar = null;
        if (id?.Name != null)
        {
            classNameScope = this.scope.Push(new FastFunctionScope(this.scope.Top));
            innerNameVar = classNameScope.CreateVariable(id.Name, newScope: true);
        }

        var en = body.Members.GetFastEnumerator();
        while (en.MoveNext(out var property))
        {
            var isPrivateName = property.Key is AstIdentifier propertyIdentifier && propertyIdentifier.Name.Value.StartsWith("#");

            // ECMA-262 ClassDefinitionEvaluation evaluates every ClassElementName
            // (the computed key) in source order, before any static field
            // initializer or method definition that follows it. Pre-evaluate each
            // computed key here, in order, into a class-scope variable so a later
            // element's key cannot run ahead of an earlier element's abrupt
            // completion (computed-property-abrupt-completion) and so mixed
            // static/instance keys observe source order. Methods, accessors and
            // static data fields then consume the cached value instead of
            // re-evaluating it inside the deferred MemberInit; instance fields read
            // it at construction time via ComputedMemberNames.
            if (property.Computed
                && property.Kind is AstPropertyKind.Data or AstPropertyKind.Get
                    or AstPropertyKind.Set or AstPropertyKind.Method)
            {
                var computedNameVar = YExpression.Parameter(typeof(JSValue), $"#className{computedMemberNames.Count}");
                classScopeVariables.Add(computedNameVar);
                stmts.Add(YExpression.Assign(computedNameVar, ValidateStaticPropertyName(property, GetClassElementName(property))));
                computedMemberNames[property] = computedNameVar;
            }

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
                        name = property.Computed
                            ? computedMemberNames[property]
                            : ValidateStaticPropertyName(property, GetClassElementName(property));
                        var value = property.Init == null ? JSUndefinedBuilder.Value : Visit(property.Init);
                        var attributes = isPrivateName
                            ? JSPropertyAttributes.ConfigurableValue
                            : JSPropertyAttributes.EnumerableConfigurableValue;
                        staticElements.Add(JSObjectBuilder.AddValue(name, value, attributes));
                        break;
                    }
                    // The computed key (if any) was already evaluated, in source
                    // order, into ComputedMemberNames above; the initializer runs
                    // per-instance during construction (InitMembers).
                    memberInits.Add(property);
                    break;

                case AstPropertyKind.Get:
                    name = property.Computed
                        ? computedMemberNames[property]
                        : ValidateStaticPropertyName(property, GetClassElementName(property));
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
                    name = property.Computed
                        ? computedMemberNames[property]
                        : ValidateStaticPropertyName(property, GetClassElementName(property));
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
                    name = property.Computed
                        ? computedMemberNames[property]
                        : ValidateStaticPropertyName(property, GetClassElementName(property));
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

        var className = id?.Name.Value ?? string.Empty;

        if (constructor != null)
        {
            // super.x in the constructor body / field initializers resolves against
            // the superclass prototype, while super(...) targets the superclass
            // constructor. Pass both so each resolves correctly.
            var fx = CreateFunction(constructor, superPrototypeVar, true, className, memberInits, true, directEvalPrivateNames: directEvalPrivateNames, computedMemberNames: computedMemberNames,
                thisIsUninitialized: hasSuperClass, superConstructor: superVar);
            staticElements.Add(JSClassBuilder.AddConstructor(fx));
        }
        else
        {
            if (memberInits.Any())
            {
                // super.x in instance field initializers resolves against the home
                // object's prototype (the superclass prototype), so give the synthetic
                // default constructor scope that super binding.
                using var s = this.scope.Push(new FastFunctionScope(null, null, super: superPrototypeVar, memberInits: memberInits, directEvalPrivateNames: directEvalPrivateNames, computedMemberNames: computedMemberNames, thisIsUninitialized: hasSuperClass));
                var args = s.Arguments;
                var @this = s.ThisExpression;
                var inits = new Sequence<YExpression>() { };

                inits.AddRange(s.InitList);
                if (hasSuperClass)
                    inits.Add(JSFunctionBuilder.InvokeSuperConstructor(superVar, @this, args));

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

        if (innerNameVar != null)
        {
            // Initialize the inner class-name binding to the class object, then
            // lock it so a write from within the body throws a TypeError.
            stmts.Add(YExpression.Assign(innerNameVar.Expression, retValue));
            stmts.Add(JSVariableBuilder.SetReadOnly(innerNameVar.Variable, true));
        }

        if (staticBlocks.Any())
        {
            var staticBlockEnumerator = staticBlocks.GetFastEnumerator();
            while (staticBlockEnumerator.MoveNext(out var staticBlock))
            {
                var function = staticBlock.Init as AstFunctionExpression;
                // A class static initialization block always runs with `this`
                // bound to the (already-constructed) class constructor — it is
                // passed as the receiver below. Unlike a derived constructor /
                // instance field initializer, there is no super() call that
                // initializes `this`, so it is NEVER in the temporal dead zone,
                // even when the class has a superclass. `super.x` inside the
                // block reads that `this`, so leaving it uninitialized here threw
                // "Cannot access 'this' before initialization".
                var fx = CreateFunction(function, superVar, forceStrictMode: true, createPrototype: false, directEvalPrivateNames: directEvalPrivateNames,
                    thisIsUninitialized: false);
                stmts.Add(JSFunctionBuilder.InvokeFunction(fx, ArgumentsBuilder.NewEmpty(retValue)));
            }
        }

        // Pop the class scope and recreate the outer, mutable declaration binding
        // in the enclosing scope. Both bindings share the name but live in
        // different scopes: the inner const is what the body resolves to, the
        // outer one is what `class C {}` declares (and what `C = ...` after the
        // declaration targets). The class-scope locals (the inner binding) must be
        // declared in the emitted block and instantiated by its InitList before
        // any statement uses them.
        if (classNameScope != null)
        {
            classScopeVariables.AddRange(classNameScope.VariableParameters);
            var initList = new Sequence<YExpression>();
            initList.AddRange(classNameScope.InitList);
            initList.AddRange(stmts);
            stmts = initList;

            classNameScope.Dispose();

            // Only a ClassDeclaration binds its name in the enclosing scope. A
            // named ClassExpression keeps its name purely as the inner binding
            // above, so creating an outer binding here would leak it.
            if (body.IsDeclaration)
            {
                var outer = this.scope.Top.CreateVariable(id.Name);
                stmts.Add(YExpression.Assign(outer.Expression, retValue));
            }
        }

        stmts.Add(retValue);

        var result = YExpression.Block(classScopeVariables, stmts);
        scope.Dispose();
        return result;
    }

    private static string[] CombinePrivateNames(string[] outerPrivateNames, string[] ownPrivateNames)
    {
        if (outerPrivateNames == null || outerPrivateNames.Length == 0)
            return ownPrivateNames;

        if (ownPrivateNames == null || ownPrivateNames.Length == 0)
            return outerPrivateNames;

        var privateNames = new HashSet<string>(outerPrivateNames, StringComparer.Ordinal);
        foreach (var privateName in ownPrivateNames)
            privateNames.Add(privateName);

        return [.. privateNames];
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
