using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Broiler.ECMAScript.Syntax;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime.Unicode;

namespace Broiler.JavaScript.Portable;

/// <summary>Offline compiler for the documented numeric statement subset.</summary>
/// <remarks>
/// <para>
/// <b>It reads the shared tree</b> (milestone SJL-14.3, window W2; Broiler.VM's JSD-0083, decision D41): the source is
/// parsed by the shared syntax library, <c>Broiler.ECMAScript.Frontend</c>, as a script without top-level <c>await</c>,
/// and the compiler walks the library's tree. Until W2 it parsed with <c>FastParser</c> and walked this engine's
/// <c>AstNode</c> tree, and it ran none of the early errors the engine's compiler checks after the parse
/// (<c>SyntaxValidation</c>); <b>it now reports every early error the library checks at the parse</b>, as a
/// <see cref="FormatException"/> naming the library's diagnostic and its position.
/// </para>
/// <para>
/// <b>What it references</b> (decision D53): the library and <c>Broiler.JavaScript.Portable</c>, and
/// <c>Broiler.JavaScript.Expressions</c> for the one non-tree part that moved there, this engine's Unicode tables, over
/// which the library reads identifiers and Broiler.Regex checks a regular-expression literal, as the engine's own
/// parse does; it references neither <c>Broiler.JavaScript.Ast</c> nor <c>Broiler.JavaScript.Parser</c>. The parse runs
/// on a thread with the stack the library documents (decision D47 (a), <c>CompilationStack</c>), the hop skipped on a
/// compile worker that has it.
/// </para>
/// </remarks>
public sealed class PortableCompiler
{
    private readonly List<PortableInstruction> instructions = [];
    private readonly Dictionary<string, int> parameters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> locals = new(StringComparer.Ordinal);
    private int stackDepth;
    private int maximumStack;

    public PortableProgram Compile(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source is required.", nameof(source));

        instructions.Clear();
        parameters.Clear();
        locals.Clear();
        stackDepth = 0;
        maximumStack = 0;

        var program = Parse(source);
        if (program.Body.Count != 1 || FunctionOf(program.Body[0]) is not { } function)
            throw Unsupported("exactly one top-level function declaration");
        if (function.IsAsync || function.IsGenerator || function.IsArrow || function.ExpressionBody != null)
            throw Unsupported("an ordinary function with a block body");

        foreach (var parameter in function.Parameters)
        {
            if (parameter is not BindingIdentifier identifier)
                throw Unsupported("simple identifier parameters");
            if (!parameters.TryAdd(identifier.Name, parameters.Count))
                throw Unsupported("unique parameter names");
        }

        CompileStatements(function.Body);
        if (instructions.Count == 0 || instructions[^1].OpCode != PortableOpCode.Return)
            throw Unsupported("an explicit return on every compiled path");

        return new PortableProgram(
            function.Name?.Name ?? "anonymous",
            parameters.Count,
            locals.Count,
            Math.Max(1, maximumStack),
            [.. instructions]);
    }

    // A script without top-level await, over the engine's own Unicode tables, on the library's documented stack
    // (decision D47 (a)); the library's first diagnostic is the refusal. The proposals' forms and the lexer's extension
    // are the engine's (the bridge's request, decision D17), so a source the engine parses is parsed here too and a form
    // outside the subset is refused after the parse as unsupported, not as a SyntaxError (found in review of window W2).
    private const ProposalFlags EngineProposals = ProposalFlags.Decorators | ProposalFlags.AutoAccessors
        | ProposalFlags.ImportSourceCalls | ProposalFlags.ImportDeferCalls;

    private static ProgramNode Parse(string source)
    {
        var request = new ParseRequest(new SourceText(source, string.Empty, 0), ParseGoal.Script, JSUnicode.Tables, RegularExpressions.Instance)
        {
            Limits = ParseLimits.Default,
            Proposals = EngineProposals,
            Extensions = LexerExtensions.DecimalSuffix,
        };
        var result = CompilationStack.CurrentWorkerStackBytes >= ParseLimits.StackBytes
            ? SyntaxParser.Parse(request)
            : CompilationStack.RunOnFreshStack(() => SyntaxParser.Parse(request), ParseLimits.StackBytes);
        if (result.Outcome == ParseOutcome.Accepted && result.Tree is { } tree)
            return tree;

        if (result.Diagnostics.Count == 0)
            throw new FormatException("The source is not a script the shared syntax library accepts (" + result.Outcome + ").");

        var diagnostic = result.Diagnostics[0];
        throw new FormatException(string.Create(CultureInfo.InvariantCulture,
            $"SyntaxError at {diagnostic.Span.Line}:{diagnostic.Span.Column}: {diagnostic.MessageId} ({diagnostic.Code})."));
    }

