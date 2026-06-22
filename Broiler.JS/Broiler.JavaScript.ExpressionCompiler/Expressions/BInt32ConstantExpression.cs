using System;
using System.CodeDom.Compiler;
using System.Reflection;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;


public class BInt32ConstantExpression(int value) : BExpression(BExpressionType.Int32Constant, typeof(int))
{
    public readonly int Value = value;

    public override void Print(IndentedTextWriter writer) => writer.Write(Value);

    private static BInt32ConstantExpression MinusOne = new(-1);

    private static BInt32ConstantExpression _0 = new(0);

    private static BInt32ConstantExpression _1 = new(1);

    private static BInt32ConstantExpression _2 = new(2);

    private static BInt32ConstantExpression _3 = new(3);

    private static BInt32ConstantExpression _4 = new(4);

    private static BInt32ConstantExpression _5 = new(5);

    private static BInt32ConstantExpression _6 = new(6);

    private static BInt32ConstantExpression _7 = new(7);

    private static BInt32ConstantExpression _8 = new(8);

    private static BInt32ConstantExpression _16 = new(16);

    private static BInt32ConstantExpression _32 = new(32);

    private static BInt32ConstantExpression _64 = new(64);

    private static BInt32ConstantExpression _128 = new(128);

    private static BInt32ConstantExpression _256 = new(256);

    private static BInt32ConstantExpression _512 = new(512);

    private static BInt32ConstantExpression _1024 = new(1024);

    internal static BInt32ConstantExpression For(int value)
    {
        return value switch
        {
            -1 => MinusOne,
            0 => _0,
            1 => _1,
            2 => _2,
            3 => _3,
            4 => _4,
            5 => _5,
            6 => _6,
            7 => _7,
            8 => _8,
            16 => _16,
            32 => _32,
            64 => _64,
            128 => _128,
            256 => _256,
            512 => _512,
            1024 => _1024,
            _ => new BInt32ConstantExpression(value),
        };
    }
}

public class BUInt32ConstantExpression(uint value) : BExpression(BExpressionType.UInt32Constant, typeof(uint))
{
    public readonly uint Value = value;

    public override void Print(IndentedTextWriter writer) => writer.Write(Value);

    private static BUInt32ConstantExpression _0 = new(0);

    private static BUInt32ConstantExpression _1 = new(1);

    private static BUInt32ConstantExpression _2 = new(2);

    private static BUInt32ConstantExpression _3 = new(3);

    private static BUInt32ConstantExpression _4 = new(4);

    private static BUInt32ConstantExpression _5 = new(5);

    private static BUInt32ConstantExpression _6 = new(6);

    private static BUInt32ConstantExpression _7 = new(7);

    private static BUInt32ConstantExpression _8 = new(8);

    private static BUInt32ConstantExpression _16 = new(16);

    private static BUInt32ConstantExpression _32 = new(32);

    private static BUInt32ConstantExpression _64 = new(64);

    private static BUInt32ConstantExpression _128 = new(128);

    private static BUInt32ConstantExpression _256 = new(256);

    private static BUInt32ConstantExpression _512 = new(512);

    private static BUInt32ConstantExpression _1024 = new(1024);

    internal static BUInt32ConstantExpression For(uint value)
    {
        return value switch
        {
            0 => _0,
            1 => _1,
            2 => _2,
            3 => _3,
            4 => _4,
            5 => _5,
            6 => _6,
            7 => _7,
            8 => _8,
            16 => _16,
            32 => _32,
            64 => _64,
            128 => _128,
            256 => _256,
            512 => _512,
            1024 => _1024,
            _ => new BUInt32ConstantExpression(value),
        };
    }
}

public class BInt64ConstantExpression(long value) : BExpression(BExpressionType.Int64Constant, typeof(long))
{
    public readonly long Value = value;

    public override void Print(IndentedTextWriter writer) => writer.Write(Value);
}

public class BUInt64ConstantExpression(ulong value) : BExpression(BExpressionType.UInt64Constant, typeof(ulong))
{
    public readonly ulong Value = value;

    public override void Print(IndentedTextWriter writer) => writer.Write(Value);
}

public class BDoubleConstantExpression(double value) : BExpression(BExpressionType.DoubleConstant, typeof(double))
{
    public readonly double Value = value;

    public override void Print(IndentedTextWriter writer) => writer.Write(Value);
}

public class BFloatConstantExpression(float value) : BExpression(BExpressionType.FloatConstant, typeof(float))
{
    public readonly float Value = value;

    public override void Print(IndentedTextWriter writer) => writer.Write(Value);
}

public class BBooleanConstantExpression : BExpression
{
    public readonly bool Value;

    public static BBooleanConstantExpression True = new(true);

    public static BBooleanConstantExpression False = new(false);

    private BBooleanConstantExpression(bool value) : base(BExpressionType.BooleanConstant, typeof(bool)) => Value = value;

    public override void Print(IndentedTextWriter writer) => writer.Write(Value);
}

public class BByteConstantExpression(byte value) : BExpression(BExpressionType.ByteConstant, typeof(byte))
{
    public readonly byte Value = value;

    public override void Print(IndentedTextWriter writer) => writer.Write(Value);
}
public class BStringConstantExpression(string value) : BExpression(BExpressionType.StringConstant, typeof(string))
{
    public readonly string Value = value;

    public override void Print(IndentedTextWriter writer) => writer.Write(Value);
}

public class BTypeConstantExpression(Type value) : BExpression(BExpressionType.TypeConstant, typeof(Type))
{
    public readonly Type Value = value;

    public override void Print(IndentedTextWriter writer) => writer.Write(Value);
}

public class BMethodConstantExpression(MethodInfo value) : BExpression(BExpressionType.MethodConstant, typeof(Type))
{
    public readonly MethodInfo Value = value;

    public override void Print(IndentedTextWriter writer) => writer.Write(Value);
}
