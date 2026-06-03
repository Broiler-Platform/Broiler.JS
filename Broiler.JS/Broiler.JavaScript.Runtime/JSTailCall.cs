namespace Broiler.JavaScript.Runtime;

public sealed class JSTailCall(JSValue target, Arguments arguments) : JSObject
{
    internal JSValue Target { get; } = target;
    internal Arguments Arguments { get; } = arguments;

    public static JSValue Resolve(JSValue value)
    {
        while (value is JSTailCall tailCall)
        {
            var arguments = tailCall.Arguments;
            value = tailCall.Target.InvokeFunction(in arguments);
        }

        return value;
    }
}
