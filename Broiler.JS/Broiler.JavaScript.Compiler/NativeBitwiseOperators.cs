using System;
using System.Threading;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Process-wide switch for emitting the bitwise and shift operators natively on two operands the
/// analysis proved numeric (docs/performance-roadmap.md item 3-1's successor).
/// </summary>
/// <remarks>
/// Present for the same reason <c>BROILER_JS_DEFER_IL</c> and
/// <c>BROILER_JS_CAPTURED_NUMERIC_LOCALS</c> are: a compiler change has to be measurable against
/// a build that differs in nothing else, and comparing two builds compares two builds. Unlike
/// those two this has no losing side by construction — a static call replaces a virtual call
/// <em>plus</em> an allocation — so the switch exists to measure rather than to retreat.
/// </remarks>
public static class NativeBitwiseOperators
{
    public const string EnvironmentVariable = "BROILER_JS_NATIVE_BITWISE";

    private static int enabled = ReadConfigured();

    /// <summary>Whether `&amp;`, `|`, `^`, `&lt;&lt;`, `&gt;&gt;` and `&gt;&gt;&gt;` have a native form.</summary>
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
