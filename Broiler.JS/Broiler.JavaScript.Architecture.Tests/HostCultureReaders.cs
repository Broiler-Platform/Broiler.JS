using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Broiler.JavaScript.Architecture.Tests;

/// <summary>The three kinds of host-culture reader no build rule sees (the owner's ruling of 2026-11-12, "Follow-up arch test").</summary>
internal enum ReaderKind
{
    /// <summary>A numeric or date <c>Parse</c> or <c>TryParse</c> that names no format provider, or names a null one.</summary>
    Parse,

    /// <summary>A .NET <c>Regex</c> that folds case (<c>RegexOptions.IgnoreCase</c>, or <c>(?i)</c> in its pattern) without <c>RegexOptions.CultureInvariant</c>.</summary>
    Regex,

    /// <summary>An interpolated string, <c>string.Format</c> or <c>AppendFormat</c> that makes a number, a date or another formattable value text with no provider, culture or invariant format.</summary>
    Format,
}

/// <summary>One reader: where it is, what it is, and whether a kept path's <c>// Kept:</c> suppression holds it.</summary>
internal sealed record ReaderFinding(string Path, int Line, ReaderKind Kind, string Detail, bool Kept)
{
    internal string Label => Kind switch
    {
        ReaderKind.Parse => "parse with no format provider",
        ReaderKind.Regex => "Regex folding case without CultureInvariant",
        _ => "value made text with no provider",
    };

    public override string ToString() => Path + ":" + Line.ToString(CultureInfo.InvariantCulture) + ": " + Label + ": " + Detail;
}

/// <summary>
/// Finds, by Roslyn's semantic model over a compilation of a project's own sources, the three kinds of host-culture reader
/// that the globalization rules (CA1304, CA1305, CA1309, CA1310, CA1311) and the banned-API rule (RS0030) do not see.
/// </summary>
/// <remarks>
/// A finding inside a kept path is marked kept, not dropped: a kept path is the region a
/// <c>#pragma warning disable RS0030 // Kept: …</c> opens and the next <c>#pragma warning restore RS0030</c> closes, the
/// form the kept paths of the ruling of 2026-11-11 use, so that the list of kept paths stays the RS0030 suppressions' list.
/// </remarks>
internal static class HostCultureReaders
{
    private const string FormatProvider = "System.IFormatProvider";

    private const string RegexType = "System.Text.RegularExpressions.Regex";

    private const string RegexOptionsType = "System.Text.RegularExpressions.RegexOptions";

    private const int IgnoreCase = 1;

    private const int CultureInvariant = 512;

    private static readonly HashSet<string> ParseNames = new(StringComparer.Ordinal) { "Parse", "TryParse", "ParseExact", "TryParseExact" };

    private static readonly HashSet<string> RegexStatics = new(StringComparer.Ordinal)
    {
        "IsMatch", "Match", "Matches", "Replace", "Split", "Count", "EnumerateMatches", "EnumerateSplits",
    };

    /// <summary>Types whose parse reads a culture's number or date format, besides the numeric and date special types.</summary>
    private static readonly HashSet<string> ParsedByCulture = new(StringComparer.Ordinal)
    {
        "System.Int128", "System.UInt128", "System.Half", "System.Numerics.BigInteger", "System.Numerics.Complex",
        "System.DateTimeOffset", "System.TimeSpan", "System.DateOnly", "System.TimeOnly",
    };

    /// <summary>Formattable types whose text no culture changes, whatever the format.</summary>
    private static readonly HashSet<string> FormattedInvariantly = new(StringComparer.Ordinal)
    {
        "System.Guid", "System.Version", "System.Text.Rune", "char", "bool",
    };

