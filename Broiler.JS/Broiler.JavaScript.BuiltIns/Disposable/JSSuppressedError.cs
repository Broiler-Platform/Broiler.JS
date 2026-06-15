using Broiler.JavaScript.BuiltIns.Error;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
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
        base(new Arguments(JSUndefined.Value, a[2] ?? new JSString("Suppressed Error")), function: function, filePath: filePath, line: line)
    {
        FastAddValue(ErrorKey, a.GetAt(0), JSPropertyAttributes.ConfigurableValue);
        FastAddValue(SuppressedKey, a.GetAt(1), JSPropertyAttributes.ConfigurableValue);
    }

    public JSSuppressedError(JSValue error, JSValue suppressed, string message = "An error was suppressed during disposal.", [CallerMemberName] string function = null,
        [CallerFilePath] string filePath = null, [CallerLineNumber] int line = 0) : base(new JSException(new JSString(message), function, filePath, line))
    {
        Exception.With(this);
        FastAddValue(ErrorKey, error ?? JSUndefined.Value, JSPropertyAttributes.ConfigurableValue);
        FastAddValue(SuppressedKey, suppressed ?? JSUndefined.Value, JSPropertyAttributes.ConfigurableValue);
    }
}