    // The one function the source holds: a declaration, or, as the compiler read before window W2, an expression
    // statement whose expression is a (parenthesised) function expression.
    private static FunctionNode? FunctionOf(Statement statement) => statement switch
    {
        FunctionDeclaration declaration => declaration.Function,
        ExpressionStatement { Expression: var expression } => Unparenthesized(expression) is FunctionExpression function ? function.Function : null,
        _ => null,
    };

    private static Expression Unparenthesized(Expression expression)
    {
        while (expression is ParenthesizedExpression parenthesized)
            expression = parenthesized.Expression;
        return expression;
    }

    public static string EmitCSharp(
        PortableProgram program,
        string namespaceName,
        string typeName)
    {
        ArgumentNullException.ThrowIfNull(program);
        if (string.IsNullOrWhiteSpace(namespaceName) || string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("A namespace and type name are required.");

        var output = new StringBuilder();
        output.AppendLine("// <auto-generated />");
        output.AppendLine("using Broiler.JavaScript.Portable;");
        output.AppendLine();
        output.Append("namespace ").Append(namespaceName).AppendLine(";");
        output.AppendLine();
        output.Append("internal static class ").Append(typeName).AppendLine();
        output.AppendLine("{");
        output.AppendLine("    internal static readonly PortableProgram Value = new(");
        output.Append("        \"").Append(program.Name.Replace("\"", "\\\"", StringComparison.Ordinal)).AppendLine("\",");
        output.Append("        ").Append(program.ParameterCount).AppendLine(",");
        output.Append("        ").Append(program.LocalCount).AppendLine(",");
        output.Append("        ").Append(program.MaximumStack).AppendLine(",");
        output.AppendLine("        new PortableInstruction[]");
        output.AppendLine("        {");
        foreach (var instruction in program.Instructions)
        {
            // A constant is written as its IEEE-754 bits, not as text: .NET's "R" does not read back
            // as the same double for every value (2**-25 renders as the double below it), and no C#
            // literal spells NaN, the infinities or -0.
            var bits = BitConverter.DoubleToUInt64Bits(instruction.Number);
            output.Append("            new(PortableOpCode.").Append(instruction.OpCode);
            if (instruction.Operand != 0 || bits != 0)
                output.Append(", ").Append(instruction.Operand);
            if (bits != 0)
                output.Append(", System.BitConverter.UInt64BitsToDouble(0x").Append(bits.ToString("X16", CultureInfo.InvariantCulture)).Append("UL)");
            output.AppendLine("),");
        }
        output.AppendLine("        });");
        output.AppendLine("}");
        return output.ToString();
    }

    private void CompileStatements(IReadOnlyList<Statement> statements)
    {
        foreach (var statement in statements)
            CompileStatement(statement);
    }

    private void CompileStatement(Statement statement)
    {
        switch (statement)
        {
            case BlockStatement block:
                CompileStatements(block.Body);
                return;
            case VariableDeclaration declaration:
                CompileVariableDeclaration(declaration);
                return;
            case ExpressionStatement expressionStatement:
                CompileExpression(expressionStatement.Expression);
                Emit(PortableOpCode.Pop);
                return;
            case ReturnStatement { Argument: { } argument }:
                CompileExpression(argument);
                Emit(PortableOpCode.Return);
                return;
            case WhileStatement whileStatement:
                CompileWhile(whileStatement);
                return;
            case ForStatement forStatement:
                CompileFor(forStatement);
                return;
            case IfStatement ifStatement:
                CompileIf(ifStatement);
                return;
            default:
                throw Unsupported(statement.Kind.ToString());
        }
    }

    private void CompileVariableDeclaration(VariableDeclaration declaration)
    {
        if (declaration.DeclarationKind != DeclarationKind.Var)
            throw Unsupported("var numeric variables");
        foreach (var declarator in declaration.Declarations)
        {
            if (declarator.Target is not BindingIdentifier identifier)
                throw Unsupported("identifier variable declarations");
            var index = GetOrAddLocal(identifier.Name);
            if (declarator.Initializer == null)
                Emit(PortableOpCode.PushConstant, number: 0);
            else
                CompileExpression(declarator.Initializer);
            Emit(PortableOpCode.StoreLocal, index);
        }
    }

    private void CompileWhile(WhileStatement statement)
    {
        var start = instructions.Count;
        CompileExpression(statement.Test);
        var exit = EmitPlaceholder(PortableOpCode.JumpIfFalse);
        CompileStatement(statement.Body);
        Emit(PortableOpCode.Jump, start);
        Patch(exit, instructions.Count);
    }

    private void CompileFor(ForStatement statement)
    {
        if (statement.Init is Statement initStatement)
            CompileStatement(initStatement);
        else if (statement.Init is Expression initExpression)
        {
            CompileExpression(initExpression);
            Emit(PortableOpCode.Pop);
        }
        else if (statement.Init != null)
            throw Unsupported(statement.Init.Kind.ToString());

        // A for statement without a test was unsupported before window W2 too (its test read as an empty expression).
        var start = instructions.Count;
        if (statement.Test == null)
            throw Unsupported("a for statement with a test");
        CompileExpression(statement.Test);
        var exit = EmitPlaceholder(PortableOpCode.JumpIfFalse);
        CompileStatement(statement.Body);
        if (statement.Update != null)
        {
            CompileExpression(statement.Update);
            Emit(PortableOpCode.Pop);
        }
        Emit(PortableOpCode.Jump, start);
        Patch(exit, instructions.Count);
    }

    private void CompileIf(IfStatement statement)
    {
        CompileExpression(statement.Test);
        var falseJump = EmitPlaceholder(PortableOpCode.JumpIfFalse);
        CompileStatement(statement.Consequent);
        if (statement.Alternate == null)
        {
            Patch(falseJump, instructions.Count);
            return;
        }

        var endJump = EmitPlaceholder(PortableOpCode.Jump);
        Patch(falseJump, instructions.Count);
        CompileStatement(statement.Alternate);
        Patch(endJump, instructions.Count);
    }

    private void CompileExpression(Expression expression)
    {
        switch (expression)
        {
            case ParenthesizedExpression parenthesized:
                CompileExpression(parenthesized.Expression);
                return;
            case NumericLiteral literal when (literal.Flags & (NumericLiteralFlags.BigInt | NumericLiteralFlags.DecimalSuffix)) == 0:
                Emit(PortableOpCode.PushConstant, number: literal.Value);
                return;
            case Identifier identifier:
                EmitLoad(identifier.Name);
                return;
            case UpdateExpression update:
                CompileUpdate(update);
                return;
            case AssignmentExpression assignment:
                CompileAssignment(assignment);
                return;
            case BinaryExpression binary:
                CompileExpression(binary.Left);
                CompileExpression(binary.Right);
                Emit(BinaryOpCode(binary.Operator));
                return;
            default:
                throw Unsupported(expression.Kind.ToString());
        }
    }

    private void CompileAssignment(AssignmentExpression assignment)
    {
        if (AssignedName(assignment.Target) is not { } name || !locals.TryGetValue(name, out var index))
            throw Unsupported("assignments to declared locals");

        if (assignment.Operator == AssignmentOperator.Assign)
        {
            CompileExpression(assignment.Value);
        }
        else
        {
            Emit(PortableOpCode.LoadLocal, index);
            CompileExpression(assignment.Value);
            Emit(assignment.Operator switch
            {
                AssignmentOperator.AddAssign => PortableOpCode.Add,
                AssignmentOperator.SubtractAssign => PortableOpCode.Subtract,
                AssignmentOperator.MultiplyAssign => PortableOpCode.Multiply,
                AssignmentOperator.DivideAssign => PortableOpCode.Divide,
                AssignmentOperator.RemainderAssign => PortableOpCode.Remainder,
                _ => throw Unsupported(assignment.Operator.ToString()),
            });
        }

        Emit(PortableOpCode.Duplicate);
        Emit(PortableOpCode.StoreLocal, index);
    }

    // The name a simple assignment's target names: an identifier, parenthesised or not.
    private static string? AssignedName(SyntaxNode target) => target switch
    {
        Expression expression when Unparenthesized(expression) is Identifier identifier => identifier.Name,
        TargetPattern { Target: var expression } when Unparenthesized(expression) is Identifier identifier => identifier.Name,
        _ => null,
    };

    private void CompileUpdate(UpdateExpression update)
    {
        if (Unparenthesized(update.Operand) is not Identifier identifier || !locals.TryGetValue(identifier.Name, out var index))
            throw Unsupported("updates of declared locals");

        Emit(PortableOpCode.LoadLocal, index);
        if (!update.IsPrefix)
            Emit(PortableOpCode.Duplicate);
        Emit(PortableOpCode.PushConstant, number: 1);
        Emit(update.Operator == UpdateOperator.Increment ? PortableOpCode.Add : PortableOpCode.Subtract);
        if (update.IsPrefix)
            Emit(PortableOpCode.Duplicate);
        Emit(PortableOpCode.StoreLocal, index);
    }

    private void EmitLoad(string name)
    {
        if (locals.TryGetValue(name, out var local))
            Emit(PortableOpCode.LoadLocal, local);
        else if (parameters.TryGetValue(name, out var parameter))
            Emit(PortableOpCode.LoadArgument, parameter);
        else
            throw Unsupported($"unresolved identifier '{name}'");
    }

    private int GetOrAddLocal(string name)
    {
        if (parameters.ContainsKey(name))
            throw Unsupported("locals that do not redeclare parameters");
        if (!locals.TryGetValue(name, out var index))
        {
            index = locals.Count;
            locals.Add(name, index);
        }
        return index;
    }

    private static PortableOpCode BinaryOpCode(BinaryOperator token) => token switch
    {
        BinaryOperator.Add => PortableOpCode.Add,
        BinaryOperator.Subtract => PortableOpCode.Subtract,
        BinaryOperator.Multiply => PortableOpCode.Multiply,
        BinaryOperator.Divide => PortableOpCode.Divide,
        BinaryOperator.Remainder => PortableOpCode.Remainder,
        BinaryOperator.LessThan => PortableOpCode.LessThan,
        BinaryOperator.LessThanOrEqual => PortableOpCode.LessThanOrEqual,
        BinaryOperator.GreaterThan => PortableOpCode.GreaterThan,
        BinaryOperator.GreaterThanOrEqual => PortableOpCode.GreaterThanOrEqual,
        BinaryOperator.Equal or BinaryOperator.StrictEqual => PortableOpCode.Equal,
        BinaryOperator.NotEqual or BinaryOperator.StrictNotEqual => PortableOpCode.NotEqual,
        _ => throw Unsupported(token.ToString()),
    };

    private int EmitPlaceholder(PortableOpCode opCode)
    {
        var index = instructions.Count;
        Emit(opCode);
        return index;
    }

    private void Patch(int index, int target)
        => instructions[index] = instructions[index] with { Operand = target };

    private void Emit(PortableOpCode opCode, int operand = 0, double number = 0)
    {
        instructions.Add(new PortableInstruction(opCode, operand, number));
        stackDepth += StackDelta(opCode);
        if (stackDepth < 0)
            throw new InvalidOperationException("Portable compiler produced an operand-stack underflow.");
        maximumStack = Math.Max(maximumStack, stackDepth);
    }

    private static int StackDelta(PortableOpCode opCode) => opCode switch
    {
        PortableOpCode.PushConstant or PortableOpCode.LoadArgument or PortableOpCode.LoadLocal
            or PortableOpCode.Duplicate => 1,
        PortableOpCode.StoreLocal or PortableOpCode.Pop or PortableOpCode.JumpIfFalse
            or PortableOpCode.Return => -1,
        PortableOpCode.Add or PortableOpCode.Subtract or PortableOpCode.Multiply
            or PortableOpCode.Divide or PortableOpCode.Remainder or PortableOpCode.LessThan
            or PortableOpCode.LessThanOrEqual or PortableOpCode.GreaterThan
            or PortableOpCode.GreaterThanOrEqual or PortableOpCode.Equal or PortableOpCode.NotEqual => -1,
        _ => 0,
    };

    private static NotSupportedException Unsupported(string feature)
        => new($"The portable compiler does not support {feature}.");

    // Broiler.Regex's validate entry over this engine's tables, under the nesting limit the engine compiles under, as
    // the engine's own parse checks a literal (Broiler.VM's JSD-0081, decision D22). A literal the compiler cannot run is
    // refused after the parse as unsupported; one Broiler.Regex refuses is an early error the parse reports.
    private sealed class RegularExpressions : IRegularExpressionValidator
    {
        internal static RegularExpressions Instance { get; } = new();

        public RegularExpressionVerdict Validate(string pattern, string flags)
            => Broiler.Regex.BroilerRegex.Validate(pattern, flags, JSUnicode.Tables, Broiler.Regex.BroilerRegex.MaximumNestingLimit,
                out _, out _) == Broiler.Regex.RegexStatus.Success
                ? RegularExpressionVerdict.Valid
                : RegularExpressionVerdict.Invalid("regexp.literal.invalid", "/" + pattern + "/" + flags, 0, 0, inFlags: false);
    }
}
