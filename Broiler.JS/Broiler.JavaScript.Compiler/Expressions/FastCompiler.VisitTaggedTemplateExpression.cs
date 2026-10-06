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

    // Map <CR> and <CRLF> line terminators in a raw template segment to a single <LF>, per the
    // TRV (template raw value) grammar. Other characters — including <LS>/<PS> — are unchanged.
    private static string NormalizeTemplateLineTerminators(string r)
    {
        if (r.IndexOf('\r') < 0)
            return r;

        var sb = new System.Text.StringBuilder(r.Length);
        for (int i = 0; i < r.Length; i++)
        {
            char c = r[i];
            if (c == '\r')
            {
                sb.Append('\n');
                if (i + 1 < r.Length && r[i + 1] == '\n')
                    i++;
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    protected override BExpression VisitTaggedTemplateExpression(AstTaggedTemplateExpression template)
    {
        var callee = template.Tag;

        var args = new Sequence<BExpression>(template.Arguments.Count);
        var parts = new Sequence<BElementInit>(template.Arguments.Count);
        var raw = new Sequence<BExpression>(template.Arguments.Count);

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

                    // ES TRV normalization: <CR> and <CRLF> in the raw template text both map to
                    // a single <LF> (so `String.raw` and the `.raw` array never expose a carriage
                    // return). <LS>/<PS> are preserved.
                    r = NormalizeTemplateLineTerminators(r);

                    unchecked
                    {
                        foreach (var c in r)
                            rawHash = (rawHash ^ c) * 16777619;
                        rawHash = (rawHash ^ 0x1F) * 16777619; // part separator
                    }

                    raw.Add(JSStringBuilder.New(BExpression.Constant(r)));

                    // A template part with an invalid escape sequence has no cooked
                    // value: its TemplateStringsArray entry is `undefined` (ES2018
                    // template literal revision). The raw value is still preserved.
                    var cooked = l.Start.CookedInvalid
                        ? (BExpression)JSUndefinedBuilder.Value
                        : JSStringBuilder.New(BExpression.Constant(l.StringValue));
                    parts.Add(new BElementInit(JSArrayBuilder._Add, cooked));
                    continue;
                }
            }

            args.Add(VisitExpression(p));
        }

        // replace first node...
        // §13.2.8.4 GetTemplateObject freezes the template object, so its "raw" property is a
        // non-writable, non-enumerable, non-configurable data property (ReadonlyValue) — not an
        // enumerable/configurable one (test262 tagged-template/template-object).
        var rawArray = BExpression.Call(null, FreezeObjectMethod, JSArrayBuilder.New(raw));
        parts.Add(new BElementInit(JSObjectBuilder._FastAddValueKeyString, KeyOfName("raw"), rawArray, JSPropertyAttributesBuilder.ReadonlyValue));

        var unfrozenArray = JSArrayBuilder.New(parts);

        // Use source position (combined with a hash of the raw contents) as a
        // stable cache key for template object identity (ES2015 12.2.9.3), scoped to
        // THIS compilation so two distinct parse nodes — the same source `eval`'d twice —
        // get distinct template objects while re-executions of one parse node share one.
        var cacheKey = unchecked((((compilationId * 397) ^ template.Start.Span.Offset) * 397) ^ rawHash);
        var partsArray = BExpression.Call(null, GetOrCreateTemplateObjectMethod, BExpression.Constant(cacheKey), unfrozenArray);
        args[0] = partsArray;

        // A parenthesised optional chain closes the chain at the parens, but its member access is still
        // the tag's Reference: `(o?.m)`x`` calls o.m with `this` being `o`, as `(o?.m)()` does
        // (VisitCallExpression unwraps a call's callee the same way). See InvokeChainBoundaryTag for
        // the chain that short-circuits.
        var chainBoundary = false;
        if (callee is AstOptionalChain wrapped && wrapped.Expression is AstMemberExpression)
        {
            callee = wrapped.Expression;
            chainBoundary = true;
        }

        if (callee.Type == FastNodeType.MemberExpression && callee is AstMemberExpression me)
        {
            // The tag's key, read as a method call's is (VisitCallExpression): a private name, a literal
            // of any kind, a member read, or any other expression evaluated at run time. Anything but the
            // first four kinds used to fail to compile -- `o[f()]`x``, `o[a + b]`x``, `o[null]`x`` -- and a
            // private name was looked up as a public property spelled "#m".
            BExpression name;
            var isPrivateMethodKey = false;

            switch (me.Property.Type)
            {
                case FastNodeType.Identifier:
                    var id = (me.Property as AstIdentifier)!;
                    if (!me.Computed && id.Name.Length > 0 && id.Name.Value[0] == '#')
                    {
                        name = KeyOfPrivateName(id.Name);
                        isPrivateMethodKey = true;
                    }
                    else
                    {
                        name = me.Computed ? VisitExpression(id) : KeyOfName(id.Name);
                    }
                    break;

                case FastNodeType.Literal:
                    var l = (me.Property as AstLiteral)!;
                    if (l.TokenType == TokenTypes.String)
                        name = KeyOfName(l.Start.CookedText);
                    else if (l.TokenType == TokenTypes.Number)
                        name = GetLiteralPropertyKey(l);
                    else
                        // null / bigint / regexp / etc.: evaluated and coerced to a key at run time.
                        name = VisitLiteral(l);
                    break;

                case FastNodeType.MemberExpression:
                    name = VisitMemberExpression(me.Property as AstMemberExpression);
                    break;

                default:
                    name = Visit(me.Property);
                    break;
            }

            if (me.Object.Type == FastNodeType.Super)
            {
                // The superclass's method runs with the caller's `this`, as a super call's does: the
                // lexical binding, so that an arrow function inside the method passes the method's
                // receiver. It was called with `undefined`.
                var superMethod = JSValueBuilder.Index(scope.Top.Super, name, me.Coalesce);
                return JSFunctionBuilder.InvokeFunction(
                    superMethod, ArgumentsBuilder.New(scope.Top.ThisExpression, args), me.Coalesce);
            }

            var target = VisitExpression(me.Object);
            if (chainBoundary)
                return InvokeChainBoundaryTag(target, name, isPrivateMethodKey, args, me.Coalesce);

            // The receiver and the resolved method go into temps, as a method call's do. A pooled pair is
            // handed to every call compiled while it is held -- a call inside this tag's key or its
            // substitutions included -- which is safe only for an operand that runs no code between the
            // receiver's assignment and the invocation that reads it back as `this`. A computed key runs
            // exactly there, in any function: `o[f()]`x`` called o's method on f. And in a generator or
            // async body the rewrite may hoist a block-valued operand into that window, as it does a
            // method call's. Either way the tag gets locals of its own, which no other call can be handed
            // (VisitCallExpression has the long account; MayHoist is its test).
            var ownTemps = me.Computed && MayHoist(name)
                || scope.Top.Generator != null && (MayHoist(name) || MayHoist(args));

            using var te = scope.Top.GetTempVariable(typeof(JSValue));
            using var te2 = scope.Top.GetTempVariable(typeof(JSValue));
            var receiver = ownTemps ? BExpression.Parameter(typeof(JSValue), "#recv") : te.Variable;
            var resolvedMethod = ownTemps ? BExpression.Parameter(typeof(JSValue), "#callee") : te2.Variable;

            BExpression invocation;
            BParameterExpression privateKeyLocal = null;
            if (isPrivateMethodKey)
            {
                // A private name is a per-evaluation key captured from the class scope, and InvokeMethod
                // takes its key by address, which a captured variable cannot give: it is copied into a
                // method-local temp first, pooled or this tag's own as the receiver is.
                using var keyTemp = scope.Top.GetTempVariable(typeof(KeyString));
                privateKeyLocal = ownTemps ? BExpression.Parameter(typeof(KeyString), "#key") : null;
                var key = privateKeyLocal ?? keyTemp.Variable;
                invocation = BExpression.Block(
                [
                    BExpression.Assign(key, name),
                    JSValueBuilder.InvokeMethod(receiver, resolvedMethod, target, key, args, false, me.Coalesce),
                ]);
            }
            else
            {
                invocation = JSValueBuilder.InvokeMethod(receiver, resolvedMethod, target, name, args, false, me.Coalesce);
            }

            if (ownTemps)
            {
                var locals = new Sequence<BParameterExpression>
                {
                    (BParameterExpression)receiver,
                    (BParameterExpression)resolvedMethod,
                };

                if (privateKeyLocal != null)
                    locals.Add(privateKeyLocal);

                invocation = BExpression.Block(locals, invocation);
            }

            return invocation;
        }
        else
        {
            bool isSuper = callee.Type == FastNodeType.Super;

            if (isSuper)
            {
                var paramArray1 = ArgumentsBuilder.New(JSUndefinedBuilder.Value, args);
                var superNewTarget = scope.Top.NewTargetExpression ?? JSUndefinedBuilder.Value;
                return JSFunctionBuilder.InvokeSuperConstructor(scope.Top.Super, superNewTarget, scope.Top.ThisExpression, paramArray1);
            }

            var target = VisitExpression(callee);
            return JSFunctionBuilder.InvokeFunction(target, ArgumentsBuilder.New(JSUndefinedBuilder.Value, args));
        }
    }

    /// <summary>
    /// The tag <c>(o?.m)`x`</c>: a parenthesised optional chain whose last link is a member access.
    /// </summary>
    /// <remarks>
    /// Where the chain runs to its end, the tag is called on the object it was read from, as any member
    /// tag is. Where it short-circuits -- <c>o</c> is nullish, or an earlier link was -- the parens give the
    /// chain's value, <c>undefined</c>, as the tag, and calling that is a <c>TypeError</c>, thrown by the
    /// call after the substitutions have been evaluated, as Chromium does. The short-circuit InvokeMethod
    /// builds into a chain would skip the call and answer <c>undefined</c> instead, so this tag is lowered
    /// on its own, with locals of its own.
    /// </remarks>
    private BExpression InvokeChainBoundaryTag(
        BExpression target, BExpression name, bool isPrivateKey, Sequence<BExpression> args, bool memberCoalesce)
    {
        var receiver = BExpression.Parameter(typeof(JSValue), "#recv");
        var method = BExpression.Parameter(typeof(JSValue), "#callee");
        var locals = new Sequence<BParameterExpression> { receiver, method };
        var body = new Sequence<BExpression> { BExpression.Assign(receiver, target) };

        // A private name's key is read by address, as in InvokeMethod: copied into a local first.
        var key = name;
        if (isPrivateKey)
        {
            var keyLocal = BExpression.Parameter(typeof(KeyString), "#key");
            locals.Add(keyLocal);
            body.Add(BExpression.Assign(keyLocal, name));
            key = keyLocal;
        }

        BExpression shortCircuited = JSValueBuilder.IsOptionalChainSkip(receiver);
        if (memberCoalesce)
            shortCircuited = BExpression.OrElse(shortCircuited, JSValueBuilder.IsNullOrUndefined(receiver));

        body.Add(BExpression.IfThen(
            shortCircuited,
            BExpression.Block(
                BExpression.Assign(receiver, JSUndefinedBuilder.Value),
                BExpression.Assign(method, JSUndefinedBuilder.Value)),
            BExpression.Assign(method, JSValueBuilder.Index(receiver, key))));
        body.Add(JSFunctionBuilder.InvokeFunction(method, ArgumentsBuilder.New(receiver, args)));
        return BExpression.Block(locals, body);
    }
}
