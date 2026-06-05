using Broiler.JavaScript.ExpressionCompiler;
using System;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Objects;

/// <summary>
/// The <c>Atomics</c> namespace object. Currently only exposes
/// <see cref="Pause"/>; the shared-memory atomic operations are not yet
/// implemented.
/// </summary>
[JSClassGenerator("Atomics"), JSInternalObject]
public partial class JSAtomics : JSObject
{
    /// <summary>
    /// <c>Atomics.pause( [ N ] )</c> — a micro-architectural hint that the
    /// current code is in a spin-wait loop. We have nothing to signal, so this
    /// is a no-op that validates its optional argument and returns undefined.
    /// Per sec-atomics.pause: if N is neither undefined nor an integral Number,
    /// throw a TypeError.
    /// </summary>
    [JSExport("pause", Length = 0)]
    public static JSValue Pause(in Arguments a)
    {
        var n = a.GetAt(0);
        if (!n.IsUndefined)
        {
            if (!n.IsNumber)
                throw JSEngine.NewTypeError("Atomics.pause argument must be an integral Number");

            var value = n.DoubleValue;
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Floor(value) != value)
                throw JSEngine.NewTypeError("Atomics.pause argument must be an integral Number");
        }

        return JSUndefined.Value;
    }
}
