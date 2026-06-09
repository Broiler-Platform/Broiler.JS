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
        // Only a public static element can collide with the reserved "prototype"
        // name. A private name (`#x`) lives in the marker-prefixed private namespace
        // and can never be "prototype", so it needs no validation — and skipping it
        // avoids passing a per-evaluation minted key (a captured local) by reference,
        // which would load a stale value for the static field's key.
        if (!property.IsStatic || property.IsPrivate)
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

        // [[HomeObject]] holders for DYNAMIC super resolution. GetSuperBase reads the
        // home object's CURRENT [[Prototype]] on every super access, so
        // `Object.setPrototypeOf(C, X)` / `setPrototypeOf(C.prototype, X)` is observed
        // (matching the object-literal super path). The home objects are the class
        // itself (for static members and the super() target) and its prototype (for
        // instance members and the constructor's super.x). They cannot be captured as
        // plain locals: a method's function object is built DURING the class object's
        // construction (MemberInit), and the closure rewriter snapshots a plain captured
        // local at that moment — when the class/prototype do not yet exist. A JSVariable
        // is a heap box whose REFERENCE is captured (stable) while its value is filled in
        // after the class is built (the same mechanism the inner class-name binding uses).
        var homeConstructorVar = YExpression.Parameter(typeof(JSVariable), "#super-home-ctor");
        var homePrototypeVar = YExpression.Parameter(typeof(JSVariable), "#super-home-proto");
        YExpression StaticSuper() => JSValueBuilder.SuperPrototypeOf(JSVariable.ValueExpression(homeConstructorVar));
        YExpression InstanceSuper() => JSValueBuilder.SuperPrototypeOf(JSVariable.ValueExpression(homePrototypeVar));

        var stmts = new Sequence<YExpression>(body.Members.Count)
        {
            YExpression.Assign(superVar, superExp),
            YExpression.Assign(superPrototypeVar, JSClassBuilder.ResolveSuperclassPrototype(superVar)),
            // Allocate the (empty) home-object boxes before any member function object is
            // created, so the methods capture these stable references. The JSVariable
            // names are EMPTY: a named binding would re-run NamedEvaluation when the
            // class object is stored into it, renaming an anonymous `class {}` to the
            // holder's name instead of its binding's.
            YExpression.Assign(homeConstructorVar, JSVariableBuilder.New("")),
            YExpression.Assign(homePrototypeVar, JSVariableBuilder.New(""))
        };

        YExpression retValue = tempVar.Variable;

        var memberInits = new Sequence<AstClassProperty>();
        var computedMemberNames = new Dictionary<AstClassProperty, YExpression>();
        var classScopeVariables = new Sequence<YParameterExpression> { superVar, superPrototypeVar, homeConstructorVar, homePrototypeVar };
        AstFunctionExpression constructor = null;

        // Non-static private methods/accessors are installed PER INSTANCE (not on the
        // prototype) so that a `return`-override object carries them and a second
        // installation throws. Each function object is created once here, into a
        // class-scope variable, and referenced by the constructor's InitMembers. A
        // getter and setter sharing a private name merge into one element.
        var privateInstanceElements = new List<PrivateInstanceElement>();
        var privateElementByName = new Dictionary<string, PrivateInstanceElement>(StringComparer.Ordinal);

        // Static data field initializations are deferred to run AFTER the class is
        // defined and its name binding is set (ClassDefinitionEvaluation evaluates
        // static field initializers last) — so an initializer that references the
        // class name sees the constructor, and adding a static private field to a
        // (self-)sealed constructor throws. Static methods/accessors stay in the
        // constructor's object initializer and so are installed first.
        var staticFieldInits = new List<(YExpression Name, YExpression Value, bool IsPrivate)>();

        PrivateInstanceElement PrivateElementFor(AstClassProperty property, YExpression keyName)
        {
            var pname = ((AstIdentifier)property.Key).Name.Value;
            if (!privateElementByName.TryGetValue(pname, out var element))
            {
                element = new PrivateInstanceElement { Key = keyName };
                privateElementByName[pname] = element;
                privateInstanceElements.Add(element);
            }
            return element;
        }

        YParameterExpression SharedMemberFunctionVar(YExpression fx, string label)
        {
            var fnVar = YExpression.Parameter(fx.Type, $"{label}$pf{privateKeyVarCounter++}");
            classScopeVariables.Add(fnVar);
            stmts.Add(YExpression.Assign(fnVar, fx));
            return fnVar;
        }
        var ownPrivateNames = CollectPrivateNames(body.Members);
        var directEvalPrivateNames = CombinePrivateNames(this.scope.Top.DirectEvalPrivateNames, ownPrivateNames);
        // Every declared private name — instance AND static — gets a per-evaluation
        // minted key. ClassDefinitionEvaluation creates a fresh Private Name for each
        // `#x` on every class evaluation (§sec-runtime-semantics-evaluate-name), so a
        // static private element installed by one evaluation must be absent on the
        // constructor produced by another (e.g. `C1.access.call(C2)` is a TypeError).
        // Static private members reference the minted-key variable through their
        // closure exactly like instance members do.
        var mintablePrivateNames = ownPrivateNames;

        // Mint a fresh private-name key per declared `#x` for THIS class evaluation,
        // each stored in a class-scope variable that all member references close
        // over. Two evaluations of the same class text therefore use distinct keys,
        // so a private element installed by one is absent on instances of the other
        // (the key is the per-evaluation PrivateBrand). The names are registered on
        // the compiler's private-name stack while members compile, so a nested
        // class's `#x` shadows an enclosing one.
        Dictionary<string, YExpression> privateNameScope = null;
        // A direct eval inside a member can reference this class's private names, but
        // the eval is compiled separately and resolves them to the stable
        // marker-prefixed constant key (it cannot see the minted-key variables). So
        // when the class body might contain a direct eval, fall back to constant keys
        // for the whole class — losing per-evaluation brand distinctness for that
        // class but keeping private names visible to the eval (#667). The common
        // (eval-free) class still gets unique per-evaluation brands.
        if (mintablePrivateNames != null && !ClassBodyMayDirectEval(body.Members))
        {
            privateNameScope = new Dictionary<string, YExpression>(mintablePrivateNames.Length);
            foreach (var privateName in mintablePrivateNames)
            {
                // A getter and setter share one private name — mint it only once.
                if (privateNameScope.ContainsKey(privateName))
                    continue;

                // The variable MUST be named: a method that references the key by
                // address (a private-method call `o.#m()` passes the key as an
                // `in KeyString` argument) resolves the captured closure variable by
                // name, and the name must be unique across nested class evaluations.
                var privateKeyVar = YExpression.Parameter(
                    typeof(KeyString), $"{privateName}$pk{privateKeyVarCounter++}");
                classScopeVariables.Add(privateKeyVar);
                stmts.Add(YExpression.Assign(privateKeyVar, JSObjectBuilder.MintPrivateName(privateName)));
                privateNameScope[privateName] = privateKeyVar;
            }

            PushPrivateNameScope(privateNameScope);
        }

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
                        var value = property.Init == null
                            ? JSUndefinedBuilder.Value
                            : ApplyFieldFunctionName(property, name, Visit(property.Init));
                        // Deferred to after the class binding (see staticFieldInits).
                        staticFieldInits.Add((name, value, isPrivateName));
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
                        var fx = CreateFunction(property.Init as AstFunctionExpression, StaticSuper(), forceStrictMode: true,
                            inferredFunctionName: GetPropertyFunctionName(property, "get"), createPrototype: false, directEvalPrivateNames: directEvalPrivateNames);
                        staticElements.Add(JSObjectBuilder.AddGetter(name, fx, JSPropertyAttributes.ConfigurableProperty));
                        break;
                    }
                    else
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, InstanceSuper(), forceStrictMode: true,
                            inferredFunctionName: GetPropertyFunctionName(property, "get"), createPrototype: false, directEvalPrivateNames: directEvalPrivateNames);
                        if (isPrivateName)
                            PrivateElementFor(property, name).Getter = SharedMemberFunctionVar(fx, "#get");
                        else
                            prototypeElements.Add(JSObjectBuilder.AddGetter(name, fx, JSPropertyAttributes.ConfigurableProperty));
                    }
                    break;

                case AstPropertyKind.Set:
                    name = property.Computed
                        ? computedMemberNames[property]
                        : ValidateStaticPropertyName(property, GetClassElementName(property));
                    if (property.IsStatic)
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, StaticSuper(), forceStrictMode: true,
                            inferredFunctionName: GetPropertyFunctionName(property, "set"), createPrototype: false, directEvalPrivateNames: directEvalPrivateNames);
                        staticElements.Add(JSObjectBuilder.AddSetter(name, fx, JSPropertyAttributes.ConfigurableProperty));
                    }
                    else
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, InstanceSuper(), forceStrictMode: true,
                            inferredFunctionName: GetPropertyFunctionName(property, "set"), createPrototype: false, directEvalPrivateNames: directEvalPrivateNames);
                        if (isPrivateName)
                            PrivateElementFor(property, name).Setter = SharedMemberFunctionVar(fx, "#set");
                        else
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
                        var fx = CreateFunction(property.Init as AstFunctionExpression, StaticSuper(), forceStrictMode: true,
                            inferredFunctionName: GetPropertyFunctionName(property), createPrototype: false, directEvalPrivateNames: directEvalPrivateNames);
                        staticElements.Add(JSObjectBuilder.AddValue(name, fx, isPrivateName ? JSPropertyAttributes.ConfigurableReadonlyValue : JSPropertyAttributes.ConfigurableValue));
                    }
                    else
                    {
                        var fx = CreateFunction(property.Init as AstFunctionExpression, InstanceSuper(), forceStrictMode: true,
                            inferredFunctionName: GetPropertyFunctionName(property), createPrototype: false, directEvalPrivateNames: directEvalPrivateNames);
                        if (isPrivateName)
                            PrivateElementFor(property, name).Method = SharedMemberFunctionVar(fx, "#m");
                        else
                            prototypeElements.Add(JSObjectBuilder.AddValue(name, fx, JSPropertyAttributes.ConfigurableValue));
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
            var fx = CreateFunction(constructor, InstanceSuper(), true, className, memberInits, true, directEvalPrivateNames: directEvalPrivateNames, computedMemberNames: computedMemberNames,
                thisIsUninitialized: hasSuperClass, superConstructor: StaticSuper(), privateInstanceElements: privateInstanceElements);
            staticElements.Add(JSClassBuilder.AddConstructor(fx));
        }
        else
        {
            if (memberInits.Any() || privateInstanceElements.Count > 0)
            {
                // super.x in instance field initializers resolves against the home
                // object's prototype (the superclass prototype), so give the synthetic
                // default constructor scope that super binding.
                using var s = this.scope.Push(new FastFunctionScope(null, null, super: InstanceSuper(), memberInits: memberInits, directEvalPrivateNames: directEvalPrivateNames, computedMemberNames: computedMemberNames, thisIsUninitialized: hasSuperClass));
                s.PrivateInstanceElements = privateInstanceElements;
                var args = s.Arguments;
                var @this = s.ThisExpression;
                var inits = new Sequence<YExpression>() { };

                inits.AddRange(s.InitList);
                if (hasSuperClass)
                    inits.Add(JSFunctionBuilder.InvokeSuperConstructor(StaticSuper(), @this, args));

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

        // Fill the home-object boxes now that the class object exists. Member function
        // objects created above captured these boxes; their super references read the
        // current value here at call time. Done before static field initializers and
        // static blocks run (which may themselves use super).
        stmts.Add(YExpression.Assign(JSVariable.ValueExpression(homeConstructorVar), retValue));
        stmts.Add(YExpression.Assign(JSVariable.ValueExpression(homePrototypeVar), JSFunctionBuilder.Prototype(retValue)));

        if (innerNameVar != null)
        {
            // Initialize the inner class-name binding to the class object, then
            // lock it so a write from within the body throws a TypeError.
            stmts.Add(YExpression.Assign(innerNameVar.Expression, retValue));
            stmts.Add(JSVariableBuilder.SetReadOnly(innerNameVar.Variable, true));
        }

        // Static field initializers run on the constructor now that its name binding
        // is set. A private static field uses PrivateFieldAdd so a self-sealed
        // constructor (preventExtensions in an earlier initializer) throws.
        foreach (var (fieldName, fieldValue, fieldIsPrivate) in staticFieldInits)
        {
            stmts.Add(fieldIsPrivate
                ? JSObjectBuilder.PrivateFieldAdd(retValue, fieldName, fieldValue)
                : JSObjectBuilder.AddValue(retValue, fieldName, fieldValue, JSPropertyAttributes.EnumerableConfigurableValue));
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
                var fx = CreateFunction(function, StaticSuper(), forceStrictMode: true, createPrototype: false, directEvalPrivateNames: directEvalPrivateNames,
                    thisIsUninitialized: false);
                stmts.Add(JSFunctionBuilder.InvokeFunction(fx, ArgumentsBuilder.NewEmpty(retValue)));
            }
        }

        // All member, constructor and static-block bodies (the only places that can
        // reference this class's private names) have been compiled; the minted-key
        // variables remain declared in classScopeVariables.
        if (privateNameScope != null)
            PopPrivateNameScope();

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

    // Conservative scan: does any class element's source contain the token "eval"?
    // A false positive only forgoes the per-evaluation-brand optimization for that
    // class (correctness is unaffected); a missed direct eval would, in contrast,
    // break private-name resolution inside the eval, so err toward detecting it.
    private static bool ClassBodyMayDirectEval(IFastEnumerable<AstClassProperty> members)
    {
        var en = members.GetFastEnumerator();
        while (en.MoveNext(out var member))
        {
            var span = member.Start.Span;
            var source = span.Source;
            if (source == null)
                continue;

            var start = span.Offset;
            var endSpan = member.End.Span;
            var end = System.Math.Min(source.Length, endSpan.Offset + endSpan.Length);
            if (end > start && source.IndexOf("eval", start, end - start, System.StringComparison.Ordinal) >= 0)
                return true;
        }

        return false;
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
