using Broiler.JavaScript.Portable;

namespace Broiler.JavaScript.Portable.Tests;

public sealed class PortableCompilerTests
{
    private const string FibonacciSource = """
        function fibonacci(n) {
          var a = 0;
          var b = 1;
          var i = 0;
          while (i < n) {
            var next = a + b;
            a = b;
            b = next;
            i = i + 1;
          }
          return a;
        }
        """;

    [Fact]
    public void OfflineCompilerAndInterpreterExecuteNumericControlFlow()
    {
        var program = new PortableCompiler().Compile(FibonacciSource);

        Assert.Equal("fibonacci", program.Name);
        Assert.Equal(1, program.ParameterCount);
        Assert.Equal(9_227_465, PortableInterpreter.Execute(program, [35]));
        Assert.Contains("PortableOpCode.JumpIfFalse", PortableCompiler.EmitCSharp(program, "Sample", "Generated"));
    }

    // 2**-25 and the double below it share .NET's "R" text, 2.980232238769531E-08, which reads back
    // as the second: the emitted constant is the value's bits, exact for every double.
    [Fact]
    public void EmittedConstantsAreTheValuesBits()
    {
        var program = new PortableCompiler().Compile("function f() { return 2.9802322387695312e-8; }");
        var emitted = PortableCompiler.EmitCSharp(program, "Sample", "Generated");

        Assert.Equal(Math.Pow(2, -25), PortableInterpreter.Execute(program, []));
        Assert.Contains("System.BitConverter.UInt64BitsToDouble(0x3E60000000000000UL)", emitted);
        Assert.DoesNotContain("E-08", emitted);
    }

    [Fact]
    public void AnInfiniteConstantIsEmittedAsCompilableCSharp()
    {
        var program = new PortableCompiler().Compile("function f() { return 1e400; }");
        var emitted = PortableCompiler.EmitCSharp(program, "Sample", "Generated");

        Assert.Equal(double.PositiveInfinity, PortableInterpreter.Execute(program, []));
        Assert.Contains("System.BitConverter.UInt64BitsToDouble(0x7FF0000000000000UL)", emitted);
        Assert.DoesNotContain("Infinity", emitted);
    }

    [Fact]
    public void UnsupportedObjectSurfaceIsRejectedAtOfflineCompileTime()
    {
        var error = Assert.Throws<NotSupportedException>(() =>
            new PortableCompiler().Compile("function f() { var o = {}; return o.x; }"));

        Assert.Contains("does not support", error.Message);
    }

    [Fact]
    public void CompilerCanBeReusedWithoutLeakingPreviousProgramState()
    {
        var compiler = new PortableCompiler();
        var first = compiler.Compile(FibonacciSource);
        var second = compiler.Compile(FibonacciSource);

        Assert.Equal(first.Instructions.ToArray(), second.Instructions.ToArray());
        Assert.Equal(9_227_465, PortableInterpreter.Execute(second, [35]));
    }

    [Fact]
    public void ProgramValidationRejectsOperandStackUnderflow()
    {
        var error = Assert.Throws<ArgumentException>(() => new PortableProgram(
            "invalid",
            parameterCount: 0,
            localCount: 0,
            maximumStack: 1,
            [new PortableInstruction(PortableOpCode.Pop), new PortableInstruction(PortableOpCode.Return)]));

        Assert.Contains("underflow", error.Message);
    }

    [Fact]
    public void ConstantBindingsAreRejectedInsteadOfLosingAssignmentSemantics()
    {
        var error = Assert.Throws<NotSupportedException>(() => new PortableCompiler().Compile(
            "function invalid() { const value = 1; value += 1; return value; }"));

        Assert.Contains("var numeric", error.Message);
    }
}
