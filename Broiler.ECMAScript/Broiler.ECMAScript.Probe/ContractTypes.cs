using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Probe;

/// <summary>
/// The algorithms library's contract types, exercised in the image: a meter charged and one that
/// throws, a failure's validation, and the limits' answers. Each check names what it found when it
/// does not hold.
/// </summary>
internal static class ContractTypes
{
    internal static IEnumerable<string> Check()
    {
        ulong charged = 0;
        WorkCharge meter = units => charged += units;
        meter(3);
        meter(4);
        if (charged != 7)
            yield return "WorkCharge: a meter charged 3 and 4 holds " + charged.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);

        WorkCharge exhausted = static _ => throw new OperationCanceledException("the budget is spent");
        var threw = false;
        try
        {
            exhausted(1);
        }
        catch (OperationCanceledException)
        {
            threw = true;
        }

        if (!threw)
            yield return "WorkCharge: a meter that throws did not";

        var failure = new Failure(ResultStatus.Malformed, 4, "number.malformed-exponent");
        if (failure.Status != ResultStatus.Malformed || failure.Position != 4 || failure.MessageId != "number.malformed-exponent")
            yield return "Failure: the fields are not the ones given";
        if (!Throws(static () => new Failure(ResultStatus.Completed, Failure.NoPosition, "x")))
            yield return "Failure: a Completed failure was accepted";
        if (!Throws(static () => new Failure(ResultStatus.Malformed, -2, "x")))
            yield return "Failure: a position below NoPosition was accepted";
        if (!Throws(static () => new Failure(ResultStatus.LimitExceeded, 0, string.Empty)))
            yield return "Failure: an empty message identifier was accepted";

        var length = new LengthLimit(10);
        if (!length.Admits(10) || length.Admits(11))
            yield return "LengthLimit: 10 does not admit exactly 0 to 10";
        if (!Throws(static () => new LengthLimit(-1)))
            yield return "LengthLimit: a negative maximum was accepted";

        var depth = new DepthLimit(1);
        if (!depth.Admits(1) || depth.Admits(2))
            yield return "DepthLimit: 1 does not admit exactly depth 1";
        if (!Throws(static () => new DepthLimit(0)))
            yield return "DepthLimit: a maximum below one was accepted";
    }

    private static bool Throws(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }
}
