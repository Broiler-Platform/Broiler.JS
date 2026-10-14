// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at a04b2e5 (SJL-3's merge, bundle sjl-4-001's base):
//   src/Broiler.VM.Profile.JavaScript/JsValue.cs
//     blob 147dce02db1ed916b1d19c2b456e2df6ec2bb3e0: the statics ToInt32(double), ToUint32(double) and ToInteger(double)
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; the three statics sit in a static class JsValue of their own instead of the readonly struct JsValue, whose other members are the engine's;
//   - the generated assurance header and the per-member assurance annotations (Broiler-AI / Broiler-Human / Broiler-Falsified-If lines) removed;
//   - the SPDX lines above the assurance header removed.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.V;

/// <summary>The double-taking conversions of Broiler.VM's <c>JsValue</c>: <c>ToInt32</c>, <c>ToUint32</c> and <c>ToIntegerOrInfinity</c>.</summary>
internal static class JsValue
{
    /// <summary>The abstract operation <c>ToInt32</c>.</summary>
    internal static int ToInt32(double value) => unchecked((int)ToUint32(value));

    /// <summary>The abstract operation <c>ToUint32</c>.</summary>
    internal static uint ToUint32(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        var truncated = System.Math.Truncate(value) % 4294967296.0;

        if (truncated < 0)
        {
            truncated += 4294967296.0;
        }

        return (uint)truncated;
    }

    /// <summary>The abstract operation <c>ToIntegerOrInfinity</c>.</summary>
    internal static double ToInteger(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        // NEVER -0 (since 2026-09-22, JSeal VM-FIX-J): the operation answers a mathematical
        // integer, which has no sign of zero, so -0 and every value truncating to it are +0. Adding
        // +0 is the IEEE spelling of that; `[true].indexOf(true, -0)` answered -0 without it.
        return double.IsInfinity(value) ? value : System.Math.Truncate(value) + 0.0;
    }
}
