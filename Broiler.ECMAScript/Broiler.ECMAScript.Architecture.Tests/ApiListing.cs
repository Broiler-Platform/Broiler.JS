using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Broiler.ECMAScript.Architecture.Tests;

/// <summary>
/// The public API of an assembly as text, one line per type and per member a consumer can reach,
/// in ordinal order: the listing rule SL6 compares with the committed baseline in both directions.
/// </summary>
/// <remarks>
/// <para>
/// It is read from metadata, as every other rule here is, and says what binary compatibility
/// depends on: each exported type with its kind, its base type, its interfaces and its sealed,
/// abstract or static state; each public or protected member with its signature; each constant
/// and enum member with its value. Nothing private or internal appears, so a change behind the
/// surface moves nothing here.
/// </para>
/// <para>
/// The baseline is <c>&lt;Project&gt;/PublicAPI.txt</c> beside each library's project file. When
/// an API change is intended, setting <see cref="UpdateVariable"/> for one test run rewrites the
/// baselines; the diff is then reviewed like any other change, and convention 16 says which
/// version it needs.
/// </para>
/// </remarks>
internal static class ApiListing
{
    internal const string UpdateVariable = "BROILER_ECMASCRIPT_UPDATE_API";

    internal const string BaselineFile = "PublicAPI.txt";

    internal static IReadOnlyList<string> Read(string path) => Read(File.ReadAllBytes(path));

