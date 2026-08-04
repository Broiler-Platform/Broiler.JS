using System;
using System.Threading;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Process-wide switch for item 3-1's <c>ToNumeric</c> reuse.
/// </summary>
/// <remarks>
/// <para>
/// <c>ToNumeric</c> coerces the operand of <c>++</c>/<c>--</c> and hands back the coerced old
/// value. It minted unconditionally, so an operand that was <em>already</em> a
/// <c>JSNumber</c> was copied into a second, equal <c>JSNumber</c> — and a JavaScript Number has
/// no observable identity, so the copy could never be detected. Item 3-1's boxing-source census
/// priced that at <b>17 281 232 requests, 15.4% of everything the corpus boxes</b>, which is the
/// largest single removable population the phase has found and the cheapest to remove.
/// </para>
/// <para>
/// The switch exists for the reason <c>BROILER_JS_NUMERIC_SPECULATION</c> does, not because the
/// change is risky: an allocation removed is only a measurement if it can be measured against a
/// build that differs in nothing else. Setting <c>BROILER_JS_NUMERIC_UPDATE_REUSE=0</c> restores
/// the unconditional mint.
/// </para>
/// </remarks>
public static class NumericUpdateReuse
{
    public const string EnvironmentVariable = "BROILER_JS_NUMERIC_UPDATE_REUSE";

    private static int enabled = ReadConfigured();

    /// <summary>Whether an already-Number operand of <c>++</c>/<c>--</c> is handed back as is.</summary>
    public static bool Enabled
    {
        get => Volatile.Read(ref enabled) != 0;
        set => Volatile.Write(ref enabled, value ? 1 : 0);
    }

    private static int ReadConfigured()
        => string.Equals(
            Environment.GetEnvironmentVariable(EnvironmentVariable),
            "0",
            StringComparison.Ordinal)
            ? 0
            : 1;
}
