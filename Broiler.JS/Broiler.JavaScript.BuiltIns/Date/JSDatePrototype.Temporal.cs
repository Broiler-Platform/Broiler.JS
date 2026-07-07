using System.Numerics;
using Broiler.JavaScript.BuiltIns.Temporal;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Date;

public partial class JSDate
{
    // Date.prototype.toTemporalInstant (Temporal proposal §1): returns a Temporal.Instant for
    // this Date's time value (milliseconds → nanoseconds).
    [JSExport("toTemporalInstant", Length = 0)]
    internal JSValue ToTemporalInstant(in Arguments a)
    {
        var ms = GetTimeMs();
        if (double.IsNaN(ms))
            throw JSEngine.NewRangeError("Date.prototype.toTemporalInstant called on an invalid Date");

        var ns = new BigInteger(ms) * 1_000_000;
        return new JSTemporalInstant(ns, JSTemporalInstant.InstantPrototype);
    }
}