    private static readonly System.Text.RegularExpressions.Regex ItemShape = new(@"\G\{(\d+)\s*(,\s*-?\d+\s*)?(:([^}]*))?\}", RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex InlineIgnoreCase = new(@"\(\?[imnsx]*i[imnsx]*(-[imnsx]*)?[:)]", RegexOptions.CultureInvariant);


    /// <summary>The names of the methods an invocation must call to be one of the readers; any other invocation is not bound.</summary>
    private static readonly HashSet<string> InvokedNames = new([.. ParseNames, .. RegexStatics, "Format", "AppendFormat"], StringComparer.Ordinal);

    /// <summary>Every reader in <paramref name="trees"/>, each named by <paramref name="pathOf"/> and its line.</summary>
    internal static List<ReaderFinding> Read(Compilation compilation, IEnumerable<SyntaxTree> trees, Func<SyntaxTree, string> pathOf) =>
        Read(compilation, trees, pathOf, out _);

    /// <summary>
    /// Every reader in <paramref name="trees"/>, and the errors of each tree the reading binds: its syntax, declarations
    /// and bodies, by the same semantic model the reading binds it with, so that every finding, and every place none is
    /// found, rests on code that binds.
    /// </summary>
    /// <remarks>
    /// An invocation or a method group is bound only when the name it calls or names is one a reader has (a method's name
    /// cannot be aliased); object creations, attributes and interpolated strings, whose type may be named by an alias or
    /// not at all, are always bound. A tree with none of these, such as a generated table of constant data, is not bound,
    /// and its bodies' errors are not read.
    /// </remarks>
    internal static List<ReaderFinding> Read(Compilation compilation, IEnumerable<SyntaxTree> trees, Func<SyntaxTree, string> pathOf, out List<string> errors)
    {
        var read = trees.AsParallel().AsOrdered().WithDegreeOfParallelism(Math.Max(1, Math.Min(4, Environment.ProcessorCount)))
            .Select(tree => ReadTree(compilation, tree, pathOf))
            .ToList();
        errors = [.. read.SelectMany(static r => r.Errors)];
        return [.. read.SelectMany(static r => r.Findings)];
    }

    private static string? CalledName(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
        MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
        SimpleNameSyntax name => name.Identifier.ValueText,
        _ => null,
    };

    private static (List<ReaderFinding> Findings, List<string> Errors) ReadTree(Compilation compilation, SyntaxTree tree, Func<SyntaxTree, string> pathOf)
    {
        var findings = new List<ReaderFinding>();
        var model = compilation.GetSemanticModel(tree);
        var bound = false;
        IOperation? Bind(SyntaxNode node)
        {
            bound = true;
            return model.GetOperation(node);
        }

        var root = tree.GetRoot();
        var kept = KeptRegions(root);
        var path = pathOf(tree);
        void Add(SyntaxNode at, ReaderKind kind, string detail)
        {
            var line = at.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            findings.Add(new ReaderFinding(path, line, kind, detail, kept.Any(r => line > r.Start && line < r.End)));
        }

        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case InvocationExpressionSyntax call:
                    if ((CalledName(call.Expression) is not { } called || InvokedNames.Contains(called))
                        && Bind(node) is IInvocationOperation invocation)
                    {
                        ReadParse(invocation, Add);
                        ReadRegex(compilation, invocation.TargetMethod, invocation.Arguments, invocation, Add);
                        ReadComposite(invocation, Add);
                    }

                    break;
                case BaseObjectCreationExpressionSyntax:
                    if (Bind(node) is IObjectCreationOperation creation && creation.Constructor is { } constructor)
                        ReadRegex(compilation, constructor, creation.Arguments, creation, Add);
                    break;
                case AttributeSyntax:
                    if (Bind(node) is IAttributeOperation { Operation: IObjectCreationOperation attribute } && attribute.Constructor is { } attributeConstructor
                        && attributeConstructor.ContainingType.ToDisplayString() == "System.Text.RegularExpressions.GeneratedRegexAttribute")
                        ReadRegex(compilation, attributeConstructor, attribute.Arguments, attribute, Add);
                    break;
                case InterpolatedStringExpressionSyntax:
                    if (Bind(node) is IInterpolatedStringOperation interpolated)
                        ReadInterpolated(interpolated, Add);
                    break;
                case IdentifierNameSyntax or MemberAccessExpressionSyntax or GenericNameSyntax:
                    // A method group, such as Select(int.Parse) or Func<string, double> f = double.Parse: a delegate
                    // creation, which no invocation shows. The syntax of the reference is the group itself, so a
                    // name inside a member access is not read twice, and the callee of an invocation is not one.
                    if (CalledName((ExpressionSyntax)node) is { } grouped && ParseNames.Contains(grouped)
                        && !(node.Parent is InvocationExpressionSyntax { Expression: var callee } && callee == node)
                        && MethodReference(Bind(node)) is { } reference && reference.Syntax == node)
                        ReadParseGroup(reference, Add);
                    break;
            }
        }

