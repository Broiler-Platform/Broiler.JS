using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Broiler.JavaScript.JSClassGenerator;

[Generator]
public sealed class JSClassGenerator : IIncrementalGenerator
{
    private const string AttributeNamespace = "Broiler.JavaScript.ExpressionCompiler.";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classModels = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeNamespace + "JSClassGeneratorAttribute",
                static (_, _) => true,
                static (attributeContext, _) => CreateTypeModel((ITypeSymbol)attributeContext.TargetSymbol))
            .Where(static model => model != null)!;

        var functionModels = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeNamespace + "JSFunctionGeneratorAttribute",
                static (_, _) => true,
                static (attributeContext, _) => CreateTypeModel((ITypeSymbol)attributeContext.TargetSymbol))
            .Where(static model => model != null)!;

        context.RegisterSourceOutput(classModels, static (productionContext, model) =>
            productionContext.AddSource(model!.HintName, model.Code));
        context.RegisterSourceOutput(functionModels, static (productionContext, model) =>
            productionContext.AddSource(model!.HintName, model.Code));

        // Project the per-type source model to compact registration metadata before
        // Collect. A method-body-only change can regenerate its own source without
        // invalidating the aggregate Names/registration output.
        var classRegistrationModels = classModels
            .Select(static (model, _) => model!.Registration);
        var functionRegistrationModels = functionModels
            .Select(static (model, _) => model!.Registration);
        var allModels = classRegistrationModels.Collect()
            .Combine(functionRegistrationModels.Collect())
            .Select(static (pair, _) => Combine(pair.Left, pair.Right));

        var registrationTargets = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeNamespace + "JSRegistrationGeneratorAttribute",
                static (_, _) => true,
                static (attributeContext, _) => CreateRegistrationTarget((ITypeSymbol)attributeContext.TargetSymbol));

        context.RegisterSourceOutput(
            registrationTargets.Combine(allModels),
            static (productionContext, input) =>
            {
                productionContext.AddSource(
                    input.Left.HintName,
                    RegistrationGenerator.GenerateNames(input.Left, input.Right));
            });
    }

    private static GeneratedTypeModel? CreateTypeModel(ITypeSymbol symbol)
    {
        var attribute = symbol.GetAttribute();
        if (attribute == null)
            return null;

        var generatorContext = new JSGeneratorContext([(symbol, attribute)]);
        var type = generatorContext.AssemblyTypes[0];
        var code = ClassGenerator.GenerateClass(type, generatorContext);
        var ns = symbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : symbol.ContainingNamespace.ToDisplayString();

        var registration = new RegistrationTypeModel(
            ns,
            type.ClrClassName,
            type.BaseClrClassName ?? string.Empty,
            FeatureName(ns),
            type.Register,
            generatorContext.Names.Distinct().OrderBy(static name => name).ToImmutableArray());
        return new GeneratedTypeModel(
            string.IsNullOrEmpty(ns) ? $"{symbol.Name}.g.cs" : $"{ns}.{symbol.Name}.g.cs",
            code,
            registration);
    }

    private static string FeatureName(string @namespace)
    {
        if (@namespace.Contains(".Temporal"))
            return "Temporal";
        if (@namespace.Contains(".Intl"))
            return "Intl";
        return "Core";
    }

    private static RegistrationTargetModel CreateRegistrationTarget(ITypeSymbol symbol)
    {
        var ns = symbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : symbol.ContainingNamespace.ToDisplayString();
        var fields = symbol.GetMembers()
            .Where(static member => member.Kind == SymbolKind.Field)
            .Select(static member => member.Name)
            .OrderBy(static name => name)
            .ToImmutableArray();
        return new RegistrationTargetModel(
            string.IsNullOrEmpty(ns) ? $"{symbol.Name}.g.cs" : $"{ns}.{symbol.Name}.g.cs",
            ns,
            symbol.Name,
            fields);
    }

    private static ImmutableArray<RegistrationTypeModel> Combine(
        ImmutableArray<RegistrationTypeModel> classes,
        ImmutableArray<RegistrationTypeModel> functions)
    {
        var builder = ImmutableArray.CreateBuilder<RegistrationTypeModel>(classes.Length + functions.Length);
        foreach (var model in classes)
            builder.Add(model);
        foreach (var model in functions)
            builder.Add(model);

        builder.Sort(static (left, right) =>
            string.CompareOrdinal(left.QualifiedClrName, right.QualifiedClrName));
        return builder.ToImmutable();
    }
}
