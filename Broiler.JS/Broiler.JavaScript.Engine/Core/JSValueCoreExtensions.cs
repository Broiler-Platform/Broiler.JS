using System.Runtime.CompilerServices;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Core;

/// <summary>
/// Initializes <see cref="JSValue"/> factory delegates so that Runtime
/// types can create concrete JS values without referencing Core directly.
/// </summary>
internal static class JSValueCoreExtensions
{
    [ModuleInitializer]
    internal static void InitializeFactories()
    {
        JSValue.UndefinedValue = JSUndefined.Value;

        // Forward the caller info the throw site captured. Dropping it here — the
        // shape this had while the field was a Func<string, Exception> — made this
        // line the recorded origin of every TypeError the engine raises.
        JSValue.NewTypeError = static (msg, function, filePath, line) =>
            JSEngine.NewTypeError(msg, function, filePath, line);
        JSValue.ForceConvertHelper = (jsValue, type, _) =>
        {
            var protoObj = (jsValue.prototypeChain as IJSPrototype)?.Object as JSObject;
            if (protoObj != null
                && JSEngine.ClrInterop.TryUnwrapClrObject(protoObj, out var clrObj))
            {
                if (((System.Type)type).IsAssignableFrom(clrObj.GetType()))
                    return clrObj;
            }
            return null;
        };
        JSValue.InvokePropertyGetter = (getter, receiver) => getter is IJSFunction fn ? fn.InvokeFunction(new Arguments(receiver)) : JSValue.UndefinedValue;
        JSValue.CreatePrototypeObject = value => (value as JSObject)?.PrototypeObject;
        Arguments.Empty = new Arguments(JSUndefined.Value);

    }
}
