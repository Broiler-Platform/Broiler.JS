// Moved from Broiler.JavaScript.Runtime with its namespace, which keeps a type forwarder to it, and
// compiled as it was there, outside a nullable context, so its public signature reads as it did.
#nullable disable

using Broiler.JavaScript.Runtime.Unicode;
using Broiler.Regex;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Whether a regular-expression pattern is valid under the ES2015+ Unicode (u) flag: a forwarder to
/// Broiler.Regex's validate entry.
/// </summary>
/// <remarks>
/// <para>
/// Kept as a forwarder at milestone SJL-11.5 under decision SJL-D-25 (a) (Broiler.VM's JSD-0081, decision
/// D21): its body is a call of <see cref="BroilerRegex.Validate(string, RegexFlags, Broiler.Unicode.Algorithms.IUnicodeTables, int, out RegexDiagnostic, out RegexLimit)"/>
/// under <see cref="RegexFlags.Unicode"/>, with this engine's Unicode tables and the nesting limit the RegExp
/// constructor compiles under. Until SJL-11.5 it was this engine's own structural check, which the RegExp
/// constructor and the literal's check called; neither calls it now.
/// </para>
/// <para>
/// Held by <c>Broiler.JavaScript.Ast</c> since milestone SJL-12.5, below the parser and the runtime (Broiler.VM's
/// JSD-0080, decision D20). <c>Broiler.JavaScript.Runtime</c> forwards the type here, so an assembly built against
/// it still binds.
/// </para>
/// </remarks>
public static class RegExpUnicodeValidator
{
    /// <summary>
    /// Whether <paramref name="pattern"/> is a valid pattern under the <c>u</c> flag, by the grammar and every early
    /// error, as Broiler.Regex's validate entry answers it.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 22.2.1 and JSD-0081 decision D21; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
    // Broiler-Falsified-If: IsValidUnicodePattern("\\-") or IsValidUnicodePattern("(?<=a)?") answers true, or IsValidUnicodePattern("\\u{1F600}") answers false
    // Broiler-Human:        PENDING
    public static bool IsValidUnicodePattern(string pattern)
        => BroilerRegex.Validate(pattern ?? string.Empty, RegexFlags.Unicode, JSUnicode.Tables, BroilerRegex.MaximumNestingLimit,
            out _, out _) == RegexStatus.Success;
}
