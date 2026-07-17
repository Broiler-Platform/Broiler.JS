using System;
using System.Collections.Generic;

namespace Broiler.JavaScript.Portable;

public enum PortableOpCode : byte
{
    PushConstant,
    LoadArgument,
    LoadLocal,
    StoreLocal,
    Duplicate,
    Pop,
    Add,
    Subtract,
    Multiply,
    Divide,
    Remainder,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Equal,
    NotEqual,
    Jump,
    JumpIfFalse,
    Return,
}

public readonly record struct PortableInstruction(
    PortableOpCode OpCode,
    int Operand = 0,
    double Number = 0);

/// <summary>Validated, immutable numeric bytecode produced by the offline compiler.</summary>
public sealed class PortableProgram
{
    private readonly PortableInstruction[] instructions;

    public PortableProgram(
        string name,
        int parameterCount,
        int localCount,
        int maximumStack,
        PortableInstruction[] instructions)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A portable program requires a name.", nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegative(parameterCount);
        ArgumentOutOfRangeException.ThrowIfNegative(localCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumStack);
        ArgumentNullException.ThrowIfNull(instructions);
        if (instructions.Length == 0 || instructions[^1].OpCode != PortableOpCode.Return)
            throw new ArgumentException("Portable programs must end in Return.", nameof(instructions));

        Name = name;
        ParameterCount = parameterCount;
        LocalCount = localCount;
        MaximumStack = maximumStack;
        this.instructions = (PortableInstruction[])instructions.Clone();
        Validate();
    }

    public string Name { get; }
    public int ParameterCount { get; }
    public int LocalCount { get; }
    public int MaximumStack { get; }
    public ReadOnlySpan<PortableInstruction> Instructions => instructions;

    private void Validate()
    {
        for (var index = 0; index < instructions.Length; index++)
        {
            var instruction = instructions[index];
            if (!Enum.IsDefined(instruction.OpCode))
                throw new ArgumentOutOfRangeException(nameof(instructions), $"Invalid opcode at {index}.");
            switch (instruction.OpCode)
            {
                case PortableOpCode.LoadArgument when instruction.Operand < 0 || instruction.Operand >= ParameterCount:
                    throw new ArgumentOutOfRangeException(nameof(instructions), $"Invalid argument index at {index}.");
                case PortableOpCode.LoadLocal or PortableOpCode.StoreLocal
                    when instruction.Operand < 0 || instruction.Operand >= LocalCount:
                    throw new ArgumentOutOfRangeException(nameof(instructions), $"Invalid local index at {index}.");
                case PortableOpCode.Jump or PortableOpCode.JumpIfFalse
                    when instruction.Operand < 0 || instruction.Operand >= instructions.Length:
                    throw new ArgumentOutOfRangeException(nameof(instructions), $"Invalid jump target at {index}.");
            }
        }

        var stackDepths = new int[instructions.Length];
        Array.Fill(stackDepths, -1);
        var pending = new Queue<int>();
        stackDepths[0] = 0;
        pending.Enqueue(0);
        while (pending.TryDequeue(out var index))
        {
            var instruction = instructions[index];
            var inputDepth = stackDepths[index];
            var required = RequiredStack(instruction.OpCode);
            if (inputDepth < required)
                throw new ArgumentException($"Operand-stack underflow at instruction {index}.", nameof(instructions));

            var outputDepth = inputDepth + StackDelta(instruction.OpCode);
            if (outputDepth > MaximumStack)
                throw new ArgumentException($"MaximumStack is too small at instruction {index}.", nameof(MaximumStack));
            if (instruction.OpCode == PortableOpCode.Return)
                continue;

            if (instruction.OpCode == PortableOpCode.Jump)
            {
                EnqueueSuccessor(instruction.Operand, outputDepth);
                continue;
            }
            if (instruction.OpCode == PortableOpCode.JumpIfFalse)
                EnqueueSuccessor(instruction.Operand, outputDepth);
            if (index + 1 >= instructions.Length)
                throw new ArgumentException($"Reachable path falls off the program at instruction {index}.", nameof(instructions));
            EnqueueSuccessor(index + 1, outputDepth);
        }

        void EnqueueSuccessor(int successor, int depth)
        {
            if (stackDepths[successor] == depth)
                return;
            if (stackDepths[successor] != -1)
                throw new ArgumentException($"Inconsistent operand-stack depth at instruction {successor}.", nameof(instructions));
            stackDepths[successor] = depth;
            pending.Enqueue(successor);
        }
    }

    private static int RequiredStack(PortableOpCode opCode) => opCode switch
    {
        PortableOpCode.StoreLocal or PortableOpCode.Duplicate or PortableOpCode.Pop
            or PortableOpCode.JumpIfFalse or PortableOpCode.Return => 1,
        PortableOpCode.Add or PortableOpCode.Subtract or PortableOpCode.Multiply
            or PortableOpCode.Divide or PortableOpCode.Remainder or PortableOpCode.LessThan
            or PortableOpCode.LessThanOrEqual or PortableOpCode.GreaterThan
            or PortableOpCode.GreaterThanOrEqual or PortableOpCode.Equal
            or PortableOpCode.NotEqual => 2,
        _ => 0,
    };

    private static int StackDelta(PortableOpCode opCode) => opCode switch
    {
        PortableOpCode.PushConstant or PortableOpCode.LoadArgument
            or PortableOpCode.LoadLocal or PortableOpCode.Duplicate => 1,
        PortableOpCode.StoreLocal or PortableOpCode.Pop or PortableOpCode.JumpIfFalse
            or PortableOpCode.Return => -1,
        PortableOpCode.Add or PortableOpCode.Subtract or PortableOpCode.Multiply
            or PortableOpCode.Divide or PortableOpCode.Remainder or PortableOpCode.LessThan
            or PortableOpCode.LessThanOrEqual or PortableOpCode.GreaterThan
            or PortableOpCode.GreaterThanOrEqual or PortableOpCode.Equal
            or PortableOpCode.NotEqual => -1,
        _ => 0,
    };
}
