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
    private static int tempHomeId;

    private static readonly MethodInfo PrepareAnonymousFunctionNameForPropertyUIntMethod = typeof(JSVariable)
        .GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForProperty), [typeof(JSValue), typeof(uint)])
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForProperty(JSValue, uint) not found");
    private static readonly MethodInfo PrepareAnonymousFunctionNameForPropertyKeyStringMethod = typeof(JSVariable)
        .GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForProperty), [typeof(JSValue), typeof(KeyString)])
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForProperty(JSValue, KeyString) not found");
    private static readonly MethodInfo PrepareAnonymousFunctionNameForPropertyJSValueMethod = typeof(JSVariable)
        .GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForProperty), [typeof(JSValue), typeof(JSValue)])
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForProperty(JSValue, JSValue) not found");
    private static readonly MethodInfo PrepareAnonymousFunctionNameForGetterMethod = typeof(JSVariable)
        .GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForGetter), [typeof(JSValue), typeof(JSValue)])
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForGetter(JSValue, JSValue) not found");
    private static readonly MethodInfo PrepareAnonymousFunctionNameForSetterMethod = typeof(JSVariable)
        .GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForSetter), [typeof(JSValue), typeof(JSValue)])
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForSetter(JSValue, JSValue) not found");
    private static readonly MethodInfo PrepareAnonymousFunctionNameForGetterUIntMethod = typeof(JSVariable)
        .GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForGetter), [typeof(JSValue), typeof(uint)])
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForGetter(JSValue, uint) not found");
    private static readonly MethodInfo PrepareAnonymousFunctionNameForSetterUIntMethod = typeof(JSVariable)
        .GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForSetter), [typeof(JSValue), typeof(uint)])
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForSetter(JSValue, uint) not found");
    private static readonly MethodInfo PrepareAnonymousFunctionNameForFieldMethod = typeof(JSVariable)
        .GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForField), [typeof(JSValue), typeof(string)])
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForField(JSValue, string) not found");

    // ClassFieldDefinitionEvaluation: if a field initializer is an anonymous
    // function definition, NamedEvaluation names it after the field. Computed keys
    // resolve the name from the runtime key value; everything else uses the
    // compile-time field name (which already carries the "#" for a private field).
    private YExpression ApplyFieldFunctionName(AstClassProperty property, YExpression nameExpr, YExpression value)
    {
        if (!IsAnonymousFunctionDefinition(property.Init))
            return value;

        if (property.Computed)
            return YExpression.Call(null, PrepareAnonymousFunctionNameForPropertyJSValueMethod, value, nameExpr);

        var fieldName = GetPropertyFunctionName(property);
        if (fieldName == null)
            return value;

        return YExpression.Call(null, PrepareAnonymousFunctionNameForFieldMethod, value, YExpression.Constant(fieldName, typeof(string)));
    }

    // A computed PropertyName whose value is an anonymous function/accessor is used
    // both as the property key and for NamedEvaluation of the value. Returning the
    // same key node for both emits it twice, so it is evaluated twice — a spec
    // violation (PropertyName must be evaluated once) that, inside a generator, also
    // suspends a `yield` key twice so the property is never added. When that double
    // use applies (and the key is not a literal constant, which is side-effect free),
    // rewrite the in-place key (`keyExp`) so it evaluates once into a temp and returns
    // it, and hand back a side-effect-free read of that temp for the name. The temp is
    // assigned by the key argument (evaluated first by the Add* call) before the value
    // argument reads it.
    private YExpression SpillComputedPropertyKey(ref YExpression keyExp, bool keyIsLiteral, AstClassProperty p)
    {
        if (keyIsLiteral || !IsAnonymousFunctionDefinition(p.Init))
            return keyExp;

        if (p.Kind is not (AstPropertyKind.Data or AstPropertyKind.Method or AstPropertyKind.Constructor
            or AstPropertyKind.Get or AstPropertyKind.Set))
            return keyExp;

        var keyTemp = scope.Top.GetTempVariable(typeof(JSValue));
        var keyForName = keyTemp.Variable;
        keyExp = YExpression.Block(YExpression.Assign(keyTemp.Variable, keyExp), keyTemp.Variable);
        keyTemp.Dispose();
        return keyForName;
    }

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

        // NamedEvaluation for a computed-key accessor: name the function "get "/"set "
        // followed by the property key (the function is created anonymous because the key
        // is not known at compile time).
        static YExpression PrepareAnonymousAccessorName(YExpression value, YExpression key, bool isGetter)
        {
            if (key.Type == typeof(uint))
                return YExpression.Call(null, isGetter ? PrepareAnonymousFunctionNameForGetterUIntMethod : PrepareAnonymousFunctionNameForSetterUIntMethod, value, key);

            return YExpression.Call(null, isGetter ? PrepareAnonymousFunctionNameForGetterMethod : PrepareAnonymousFunctionNameForSetterMethod, value, key);
        }

        var properties = objectExpression.Properties;

        // A CoverInitializedName (`{ id = expr }`) reaching value compilation means
        // the object literal is being used as a value rather than reinterpreted as a
        // destructuring pattern — e.g. the `{b = 0}` base of `({a: {b = 0}.x} = {})`.
        // That is a SyntaxError (a genuine target is compiled as an ObjectPattern).
        var coverScan = properties.GetFastEnumerator();
        while (coverScan.MoveNext(out var coverNode))
        {
            if (coverNode is AstClassProperty { Kind: AstPropertyKind.Data, UsesAssign: true } coverInit)
                throw new FastParseException(coverInit.Start, "Invalid shorthand property initializer in object literal");
        }

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

        // Object literal methods/accessors that reference super need a home object,
        // so build the object into a temp and resolve super against its prototype.
        var usesSuper = ObjectLiteralUsesSuper(objectExpression);

        if (!hasProtoSetter && !usesSuper)
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
                    var keyIsLiteral = pKey.IsUIntLiteral(out var num);
                    var keyExp = keyIsLiteral ? YExpression.Constant(num) : Visit(pKey);

                    // When the value is an anonymous function/accessor, the computed
                    // PropertyName is consumed twice: as the property key and for
                    // NamedEvaluation of the value. Emitting the same key expression
                    // node twice evaluates it twice — a spec violation that, in a
                    // generator, suspends a `yield` key twice so the property is never
                    // added. Spill it into a temp so the key is evaluated once (in the
                    // add) and the name reads the stored value.
                    var keyForName = SpillComputedPropertyKey(ref keyExp, keyIsLiteral, p);

                    if (p.Kind is AstPropertyKind.Data or AstPropertyKind.Method or AstPropertyKind.Constructor
                        && IsAnonymousFunctionDefinition(p.Init))
                    {
                        value = PrepareAnonymousFunctionName(value, keyForName);
                    }

                    if (p.Kind == AstPropertyKind.Get)
                    {
                        value = PrepareAnonymousAccessorName(value, keyForName, isGetter: true);
                        elements.Add(JSObjectBuilder.AddGetter(keyExp, value));
                        continue;
                    }

                    if (p.Kind == AstPropertyKind.Set)
                    {
                        value = PrepareAnonymousAccessorName(value, keyForName, isGetter: false);
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

        // Methods/accessors that reference super capture the home object as a
        // closure. The pooled temp above is also declared at the function level
        // (VariableParameters), and a doubly-declared variable cannot be captured
        // reliably, leaving super null at call time. Mirror the class compiler and
        // capture a dedicated, block-local home-object variable instead.
        var homeObjectVar = usesSuper ? YExpression.Parameter(typeof(JSObject), "#home" + tempHomeId++) : null;

        var statements = new Sequence<YExpression>
        {
            YExpression.Assign(temp.Variable, JSObjectBuilder.New())
        };

        if (usesSuper)
            statements.Add(YExpression.Assign(homeObjectVar, temp.Variable));

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

            // Home object prototype for super.x inside this object's methods/accessors.
            YExpression MethodSuper() => usesSuper ? JSValueBuilder.SuperPrototypeOf(homeObjectVar) : null;

            YExpression key = null;
            YExpression value = p.Kind switch
            {
                AstPropertyKind.Get when p.Init is AstFunctionExpression function =>
                    CreateFunction(function, super: MethodSuper(), inferredFunctionName: GetPropertyFunctionName(p, "get"), createPrototype: false),
                AstPropertyKind.Set when p.Init is AstFunctionExpression function =>
                    CreateFunction(function, super: MethodSuper(), inferredFunctionName: GetPropertyFunctionName(p, "set"), createPrototype: false),
                AstPropertyKind.Method or AstPropertyKind.Constructor when p.Init is AstFunctionExpression function =>
                    CreateFunction(function, super: MethodSuper(), createPrototype: false),
                _ => VisitExpression(p.Init)
            };
            var pKey = p.Key;

            if (p.Computed)
            {
                var keyIsLiteral = pKey.IsUIntLiteral(out var num);
                var keyExp = keyIsLiteral ? YExpression.Constant(num) : Visit(pKey);

                // See the fast path above: spill the computed PropertyName so it is
                // evaluated exactly once when it is also used for NamedEvaluation.
                var keyForName = SpillComputedPropertyKey(ref keyExp, keyIsLiteral, p);

                if (p.Kind is AstPropertyKind.Data or AstPropertyKind.Method or AstPropertyKind.Constructor
                    && IsAnonymousFunctionDefinition(p.Init))
                {
                    value = PrepareAnonymousFunctionName(value, keyForName);
                }

                if (p.Kind == AstPropertyKind.Get)
                {
                    value = PrepareAnonymousAccessorName(value, keyForName, isGetter: true);
                    statements.Add(JSObjectBuilder.AddGetter(temp.Variable, keyExp, value));
                    continue;
                }

                if (p.Kind == AstPropertyKind.Set)
                {
                    value = PrepareAnonymousAccessorName(value, keyForName, isGetter: false);
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
                // B.3.1: `__proto__: value` mutates [[Prototype]] directly, NOT via a property
                // assignment (which a preceding/following own "__proto__" data property would shadow).
                statements.Add(JSObjectBuilder.SetPrototype(temp.Variable, value));
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
        var blockVariables = new Sequence<YParameterExpression> { temp.Variable };
        if (usesSuper)
            blockVariables.Add(homeObjectVar);
        return YExpression.Block(blockVariables, statements);
    }

    // Detects whether any of the object literal's own methods/accessors reference
    // super (crossing arrow functions, which inherit the home object, but not
    // nested non-arrow functions / object / class literals, which have their own).
    private static bool ObjectLiteralUsesSuper(AstObjectLiteral objectExpression)
    {
        var detector = new SuperUsageDetector();
        var en = objectExpression.Properties.GetFastEnumerator();
        while (en.MoveNext(out var pn))
        {
            if (pn is not AstClassProperty p
                || p.Init is not AstFunctionExpression { IsArrowFunction: false } function
                || p.Kind is not (AstPropertyKind.Method or AstPropertyKind.Get or AstPropertyKind.Set or AstPropertyKind.Constructor))
            {
                continue;
            }

            // super is permitted in (and scoped to) a method's parameter list as
            // well as its body, e.g. `m(a = super.x) {}`. Both must be scanned so the
            // home object is built; otherwise a method whose only super use is in a
            // default-parameter initializer would resolve super against `this`.
            detector.VisitParams(function.Params);
            detector.Visit(function.Body);
            if (detector.Found)
                return true;
        }

        return false;
    }

    private sealed class SuperUsageDetector : AstReduce
    {
        public bool Found;

        // Scan a function's parameter list for super usage (default-value
        // initializers and destructuring-pattern defaults).
        public void VisitParams(IFastEnumerable<VariableDeclarator> @params)
        {
            if (@params == null)
                return;

            var en = @params.GetFastEnumerator();
            while (en.MoveNext(out var param))
            {
                if (param.Identifier != null)
                    Visit(param.Identifier);
                if (param.Init != null)
                    Visit(param.Init);
            }
        }

        protected override AstNode VisitCallExpression(AstCallExpression callExpression)
        {
            if (callExpression.Callee is AstSuper)
            {
                Found = true;
                return callExpression;
            }

            // A direct eval may reference super at runtime (e.g. eval("super.x")),
            // so the method needs a [[HomeObject]]. Per spec every concise method /
            // accessor has one regardless of whether super is used syntactically;
            // detecting the direct eval call is enough to build it here, matching
            // class methods (which always establish a home object).
            if (callExpression.Callee is AstIdentifier evalId && evalId.Name.Equals("eval"))
            {
                Found = true;
                return callExpression;
            }

            return base.VisitCallExpression(callExpression);
        }

        protected override AstNode VisitMemberExpression(AstMemberExpression memberExpression)
        {
            if (memberExpression.Object is AstSuper)
            {
                Found = true;
                return memberExpression;
            }

            return base.VisitMemberExpression(memberExpression);
        }

        protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        {
            // Only arrow functions inherit the enclosing home object's super; a
            // nested non-arrow function introduces its own (absent) super binding.
            if (functionExpression.IsArrowFunction)
            {
                VisitParams(functionExpression.Params);
                Visit(functionExpression.Body);
            }

            return functionExpression;
        }
    }
}
