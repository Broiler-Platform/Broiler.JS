using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using Broiler.JavaScript.BuiltIns.Array;

namespace Broiler.JavaScript.BuiltIns.Error;

[JSClassGenerator("Error")]
public partial class JSError : JSObject, IJSError
{
    public string Message { get; private set; }
    public string Stack { get; private set; }

    private static readonly KeyString CauseKey = KeyStrings.GetOrCreate("cause");

    Exception IJSError.Exception => Exception;

    // 20.5.8.1 InstallErrorCause(O, options): when options is an object having
    // an own-or-inherited "cause" property, copy it as a non-enumerable data
    // property (configurable, writable, non-enumerable).
    private protected void InstallErrorCause(JSValue options)
    {
        if (options is JSObject optionsObject && optionsObject.HasProperty(CauseKey.ToJSValue()).BooleanValue)
            FastAddValue(CauseKey, optionsObject[CauseKey], JSPropertyAttributes.ConfigurableValue);
    }

    private string CreateStack()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{ToString(Arguments.Empty)}");

        var top = (JSEngine.Current as IJSExecutionContext)?.Top;
        while (top != null)
        {
            // ref var top = ref walker.Current;
            var fx = top.Function;
            var file = top.FileName;

            if (fx.IsNullOrWhiteSpace())
                fx = "native";

            if (string.IsNullOrWhiteSpace(file))
                file = "file";

            sb.AppendLine($"    at {fx}:{file}:{top.Line},{top.Column}");
            top = top.Parent;
        }

        return sb.ToString();
    }

    public JSError(in Arguments a, [CallerMemberName] string function = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) :
        // The [[Construct]] machinery (CreateInstance) has already performed the single
        // OrdinaryCreateFromConstructor read of NewTarget.prototype and pre-created the
        // instance (passed as `this`) carrying that prototype — which it re-applies after the
        // body runs. Reuse that resolved prototype instead of reading NewTarget.prototype a
        // second time here: a second Get is observable (a Proxy/getter on the constructor) and
        // must NOT run again after the message's ToString. Fall back to NewTarget.prototype
        // only when there is no pre-created instance (e.g. an internal direct allocation).
        this((a.This as JSObject)?.GetPrototypeOf() as JSObject ?? JSEngine.NewTargetPrototype)
    {
        Exception = new JSException(this, function: function, filePath: filePath, line: line);
        var hasMessage = a.TryGetAt(0, out var messageValue);
        // The message is coerced with ToString (StringValue), so an object message observes its toString
        // (a thrown value propagates) and a Symbol — directly or returned from toString — is a TypeError,
        // rather than being silently stringified.
        var message = hasMessage ? messageValue.StringValue : string.Empty;

        Message = message;
        Stack = CreateStack();

        if (hasMessage && !messageValue.IsUndefined)
            FastAddValue(KeyStrings.message, CreateString(message), JSPropertyAttributes.ConfigurableValue);

        FastAddValue(KeyStrings.stack, CreateString(Stack), JSPropertyAttributes.ConfigurableValue);

        InstallErrorCause(a.GetAt(1));
    }

    [JSExport("isError", Feature = (int)JavaScriptFeatureFlags.ErrorIsError)]
    internal static JSValue IsError(in Arguments a)
    {
        var arg = a.Get1();
        return arg is JSError ? BooleanTrue : BooleanFalse;
    }

    [JSExport("toString")]
    public new JSValue ToString(in Arguments a)
    {
        var name = prototypeChain.Object[KeyStrings.constructor][KeyStrings.name];
        return CreateString($"{name}: {Message}");
    }

    public override string ToString() => ToString(Arguments.Empty).ToString();

    public override string ToDetailString() => ToString(Arguments.Empty).ToString() + "\r\n" + Exception.JSStackTrace.ToString();

    public JSException Exception { get; }

    internal protected JSError(JSException ex, JSObject prototype = null) : base(prototype)
    {
        Exception = ex;
        ex.Error ??= this;
        Message = ex.RawMessage ?? ex.Message;
        Stack = ex.JSStackTrace.ToString();

        FastAddValue(KeyStrings.message, CreateString(Message), JSPropertyAttributes.ConfigurableValue);
        FastAddValue(KeyStrings.stack, CreateString(Stack), JSPropertyAttributes.ConfigurableValue);
    }

    internal JSError(JSException ex, string msg) : this()
    {
        Exception = ex;
        ex.Error ??= this;
        Message = msg;
        Stack = ex.JSStackTrace.ToString();

        FastAddValue(KeyStrings.message, CreateString(msg), JSPropertyAttributes.ConfigurableValue);
        FastAddValue(KeyStrings.stack, CreateString(Stack), JSPropertyAttributes.ConfigurableValue);
    }

    public static JSValue From(Exception ex)
    {
        return JSException.ErrorFrom(ex);
    }
}

[JSClassGenerator("TypeError"), JSBaseClass("Error")]
public partial class JSTypeError : JSError
{
    public JSTypeError(in Arguments a, [CallerMemberName] string function = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) :
        base(in a, function: function, filePath: filePath, line: line)
    { }
}

[JSClassGenerator("SyntaxError"), JSBaseClass("Error")]
public partial class JSSyntaxError : JSError
{
    public JSSyntaxError(in Arguments a, [CallerMemberName] string function = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) :
        base(in a, function: function, filePath: filePath, line: line)
    { }
}

[JSClassGenerator("URIError"), JSBaseClass("Error")]
public partial class JSURIError : JSError
{
    public JSURIError(in Arguments a, [CallerMemberName] string function = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) :
        base(in a, function: function, filePath: filePath, line: line)
    { }
}

[JSClassGenerator("RangeError"), JSBaseClass("Error")]
public partial class JSRangeError : JSError
{
    public JSRangeError(in Arguments a, [CallerMemberName] string function = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) :
        base(in a, function: function, filePath: filePath, line: line)
    { }
}

[JSClassGenerator("ReferenceError"), JSBaseClass("Error")]
public partial class JSReferenceError : JSError
{
    public JSReferenceError(in Arguments a, [CallerMemberName] string function = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) :
        base(in a, function: function, filePath: filePath, line: line)
    { }
}

[JSClassGenerator("EvalError"), JSBaseClass("Error")]
public partial class JSEvalError : JSError
{
    public JSEvalError(in Arguments a, [CallerMemberName] string function = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) :
        base(in a, function: function, filePath: filePath, line: line)
    { }
}

[JSClassGenerator("AggregateError"), JSBaseClass("Error")]
public partial class JSAggregateError : JSError
{
    private static readonly KeyString ErrorsKey = KeyStrings.GetOrCreate("errors");

    [JSExport(Length = 2)]
    public JSAggregateError(in Arguments a, [CallerMemberName] string function = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) :
        base(new Arguments(a.This, a.GetAt(1)), function: function, filePath: filePath, line: line)
    {
        FastAddValue(ErrorsKey, JSArray.StaticFrom(new Arguments(JSUndefined.Value, a.GetAt(0))), JSPropertyAttributes.ConfigurableValue);

        // AggregateError(errors, message, options): options is the third argument.
        InstallErrorCause(a.GetAt(2));
    }
}
