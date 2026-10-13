// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at a04b2e5 (SJL-3's merge, bundle sjl-4-001's base):
//   src/Broiler.VM.Profile.JavaScript/JsRealm.Binary.cs
//     blob 70f230863c192f03069ba354fb5e9c9372a19083: BinaryToIndex's range rule
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; BinaryToIndex sits in a static class JsRealmBinary of its own instead of the partial class JsRealm, whose other members are the engine's;
//   - the generated assurance header and the per-member assurance annotations (Broiler-AI / Broiler-Human / Broiler-Falsified-If lines) removed;
//   - the SPDX lines above the assurance header removed;
//   - BinaryToIndex made internal (private in the source);
//   - the parameters (JsEngine engine, JsValue value, string what) replaced by the double `number` the source computes as `engine.ToInteger(value)` (JsValue.ToInteger(engine.ToNumber(value))); that line, and the test before it that answers 0 for `undefined` (`value.Type == JsType.Undefined`), are the engine's adapter and are not copied;
//   - the RangeError (`throw engine.Error("RangeError", "Invalid " + what + ": " + JsNumberFormat.NumberToString(number))`) replaced by `return null`, so the return type is double? where the source's is double;
//   - the using directives (System, System.Collections.Generic) dropped: the copied lines need none.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.V;

/// <summary>The range rule of Broiler.VM's <c>ToIndex</c>, over the integer <c>ToIntegerOrInfinity</c> answered.</summary>
internal static class JsRealmBinary
{
    /// <summary>The specification's <c>ToIndex</c>: a non-negative integer a length may be.</summary>
    /// <remarks>
    /// <c>undefined</c> is zero, a fraction truncates, and a negative or an out-of-range value is a
    /// <c>RangeError</c> naming what it was - because a length that silently became zero would
    /// turn <c>new Uint8Array(-1)</c> into an empty array that the calling code then walks
    /// believing it asked for something.
    /// </remarks>
    internal static double? BinaryToIndex(double number)
    {
        if (number < 0 || number > 9007199254740991.0)
        {
            return null;
        }

        return number;
    }
}