        var errors = bound
            ? model.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()).ToList()
            : [];
        return (findings, errors);
    }

    // ---------------------------------------------------------------------------------------------
    // Parse and TryParse.

    private static void ReadParse(IInvocationOperation invocation, Action<SyntaxNode, ReaderKind, string> add)
    {
        var method = invocation.TargetMethod;
        if (!ParseNames.Contains(method.Name) || !IsParsedByCulture(method.ContainingType))
            return;

        var shown = method.ContainingType.ToDisplayString() + "." + method.Name;
        var provider = invocation.Arguments.FirstOrDefault(static a => a.Parameter is { } p && IsFormatProvider(p.Type));
        if (provider is null)
            add(invocation.Syntax, ReaderKind.Parse, shown + " names no format provider: " + Shown(invocation.Syntax));
        else if (IsNull(provider.Value))
            add(invocation.Syntax, ReaderKind.Parse, shown + " passes a null format provider: " + Shown(invocation.Syntax));
    }

    /// <summary>A parse named as a method group: refused when none of its parameters is a format provider, as nothing can give it one.</summary>
    private static void ReadParseGroup(IMethodReferenceOperation reference, Action<SyntaxNode, ReaderKind, string> add)
    {
        var method = reference.Method;
        if (!ParseNames.Contains(method.Name) || !IsParsedByCulture(method.ContainingType) || method.Parameters.Any(static p => IsFormatProvider(p.Type)))
            return;
        add(reference.Syntax, ReaderKind.Parse, method.ContainingType.ToDisplayString() + "." + method.Name
            + " as a method group names no format provider: " + Shown(reference.Syntax.FirstAncestorOrSelf<StatementSyntax>() ?? reference.Syntax.Parent ?? reference.Syntax));
    }

    private static IMethodReferenceOperation? MethodReference(IOperation? operation)
    {
        while (true)
        {
            switch (operation)
            {
                case IMethodReferenceOperation reference:
                    return reference;
                case IDelegateCreationOperation creation:
                    operation = creation.Target;
                    break;
                case IConversionOperation conversion:
                    operation = conversion.Operand;
                    break;
                default:
                    return null;
            }
        }
    }

    private static bool IsParsedByCulture(INamedTypeSymbol? type)
    {
        if (type is null)
            return false;
        switch (type.SpecialType)
        {
            case SpecialType.System_SByte: case SpecialType.System_Byte: case SpecialType.System_Int16: case SpecialType.System_UInt16:
            case SpecialType.System_Int32: case SpecialType.System_UInt32: case SpecialType.System_Int64: case SpecialType.System_UInt64:
            case SpecialType.System_IntPtr: case SpecialType.System_UIntPtr: case SpecialType.System_Single: case SpecialType.System_Double:
            case SpecialType.System_Decimal: case SpecialType.System_DateTime:
                return true;
        }

        var name = type.OriginalDefinition.ToDisplayString();
        return ParsedByCulture.Contains(name)
            || name.StartsWith("System.IParsable<", StringComparison.Ordinal)
            || name.StartsWith("System.ISpanParsable<", StringComparison.Ordinal)
            || name.StartsWith("System.IUtf8SpanParsable<", StringComparison.Ordinal)
            || name.StartsWith("System.Numerics.INumberBase<", StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // Regex.

    private static void ReadRegex(Compilation compilation, IMethodSymbol method, IEnumerable<IArgumentOperation> arguments, IOperation at, Action<SyntaxNode, ReaderKind, string> add)
    {
        var type = method.ContainingType?.ToDisplayString();
        var isConstructor = method.MethodKind == MethodKind.Constructor;
        if (type == RegexType)
        {
            if (!isConstructor && !(method.IsStatic && RegexStatics.Contains(method.Name)))
                return;
        }
        else if (type != "System.Text.RegularExpressions.GeneratedRegexAttribute")
        {
            return;
        }

        var all = arguments.ToList();
        var pattern = all.FirstOrDefault(static a => a.Parameter?.Name == "pattern");
        if (pattern is null)
            return;

        var options = all.FirstOrDefault(static a => a.Parameter?.Type.ToDisplayString() == RegexOptionsType);
        var culture = all.FirstOrDefault(static a => a.Parameter?.Name == "cultureName");
        if (culture is not null && culture.Value.ConstantValue is { HasValue: true, Value: string { Length: > 0 } })
            return;

        var (known, value, invariant) = options is null || options.ArgumentKind == ArgumentKind.DefaultValue
            ? (true, 0, false)
            : Options(compilation, options.Value, 0);
        if (invariant || (known && (value & CultureInvariant) != 0))
            return;

        var inline = pattern.Value.ConstantValue is { HasValue: true, Value: string text } && InlineIgnoreCase.IsMatch(text);
        if (known && (value & IgnoreCase) != 0)
            add(at.Syntax, ReaderKind.Regex, "RegexOptions.IgnoreCase without RegexOptions.CultureInvariant: " + Shown(at.Syntax));
        else if (!known)
            add(at.Syntax, ReaderKind.Regex, "options not known when compiled and not combined with RegexOptions.CultureInvariant: " + Shown(at.Syntax));
        else if (inline)
            add(at.Syntax, ReaderKind.Regex, "the pattern turns IgnoreCase on, (?i), without RegexOptions.CultureInvariant: " + Shown(at.Syntax));
    }

    /// <summary>
    /// The options an expression gives: whether its value is known when compiled, the value, and whether an or of it names
    /// CultureInvariant. A static readonly field of the compilation's own sources gives its initializer's options, when its
    /// type declares no static constructor that could assign it again.
    /// </summary>
    private static (bool Known, int Value, bool Invariant) Options(Compilation compilation, IOperation operation, int depth)
    {
        operation = Unwrap(operation);
        if (depth < 8 && operation is IFieldReferenceOperation { Field: { IsStatic: true, IsReadOnly: true } field }
            && !field.ContainingType.StaticConstructors.Any(static c => !c.IsImplicitlyDeclared)
            && field.DeclaringSyntaxReferences is [var declaration]
            && declaration.GetSyntax() is VariableDeclaratorSyntax { Initializer.Value: { } initializer }
            && compilation.ContainsSyntaxTree(initializer.SyntaxTree)
            && compilation.GetSemanticModel(initializer.SyntaxTree).GetOperation(initializer) is { } initial)
            return Options(compilation, initial, depth + 1);

        if (operation.ConstantValue is { HasValue: true, Value: { } constant })
        {
            var value = Convert.ToInt32(constant, CultureInfo.InvariantCulture);
            return (true, value, (value & CultureInvariant) != 0);
        }

        if (operation is IBinaryOperation { OperatorKind: BinaryOperatorKind.Or } or)
        {
            var left = Options(compilation, or.LeftOperand, depth);
            var right = Options(compilation, or.RightOperand, depth);
            return (left.Known && right.Known, left.Value | right.Value, left.Invariant || right.Invariant);
        }

        return (false, 0, false);
    }

    // ---------------------------------------------------------------------------------------------
    // Interpolation and composite formatting.

    private static void ReadInterpolated(IInterpolatedStringOperation interpolated, Action<SyntaxNode, ReaderKind, string> add)
    {
        // $"..." + $"..." given to a handler is one handler's content: the handler is above the additions.
        var parent = interpolated.Parent;
        while (parent is IInterpolatedStringAdditionOperation addition)
            parent = addition.Parent;
        if (parent is IInterpolatedStringHandlerCreationOperation handler)
        {
            if (HandlerNamesProvider(handler))
                return;
        }
        else if (parent is IConversionOperation { Type: { } target } conversion
            && target.ToDisplayString() is "System.FormattableString" or "System.IFormattable")
        {
            if (conversion.Parent is IArgumentOperation { Parent: IInvocationOperation { TargetMethod: var consumer } }
                && consumer.ContainingType?.ToDisplayString() == "System.FormattableString" && consumer.Name == "Invariant")
                return;
        }

        foreach (var part in interpolated.Parts)
        {
            IOperation? value = null;
            string? format = null;
            switch (part)
            {
                case IInterpolationOperation hole:
                    value = hole.Expression;
                    format = hole.FormatString?.ConstantValue.Value as string;
                    break;
                case IInterpolatedStringAppendOperation { AppendCall: IInvocationOperation append } when part.Kind == OperationKind.InterpolatedStringAppendFormatted:
                    value = append.Arguments.FirstOrDefault(static a => a.Parameter?.Ordinal == 0)?.Value;
                    format = append.Arguments.FirstOrDefault(static a => a.Parameter?.Name == "format")?.Value.ConstantValue.Value as string;
                    break;
            }

            if (value is not null && FormatsByCulture(value, format) is { } type)
                add(value.Syntax, ReaderKind.Format, "the interpolation hole {" + Shown(value.Syntax) + (format is null ? "" : ":" + format) + "} of type " + type + ": " + Shown(interpolated.Syntax));
        }
    }

    /// <summary>Whether a handler's constructor takes a format provider and is given one that is not null.</summary>
    private static bool HandlerNamesProvider(IInterpolatedStringHandlerCreationOperation handler)
    {
        if (handler.HandlerCreation is not IObjectCreationOperation { Constructor: { } constructor } creation
            || !constructor.Parameters.Any(static p => IsFormatProvider(p.Type)))
            return false;

        var given = creation.Arguments.First(static a => a.Parameter is { } p && IsFormatProvider(p.Type));
        if (given.Value is IInterpolatedStringHandlerArgumentPlaceholderOperation { ArgumentIndex: >= 0 } placeholder)
        {
            var call = handler.Parent is IArgumentOperation { Parent: { } outer } ? outer : null;
            var outerArguments = call switch
            {
                IInvocationOperation invocation => invocation.Arguments,
                IObjectCreationOperation construction => construction.Arguments,
                _ => default,
            };
            if (outerArguments.IsDefault)
                return false;
            var provider = outerArguments.FirstOrDefault(a => a.Parameter?.Ordinal == placeholder.ArgumentIndex);
            return provider is not null && !IsNull(provider.Value);
        }

        return !IsNull(given.Value);
    }

    private static void ReadComposite(IInvocationOperation invocation, Action<SyntaxNode, ReaderKind, string> add)
    {
        var method = invocation.TargetMethod;
        var type = method.ContainingType?.ToDisplayString();
        if (!((type == "string" && method.Name == "Format") || (type == "System.Text.StringBuilder" && method.Name == "AppendFormat")))
            return;

        var provider = invocation.Arguments.FirstOrDefault(static a => a.Parameter is { } p && IsFormatProvider(p.Type));
        if (provider is not null && !IsNull(provider.Value))
            return;

        var formatArgument = invocation.Arguments.FirstOrDefault(static a => a.Parameter?.Name == "format");
        if (formatArgument is null)
            return;

        var formats = new Dictionary<int, List<string?>>();
        var composite = formatArgument.Value.ConstantValue.Value as string;
        if (composite is not null)
        {
            foreach (var (index, format) in CompositeItems(composite))
            {
                if (!formats.TryGetValue(index, out var list))
                    formats[index] = list = [];
                list.Add(format);
            }
        }

        var values = new List<IOperation>();
        foreach (var argument in invocation.Arguments.Where(a => a.Parameter is { } p && p.Ordinal > formatArgument.Parameter!.Ordinal).OrderBy(static a => a.Parameter!.Ordinal))
        {
            switch (Unwrap(argument.Value))
            {
                case IArrayCreationOperation { Initializer: { } initializer } when argument.ArgumentKind != ArgumentKind.Explicit || argument.Parameter!.IsParams:
                    values.AddRange(initializer.ElementValues);
                    break;
                case ICollectionExpressionOperation collection:
                    values.AddRange(collection.Elements);
                    break;
                default:
                    values.Add(argument.Value);
                    break;
            }
        }

        for (var i = 0; i < values.Count; i++)
        {
            IEnumerable<string?> used = composite is null ? [null] : formats.TryGetValue(i, out var list) ? list : [];
            foreach (var format in used)
            {
                if (FormatsByCulture(values[i], format) is { } shownType)
                {
                    add(values[i].Syntax, ReaderKind.Format, method.ContainingType!.Name + "." + method.Name + "'s argument {" + i.ToString(CultureInfo.InvariantCulture)
                        + (format is null ? "" : ":" + format) + "} of type " + shownType + (provider is null ? " with no provider" : " with a null provider") + ": " + Shown(invocation.Syntax));
                    break;
                }
            }
        }
    }

    /// <summary>
    /// The format items of a composite format, each with its index and its format string, if any: <c>{{</c> and <c>}}</c>
    /// are escaped braces, not items.
    /// </summary>
    internal static List<(int Index, string? Format)> CompositeItems(string composite)
    {
        var items = new List<(int Index, string? Format)>();
        for (var i = 0; i < composite.Length; i++)
        {
            var c = composite[i];
            if ((c == '{' || c == '}') && i + 1 < composite.Length && composite[i + 1] == c)
            {
                i++;
                continue;
            }

            if (c != '{')
                continue;
            var end = composite.IndexOf('}', i + 1);
            if (end < 0)
                break;
            var item = ItemShape.Match(composite, i, end - i + 1);
            if (item.Success)
                items.Add((int.Parse(item.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture), item.Groups[4].Success ? item.Groups[4].Value : null));
            i = end;
        }

        return items;
    }

    /// <summary>The type shown when <paramref name="value"/> becomes text by the current culture with <paramref name="format"/>; null when no culture changes it.</summary>
    private static string? FormatsByCulture(IOperation value, string? format)
    {
        var operand = Unwrap(value);
        var type = operand.Type;
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            type = nullable.TypeArguments[0];
        if (type is null || type.TypeKind == TypeKind.Enum)
            return null;

        if (type is ITypeParameterSymbol parameter)
            return parameter.ConstraintTypes.Any(IsFormattable) ? type.ToDisplayString() : null;
        if (!IsFormattable(type) || FormattedInvariantly.Contains(type.OriginalDefinition.ToDisplayString()))
            return null;

        // A standard format is a letter and an optional precision; any other format is a custom one, which reads the
        // culture's signs, separators or calendar.
        var specifier = string.IsNullOrWhiteSpace(format) ? "" : format.Trim();
        var letter = specifier.Length > 0 && char.IsAsciiLetter(specifier[0]) && specifier.Skip(1).All(char.IsAsciiDigit) ? specifier[0] : '\0';
        var integral = IsIntegral(type, out var unsigned);
        if (integral)
        {
            if (letter is 'X' or 'x' or 'B' or 'b')
                return null;
            if (unsigned && (specifier.Length == 0 || letter is 'G' or 'g' or 'D' or 'd'))
                return null;
            if (specifier.Length == 0 && operand.ConstantValue is { HasValue: true, Value: { } constant }
                && Convert.ToDecimal(constant, CultureInfo.InvariantCulture) >= 0)
                return null;
        }

        var name = type.ToDisplayString();
        if (type.SpecialType == SpecialType.System_DateTime || name == "System.DateTimeOffset")
            return letter is 'O' or 'o' or 'R' or 'r' or 's' or 'u' ? null : name;
        if (name is "System.DateOnly" or "System.TimeOnly")
            return letter is 'O' or 'o' or 'R' or 'r' ? null : name;
        if (name == "System.TimeSpan")
            return specifier.Length == 0 || letter is 'c' or 't' or 'T' ? null : name;
        return name;
    }

    private static bool IsIntegral(ITypeSymbol type, out bool unsigned)
    {
        unsigned = type.SpecialType is SpecialType.System_Byte or SpecialType.System_UInt16 or SpecialType.System_UInt32
            or SpecialType.System_UInt64 or SpecialType.System_UIntPtr || type.ToDisplayString() == "System.UInt128";
        return unsigned || type.SpecialType is SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_Int32
            or SpecialType.System_Int64 or SpecialType.System_IntPtr || type.ToDisplayString() is "System.Int128" or "System.Numerics.BigInteger";
    }

    private static bool IsFormattable(ITypeSymbol type) =>
        type.ToDisplayString() == "System.IFormattable" || type.AllInterfaces.Any(static i => i.ToDisplayString() == "System.IFormattable");

    // ---------------------------------------------------------------------------------------------
    // Kept paths and helpers.

    /// <summary>The kept paths of a source: from each <c>#pragma warning disable RS0030 // Kept: …</c> line to the next restore of RS0030.</summary>
    internal static List<(int Start, int End)> KeptRegions(SyntaxNode root)
    {
        var regions = new List<(int Start, int End)>();
        int? open = null;
        foreach (var directive in Directives(root).OfType<PragmaWarningDirectiveTriviaSyntax>())
        {
            if (!directive.ErrorCodes.Any(static c => c.ToString().Trim() == "RS0030"))
                continue;
            var line = directive.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            if (directive.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword))
            {
                var text = directive.ToFullString();
                var at = text.IndexOf("//", StringComparison.Ordinal);
                var comment = at < 0 ? "" : text[at..].Trim();
                if (open is null && comment.StartsWith("// Kept: ", StringComparison.Ordinal) && comment.Length > "// Kept: ".Length)
                    open = line;
            }
            else if (open is { } start)
            {
                regions.Add((start, line));
                open = null;
            }
        }

        if (open is { } unclosed)
            regions.Add((unclosed, int.MaxValue));
        return regions;
    }

    /// <summary>The line of every <c>#pragma warning disable</c> of RS0030 in a source, kept path or not.</summary>
    internal static IEnumerable<int> Rs0030Disables(SyntaxNode root) =>
        Directives(root).OfType<PragmaWarningDirectiveTriviaSyntax>()
            .Where(static d => d.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword) && d.ErrorCodes.Any(static c => c.ToString().Trim() == "RS0030"))
            .Select(static d => d.GetLocation().GetLineSpan().StartLinePosition.Line + 1);

    /// <summary>
    /// Every other way a source silences RS0030, which no kept path may use: a <c>#pragma warning disable</c> that names no
    /// code, and so disables every rule, and a <c>SuppressMessage</c> or <c>UnconditionalSuppressMessage</c> attribute that
    /// names RS0030.
    /// </summary>
    internal static IEnumerable<(int Line, string Form)> OtherRs0030Suppressions(SyntaxNode root)
    {
        foreach (var directive in Directives(root).OfType<PragmaWarningDirectiveTriviaSyntax>())
        {
            if (directive.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword) && directive.ErrorCodes.Count == 0)
                yield return (directive.GetLocation().GetLineSpan().StartLinePosition.Line + 1, "#pragma warning disable names no code, so it disables RS0030 too");
        }

        if (!root.SyntaxTree.GetText().ToString().Contains("SuppressMessage", StringComparison.Ordinal))
            yield break;
        foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
        {
            var name = attribute.Name switch
            {
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
                SimpleNameSyntax simple => simple.Identifier.ValueText,
                _ => attribute.Name.ToString(),
            };
            if (name is "SuppressMessage" or "SuppressMessageAttribute" or "UnconditionalSuppressMessage" or "UnconditionalSuppressMessageAttribute"
                && attribute.ArgumentList?.Arguments.Any(static a => a.Expression.ToString().Contains("RS0030", StringComparison.Ordinal)) == true)
                yield return (attribute.GetLocation().GetLineSpan().StartLinePosition.Line + 1, "[" + Shown(attribute) + "] suppresses RS0030");
        }
    }

    /// <summary>The directives of a source, found without walking the nodes that hold none.</summary>
    internal static IEnumerable<DirectiveTriviaSyntax> Directives(SyntaxNode root) =>
        root.DescendantTrivia(static n => n.ContainsDirectives).Where(static t => t.IsDirective).Select(static t => t.GetStructure()).OfType<DirectiveTriviaSyntax>();

    private static bool IsFormatProvider(ITypeSymbol type) =>
        type.ToDisplayString().TrimEnd('?') == FormatProvider || type.AllInterfaces.Any(static i => i.ToDisplayString() == FormatProvider);

    private static bool IsNull(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
            operation = conversion.Operand;
        return operation.ConstantValue is { HasValue: true, Value: null } || operation is IDefaultValueOperation;
    }

    private static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
            operation = conversion.Operand;
        return operation;
    }

    private static string Shown(SyntaxNode node)
    {
        var text = string.Join(' ', node.ToString().Split((char[])['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(static l => l.Trim()));
        return text.Length <= 160 ? text : text[..157] + "...";
    }
}
