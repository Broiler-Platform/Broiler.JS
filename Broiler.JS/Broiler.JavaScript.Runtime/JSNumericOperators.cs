namespace Broiler.JavaScript.Runtime;

/// <summary>
/// The bitwise and shift operators on two operands the compiler has already proved are numbers,
/// computed on raw CLR doubles (docs/performance-roadmap.md item 3-1's successor).
/// </summary>
/// <remarks>
/// <para>
/// <c>NumericLocalAnalysis.IsNumericBinary</c> has always <em>typed</em> these operators — a
/// candidate assigned <c>i &amp; 1023</c> stays numeric — while
/// <c>FastCompiler.TryCreateNativeNumericValue</c> had no native form for them, so the analysis
/// proved something the emitter could not use: `s = i + 1023` costs **0.00 bytes an iteration**
/// and `s = i &amp; 1023` costs **31.84**, with both operands raw doubles and the result stored
/// straight back into one. The value went out to a <c>JSValue</c> operator and came back.
/// </para>
/// <para>
/// The reason it was excluded is real and is why these live here rather than as
/// <c>BExpression</c> nodes: a bitwise operand is not the double, it is
/// <c>ToInt32</c>/<c>ToUint32</c> of it (§7.1.5/§7.1.6), which truncates toward zero and reduces
/// modulo 2^32, maps NaN and the infinities to 0, and is <em>not</em> a plain CLR cast — an
/// out-of-range <c>(int)</c> conversion is undefined in .NET rather than wrapping. Routing every
/// operator through <see cref="JSValue.ToUint32"/>, the same helper <c>JSValue.IntValue</c> uses,
/// makes these identical to the boxed operators by construction rather than by inspection.
/// </para>
/// <para>
/// Shift counts follow the boxed operators exactly, including where they rely on the CLR: C#
/// masks an <c>int</c> or <c>uint</c> shift count to its low five bits, which is what §13.9's
/// <c>ToUint32(rval) &amp; 0x1F</c> requires, so <c>&lt;&lt;</c> and <c>&gt;&gt;&gt;</c> need no
/// explicit mask and <c>&gt;&gt;</c> keeps the one <c>JSValue.RightShift</c> writes out.
/// </para>
/// </remarks>
public static class JSNumericOperators
{
    /// <summary>ToInt32 (§7.1.5), via the same reduction <see cref="JSValue.IntValue"/> uses.</summary>
    public static int ToInt32(double value) => unchecked((int)JSValue.ToUint32(value));

    /// <summary>ToUint32 (§7.1.6).</summary>
    public static uint ToUint32(double value) => JSValue.ToUint32(value);

    public static double BitwiseAnd(double left, double right) => ToInt32(left) & ToInt32(right);

    public static double BitwiseOr(double left, double right) => ToInt32(left) | ToInt32(right);

    public static double BitwiseXor(double left, double right) => ToInt32(left) ^ ToInt32(right);

    // The CLR masks the shift count to five bits for a 32-bit shift, which is exactly
    // ToUint32(rval) & 0x1F — so these match JSValue.LeftShift / RightShift / UnsignedRightShift
    // term for term, including RightShift's explicit mask.
    public static double LeftShift(double left, double right) => ToInt32(left) << ToInt32(right);

    public static double RightShift(double left, double right) => ToInt32(left) >> (ToInt32(right) & 0x1F);

    public static double UnsignedRightShift(double left, double right) => ToUint32(left) >> ToInt32(right);
}
