using System;

namespace Broiler.JavaScript.Portable;

/// <summary>
/// Dynamic-code-free numeric stack interpreter. It deliberately has no JS object model,
/// reflection, host callbacks, eval, modules, async work, or runtime compilation.
/// </summary>
public static class PortableInterpreter
{
    public static double Execute(PortableProgram program, ReadOnlySpan<double> arguments)
    {
        ArgumentNullException.ThrowIfNull(program);
        if (arguments.Length < program.ParameterCount)
            throw new ArgumentException("Not enough arguments for the portable program.", nameof(arguments));

        Span<double> locals = program.LocalCount <= 128
            ? stackalloc double[program.LocalCount]
            : new double[program.LocalCount];
        locals.Clear();
        Span<double> stack = program.MaximumStack <= 256
            ? stackalloc double[program.MaximumStack]
            : new double[program.MaximumStack];
        var instructions = program.Instructions;
        var stackPointer = 0;
        var instructionPointer = 0;

        while ((uint)instructionPointer < (uint)instructions.Length)
        {
            var instruction = instructions[instructionPointer++];
            switch (instruction.OpCode)
            {
                case PortableOpCode.PushConstant:
                    stack[stackPointer++] = instruction.Number;
                    break;
                case PortableOpCode.LoadArgument:
                    stack[stackPointer++] = arguments[instruction.Operand];
                    break;
                case PortableOpCode.LoadLocal:
                    stack[stackPointer++] = locals[instruction.Operand];
                    break;
                case PortableOpCode.StoreLocal:
                    locals[instruction.Operand] = stack[--stackPointer];
                    break;
                case PortableOpCode.Duplicate:
                    stack[stackPointer] = stack[stackPointer - 1];
                    stackPointer++;
                    break;
                case PortableOpCode.Pop:
                    stackPointer--;
                    break;
                case PortableOpCode.Add:
                    Binary(ref stackPointer, stack, static (left, right) => left + right);
                    break;
                case PortableOpCode.Subtract:
                    Binary(ref stackPointer, stack, static (left, right) => left - right);
                    break;
                case PortableOpCode.Multiply:
                    Binary(ref stackPointer, stack, static (left, right) => left * right);
                    break;
                case PortableOpCode.Divide:
                    Binary(ref stackPointer, stack, static (left, right) => left / right);
                    break;
                case PortableOpCode.Remainder:
                    Binary(ref stackPointer, stack, static (left, right) => left % right);
                    break;
                case PortableOpCode.LessThan:
                    Binary(ref stackPointer, stack, static (left, right) => left < right ? 1 : 0);
                    break;
                case PortableOpCode.LessThanOrEqual:
                    Binary(ref stackPointer, stack, static (left, right) => left <= right ? 1 : 0);
                    break;
                case PortableOpCode.GreaterThan:
                    Binary(ref stackPointer, stack, static (left, right) => left > right ? 1 : 0);
                    break;
                case PortableOpCode.GreaterThanOrEqual:
                    Binary(ref stackPointer, stack, static (left, right) => left >= right ? 1 : 0);
                    break;
                case PortableOpCode.Equal:
                    Binary(ref stackPointer, stack, static (left, right) => left == right ? 1 : 0);
                    break;
                case PortableOpCode.NotEqual:
                    Binary(ref stackPointer, stack, static (left, right) => left != right ? 1 : 0);
                    break;
                case PortableOpCode.Jump:
                    instructionPointer = instruction.Operand;
                    break;
                case PortableOpCode.JumpIfFalse:
                    if (!ToBoolean(stack[--stackPointer]))
                        instructionPointer = instruction.Operand;
                    break;
                case PortableOpCode.Return:
                    return stack[--stackPointer];
                default:
                    throw new InvalidOperationException($"Unsupported opcode {instruction.OpCode}.");
            }

            if ((uint)stackPointer > (uint)stack.Length)
                throw new InvalidOperationException("Portable operand stack overflow or underflow.");
        }

        throw new InvalidOperationException("Portable program completed without Return.");
    }

    private static void Binary(
        ref int stackPointer,
        Span<double> stack,
        Func<double, double, double> operation)
    {
        var right = stack[--stackPointer];
        stack[stackPointer - 1] = operation(stack[stackPointer - 1], right);
    }

    private static bool ToBoolean(double value) => value != 0 && !double.IsNaN(value);
}
