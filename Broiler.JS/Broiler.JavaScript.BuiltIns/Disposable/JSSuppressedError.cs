using Broiler.JavaScript.BuiltIns.Error;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.BuiltIns.Disposable;

[JSClassGenerator("SuppressedError"), JSBaseClass("Error")]
public partial class JSSuppressedError : JSError
{
    // Per spec `error` and `suppressed` are non-enumerable data properties created on each *instance*
    // (like Error's `message` / AggregateError's `errors`), not accessors on SuppressedError.prototype
    // — SuppressedError.prototype.hasOwnProperty("error"/"suppressed") must be false.
    private static readonly KeyString ErrorKey = KeyStrings.GetOrCreate("error");
    private static readonly KeyString SuppressedKey = KeyStrings.GetOrCreate("suppressed");

    [JSExport(Length = 3)]
    public JSSuppressedError(in Arguments a, [CallerMemberName] string function = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) :
        // Forward the user-supplied `message` (third argument) untouched. When it is
        // absent or undefined the Error base constructor creates no own `message`
        // property — SuppressedError must not invent a default one
        // (test262: built-ins/SuppressedError/message-undefined-no-prop).
        base(new Arguments(JSUndefined.Value, a.GetAt(2)), function: function, filePath: filePath, line: line)
    {
        DefineErrorAndSuppressed(a.GetAt(0), a.GetAt(1));
    }

    public JSSuppressedError(JSValue error, JSValue suppressed, string message = "An error was suppressed during disposal.", [CallerMemberName] string function = null,
        [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) : base(new JSException(new JSString(message), function, filePath, line))
    {
        Exception.With(this);
        DefineErrorAndSuppressed(error ?? JSUndefined.Value, suppressed ?? JSUndefined.Value);
    }

    // SuppressedError steps 4-5 create `error` then `suppressed` as the own properties
    // immediately following `message`. The Error base constructor has already appended
    // the implementation-defined `stack` property right after `message`, so relocate it
    // to the end — keeping message/error/suppressed consecutive, as test262
    // SuppressedError/order-of-args-evaluation requires (impl-defined properties are only
    // permitted before `message` or after `suppressed`).
    private void DefineErrorAndSuppressed(JSValue error, JSValue suppressed)
    {
        var stackKeyValue = KeyStrings.stack.ToJSValue();
        var hadStack = !GetOwnPropertyDescriptor(stackKeyValue).IsUndefined;
        var stackValue = hadStack ? this[KeyStrings.stack] : JSUndefined.Value;
        if (hadStack)
            Delete(stackKeyValue);

        FastAddValue(ErrorKey, error, JSPropertyAttributes.ConfigurableValue);
        FastAddValue(SuppressedKey, suppressed, JSPropertyAttributes.ConfigurableValue);

        if (hadStack)
            FastAddValue(KeyStrings.stack, stackValue, JSPropertyAttributes.ConfigurableValue);
    }
}
