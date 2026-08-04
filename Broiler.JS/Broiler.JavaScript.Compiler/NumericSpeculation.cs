using System;
using System.Threading;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Process-wide switch for item 3-1's guarded numeric tree.
/// </summary>
/// <remarks>
/// Present for the same reason <c>BROILER_JS_NATIVE_BITWISE</c> and
/// <c>BROILER_JS_NUMERIC_LOCALS</c> are: the change has a losing side — an arithmetic tree whose
/// operands turn out not to be Numbers pays one type test per guarded leaf and then does exactly
/// what it did before — so it has to be measurable against a build that differs in nothing else.
/// Setting <c>BROILER_JS_NUMERIC_SPECULATION=0</c> restores the unguarded emission.
/// </remarks>
public static class NumericSpeculation
{
    public const string EnvironmentVariable = "BROILER_JS_NUMERIC_SPECULATION";

    private static int enabled = ReadConfigured();

    /// <summary>Whether an arithmetic tree over non-provable operands is speculated on.</summary>
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
