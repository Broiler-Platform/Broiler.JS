using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Broiler.JavaScript.JSClassGenerator;

internal static class RegistrationGenerator
{
    internal static string GenerateNames(
        RegistrationTargetModel target,
        ImmutableArray<RegistrationTypeModel> types)
    {
        var names = types.SelectMany(static type => type.Names)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
        var existingFields = new HashSet<string>(target.ExistingFields, StringComparer.Ordinal);

        var sb = new StringBuilder()
            .AppendLine("using Broiler.JavaScript.Runtime;")
            .AppendLine("using Broiler.JavaScript.Storage;")
            .AppendLine("using Broiler.JavaScript.Engine.Core;")
            .AppendLine($"namespace {target.Namespace} {{")
            .AppendLine($"partial class {target.ClassName} {{");

        foreach (var name in names)
        {
            var memberName = name.ToUnescapedCSharpIdentifier();
            if (!existingFields.Contains(memberName))
                sb.AppendLine($"public static readonly KeyString {name};");
        }

        sb.AppendLine($"static {target.ClassName}() {{");
        foreach (var name in names)
            sb.AppendLine($"{target.ClassName}.{name} = \"{name.ToUnescapedCSharpIdentifier()}\";");
        sb.AppendLine("}");
        sb.AppendLine("internal static readonly BuiltInRegistrationDescriptor[] GeneratedRegistrationDescriptors = new BuiltInRegistrationDescriptor[] {");
        foreach (var type in types.OrderBy(static type => type.QualifiedClrName, StringComparer.Ordinal))
        {
            sb.AppendLine(
                $"new BuiltInRegistrationDescriptor(BuiltInFeatureId.{type.FeatureName}, \"{type.QualifiedClrName}\", {(type.Register ? "true" : "false")}),");
        }
        sb.AppendLine("};");
        sb.AppendLine("static private void RegisterAll(JSObject context, BuiltInFeatures features = BuiltInFeatures.All) {");

        foreach (var type in RegistrationOrder(types))
            if (type.Register)
                sb.AppendLine($"if ((features & BuiltInFeatures.{type.FeatureName}) != 0) {type.QualifiedClrName}.CreateClass(context);");

        return sb.AppendLine("}")
            .AppendLine("}")
            .AppendLine("}")
            .ToString();
    }

    private static IEnumerable<RegistrationTypeModel> RegistrationOrder(
        ImmutableArray<RegistrationTypeModel> allTypes)
    {
        var remaining = allTypes.OrderBy(static type => type.QualifiedClrName, StringComparer.Ordinal).ToList();
        var availableNames = new HashSet<string>(remaining.Select(static type => type.ClrClassName), StringComparer.Ordinal);
        var emitted = new HashSet<string>(StringComparer.Ordinal);

        while (remaining.Count > 0)
        {
            var progressed = false;
            for (var i = 0; i < remaining.Count;)
            {
                var type = remaining[i];
                if (string.IsNullOrEmpty(type.BaseClrClassName)
                    || !availableNames.Contains(type.BaseClrClassName)
                    || emitted.Contains(type.BaseClrClassName))
                {
                    remaining.RemoveAt(i);
                    emitted.Add(type.ClrClassName);
                    progressed = true;
                    yield return type;
                    continue;
                }
                i++;
            }

            // Defensive deterministic fallback for malformed/cyclic metadata.
            if (!progressed)
            {
                foreach (var type in remaining)
                    yield return type;
                yield break;
            }
        }
    }
}
