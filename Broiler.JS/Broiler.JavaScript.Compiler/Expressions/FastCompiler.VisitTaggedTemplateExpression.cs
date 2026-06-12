using System;
using System.Reflection;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private static readonly MethodInfo FreezeObjectMethod = typeof(JSObject).GetMethod("FreezeObject", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("JSObject.FreezeObject not found");

    private static readonly MethodInfo GetOrCreateTemplateObjectMethod = typeof(JSObject).GetMethod("GetOrCreateTemplateObject", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("JSObject.GetOrCreateTemplateObject not found");

    protected override YExpression VisitTaggedTemplateExpression(AstTaggedTemplateExpression template)
    {
        var callee = template.Tag;

        var args = new Sequence<YExpression>(template.Arguments.Count);
        var parts = new Sequence<YElementInit>(template.Arguments.Count);
        var raw = new Sequence<YExpression>(template.Arguments.Count);

        var e = template.Arguments.GetFastEnumerator();
        args.Add(null);

        // Deterministic hash of the raw template contents, folded into the cache key
        // below so that two distinct templates that happen to start at the same
        // source offset (different compilations sharing the process-wide cache) do
        // not alias one another's frozen template object.
        var rawHash = unchecked((int)2166136261);

        while (e.MoveNext(out var p))
        {
            if (p.Type == FastNodeType.Literal)
            {
                var l = p as AstLiteral;
                if (l.TokenType == TokenTypes.TemplatePart || l.TokenType == TokenTypes.TemplateEnd)
                {
                    var r = l.Start.Span.Value;
                    if (r.StartsWith("`"))
                        r = r.Substring(1);

                    if (r.StartsWith("}"))
                        r = r.TrimStart('}');

                    if (r.EndsWith("${"))
                        r = r.Substring(0, r.Length - 2);
                    else if (r.EndsWith("`"))
                        r = r.Substring(0, r.Length - 1);

                    unchecked
                    {
                        foreach (var c in r)
                            rawHash = (rawHash ^ c) * 16777619;
                        rawHash = (rawHash ^ 0x1F) * 16777619; // part separator
                    }

                    raw.Add(JSStringBuilder.New(YExpression.Constant(r)));

                    // A template part with an invalid escape sequence has no cooked
                    // value: its TemplateStringsArray entry is `undefined` (ES2018
                    // template literal revision). The raw value is still preserved.
                    var cooked = l.Start.CookedInvalid
                        ? (YExpression)JSUndefinedBuilder.Value
                        : JSStringBuilder.New(YExpression.Constant(l.StringValue));
                    parts.Add(new YElementInit(JSArrayBuilder._Add, cooked));
                    continue;
                }
            }

            args.Add(VisitExpression(p));
        }

        // replace first node...
        var rawArray = YExpression.Call(null, FreezeObjectMethod, JSArrayBuilder.New(raw));
        parts.Add(new YElementInit(JSObjectBuilder._FastAddValueKeyString, KeyOfName("raw"), rawArray, JSPropertyAttributesBuilder.EnumerableConfigurableValue));

        var unfrozenArray = JSArrayBuilder.New(parts);

        // Use source position (combined with a hash of the raw contents) as a
        // stable cache key for template object identity (ES2015 12.2.9.3). The
        // raw-content hash disambiguates distinct templates that share a source
        // offset across separate compilations using the process-wide cache.
        var cacheKey = unchecked((template.Start.Span.Offset * 397) ^ rawHash);
        var partsArray = YExpression.Call(null, GetOrCreateTemplateObjectMethod, YExpression.Constant(cacheKey), unfrozenArray);
        args[0] = partsArray;

        if (callee.Type == FastNodeType.MemberExpression && callee is AstMemberExpression me)
        {
            YExpression name;

            switch (me.Property.Type)
            {
                case FastNodeType.Identifier:
                    var id = (me.Property as AstIdentifier)!;
                    name = me.Computed ? VisitExpression(id) : KeyOfName(id.Name);
                    break;

                case FastNodeType.Literal:
                    var l = (me.Property as AstLiteral)!;
                    if (l.TokenType == TokenTypes.String)
                        name = KeyOfName(l.Start.CookedText);
                    else if (l.TokenType == TokenTypes.Number)
                        name = GetLiteralPropertyKey(l);
                    else
                        throw new NotImplementedException();
                    break;

                case FastNodeType.MemberExpression:
                    name = VisitMemberExpression(me.Property as AstMemberExpression);
                    break;

                default:
                    throw new NotImplementedException($"{me.Property}");
            }

            bool isSuper = me.Object.Type == FastNodeType.Super;
            var super = isSuper ? scope.Top.Super : null;
            var target = isSuper ? scope.Top.ThisExpression : VisitExpression(me.Object);

            if (isSuper)
            {
                var superMethod = JSValueBuilder.Index(super, name, me.Coalesce);
                return JSFunctionBuilder.InvokeFunction(superMethod, ArgumentsBuilder.New(JSUndefinedBuilder.Value, args), me.Coalesce);
            }

            using var te = scope.Top.GetTempVariable(typeof(JSValue));
            using var te2 = scope.Top.GetTempVariable(typeof(JSValue));
            return JSValueBuilder.InvokeMethod(te.Variable, te2.Variable, target, name, args, false, me.Coalesce);
        }
        else
        {
            bool isSuper = callee.Type == FastNodeType.Super;

            if (isSuper)
            {
                var paramArray1 = ArgumentsBuilder.New(JSUndefinedBuilder.Value, args);
                return JSFunctionBuilder.InvokeSuperConstructor(scope.Top.Super, scope.Top.ThisExpression, paramArray1);
            }

            var target = VisitExpression(callee);
            return JSFunctionBuilder.InvokeFunction(target, ArgumentsBuilder.New(JSUndefinedBuilder.Value, args));
        }
    }
}