    internal static IReadOnlyList<string> Read(byte[] image)
    {
        using var reader = new PEReader(ImmutableArray.Create(image));
        var metadata = reader.GetMetadataReader();
        var names = new SignatureNames(metadata);
        var lines = new List<string>();

        foreach (var handle in metadata.TypeDefinitions)
        {
            var type = metadata.GetTypeDefinition(handle);
            if (!IsExported(metadata, type))
                continue;
            var typeName = SignatureNames.Qualified(metadata, handle);
            lines.Add(TypeLine(metadata, names, type, typeName));

            var isEnum = BaseTypeName(metadata, names, type) == "System.Enum";
            foreach (var fieldHandle in type.GetFields())
            {
                var field = metadata.GetFieldDefinition(fieldHandle);
                if (!IsVisible(field.Attributes & FieldAttributes.FieldAccessMask))
                    continue;
                var line = typeName + "." + metadata.GetString(field.Name);
                if (isEnum && (field.Attributes & FieldAttributes.Literal) != 0)
                {
                    lines.Add(line + " = " + Constant(metadata, field.GetDefaultValue()));
                    continue;
                }

                var modifiers = Modifiers(
                    ((field.Attributes & FieldAttributes.Static) != 0, "static"),
                    ((field.Attributes & FieldAttributes.Literal) != 0, "const"),
                    ((field.Attributes & FieldAttributes.InitOnly) != 0, "readonly"));
                var value = (field.Attributes & FieldAttributes.Literal) != 0 ? " = " + Constant(metadata, field.GetDefaultValue()) : string.Empty;
                lines.Add(line + " field " + modifiers + field.DecodeSignature(names, null) + value);
            }

            foreach (var methodHandle in type.GetMethods())
            {
                var method = metadata.GetMethodDefinition(methodHandle);
                if (!IsVisible(method.Attributes & MethodAttributes.MemberAccessMask))
                    continue;
                var signature = method.DecodeSignature(names, null);
                var generic = method.GetGenericParameters().Count;
                var name = metadata.GetString(method.Name) + (generic > 0 ? "``" + generic.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty);
                var modifiers = Modifiers(
                    ((method.Attributes & MethodAttributes.Family) == MethodAttributes.Family && (method.Attributes & MethodAttributes.Public) != MethodAttributes.Public, "protected"),
                    ((method.Attributes & MethodAttributes.Static) != 0, "static"),
                    ((method.Attributes & MethodAttributes.Abstract) != 0, "abstract"),
                    ((method.Attributes & MethodAttributes.Virtual) != 0 && (method.Attributes & MethodAttributes.Abstract) == 0 && (method.Attributes & MethodAttributes.Final) == 0, "virtual"));
                lines.Add(typeName + "." + name + "(" + string.Join(", ", signature.ParameterTypes) + ") method " + modifiers + signature.ReturnType);
            }
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }

    internal static string BaselinePath(SharedProject project) => Path.Combine(project.Directory, BaselineFile);

    internal static IReadOnlyList<string> ReadBaseline(string path) =>
        File.Exists(path)
            ? File.ReadAllLines(path).Where(static l => l.Length > 0 && !l.StartsWith('#')).ToList()
            : [];

    internal static string Render(string assembly, IReadOnlyList<string> listing) =>
        "# The public API of " + assembly + ", generated by Broiler.ECMAScript.Architecture.Tests (rule SL6).\n"
        + "# Regenerate with " + UpdateVariable + "=1 dotnet test Broiler.ECMAScript/Broiler.ECMAScript.Architecture.Tests;\n"
        + "# a change here is an API change, versioned under convention 16.\n"
        + string.Concat(listing.Select(static l => l + "\n"));

    private static string TypeLine(MetadataReader metadata, SignatureNames names, TypeDefinition type, string typeName)
    {
        var attributes = type.Attributes;
        var baseType = BaseTypeName(metadata, names, type);
        var kind = (attributes & TypeAttributes.Interface) != 0 ? "interface"
            : baseType == "System.Enum" ? "enum"
            : baseType == "System.ValueType" ? "struct"
            : baseType == "System.MulticastDelegate" ? "delegate"
            : "class";
        var modifiers = kind == "class"
            ? Modifiers(
                ((attributes & TypeAttributes.Abstract) != 0 && (attributes & TypeAttributes.Sealed) != 0, "static"),
                ((attributes & TypeAttributes.Abstract) != 0 && (attributes & TypeAttributes.Sealed) == 0, "abstract"),
                ((attributes & TypeAttributes.Sealed) != 0 && (attributes & TypeAttributes.Abstract) == 0, "sealed"))
            : string.Empty;
        var interfaces = type.GetInterfaceImplementations()
            .Select(h => TypeName(metadata, names, metadata.GetInterfaceImplementation(h).Interface))
            .Order(StringComparer.Ordinal)
            .ToList();
        var bases = new List<string>();
        if (kind == "class" && baseType is not null && baseType != "System.Object")
            bases.Add(baseType);
        bases.AddRange(interfaces);
        return typeName + " " + modifiers + kind + (bases.Count > 0 ? " : " + string.Join(", ", bases) : string.Empty);
    }

    private static string? BaseTypeName(MetadataReader metadata, SignatureNames names, TypeDefinition type) =>
        type.BaseType.IsNil ? null : TypeName(metadata, names, type.BaseType);

    private static string TypeName(MetadataReader metadata, SignatureNames names, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.TypeReference => SignatureNames.Qualified(metadata, (TypeReferenceHandle)handle),
        HandleKind.TypeDefinition => SignatureNames.Qualified(metadata, (TypeDefinitionHandle)handle),
        HandleKind.TypeSpecification => metadata.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(names, null),
        _ => "?",
    };

    private static bool IsExported(MetadataReader metadata, TypeDefinition type)
    {
        var visibility = type.Attributes & TypeAttributes.VisibilityMask;
        if (visibility == TypeAttributes.Public)
            return true;
        if (visibility is not (TypeAttributes.NestedPublic or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem))
            return false;
        var declaring = type.GetDeclaringType();
        return !declaring.IsNil && IsExported(metadata, metadata.GetTypeDefinition(declaring));
    }

    private static bool IsVisible<T>(T access) where T : Enum
    {
        var value = Convert.ToInt32(access, System.Globalization.CultureInfo.InvariantCulture);

        // Public (6), Family (4) and FamORAssem (5) are reachable by a consumer; FamANDAssem (2) is not.
        return value is 4 or 5 or 6;
    }

    private static string Modifiers(params (bool On, string Word)[] modifiers) =>
        string.Concat(modifiers.Where(static m => m.On).Select(static m => m.Word + " "));

    private static string Constant(MetadataReader metadata, ConstantHandle handle)
    {
        if (handle.IsNil)
            return "?";
        var constant = metadata.GetConstant(handle);
        var blob = metadata.GetBlobReader(constant.Value);
        return constant.TypeCode switch
        {
            ConstantTypeCode.Boolean => blob.ReadBoolean() ? "true" : "false",
            ConstantTypeCode.Char => "'\\u" + ((int)blob.ReadChar()).ToString("X4", System.Globalization.CultureInfo.InvariantCulture) + "'",
            ConstantTypeCode.SByte => blob.ReadSByte().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Byte => blob.ReadByte().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Int16 => blob.ReadInt16().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt16 => blob.ReadUInt16().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Int32 => blob.ReadInt32().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt32 => blob.ReadUInt32().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Int64 => blob.ReadInt64().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt64 => blob.ReadUInt64().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Single => "0x" + BitConverter.SingleToUInt32Bits(blob.ReadSingle()).ToString("X8", System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Double => "0x" + BitConverter.DoubleToUInt64Bits(blob.ReadDouble()).ToString("X16", System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.String => "\"" + blob.ReadUTF16(blob.Length) + "\"",
            ConstantTypeCode.NullReference => "null",
            _ => "?",
        };
    }
}
