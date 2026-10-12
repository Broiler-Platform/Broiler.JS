using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Broiler.ECMAScript.Architecture.Tests;

/// <summary>A static field an assembly declares, with what SL2 asks of it.</summary>
internal sealed record StaticField(string Type, string Name, string FieldType, bool IsReadOnly, bool IsLiteral);

/// <summary>
/// One assembly's metadata tables, read without loading it. Shaped after Broiler.VM's
/// <c>AssemblyFacts</c>, with member references carrying their parameter types, because SL4
/// tells a culture-sensitive overload from an ordinal one by its signature.
/// </summary>
internal sealed class AssemblyFacts
{
    private AssemblyFacts(string name)
    {
        Name = name;
    }

    internal string Name { get; }

    /// <summary>Every assembly named in the AssemblyRef table.</summary>
    internal ImmutableArray<string> AssemblyReferences { get; private init; }

    /// <summary>Every type named in the TypeRef table, as <c>Namespace.Type</c>, nested types as <c>Outer+Inner</c>.</summary>
    internal ImmutableArray<string> TypeReferences { get; private init; }

    /// <summary>
    /// Every member reference: a method as <c>Namespace.Type.Name(ParameterType,...)</c>, a field as
    /// <c>Namespace.Type.Name</c>. A generic type's members are named on its definition,
    /// <c>System.Collections.Generic.List`1.Add(!0)</c>.
    /// </summary>
    internal ImmutableArray<string> MemberReferences { get; private init; }

    /// <summary>Every custom attribute type applied anywhere in the assembly.</summary>
    internal ImmutableArray<string> CustomAttributeTypes { get; private init; }

    /// <summary>Every platform invoke the assembly declares, as <c>module!entryPoint</c>.</summary>
    internal ImmutableArray<string> PlatformInvokes { get; private init; }

    /// <summary>Every static field a type the source declares holds; compiler-generated types and fields are left out.</summary>
    internal ImmutableArray<StaticField> StaticFields { get; private init; }

    /// <summary>Every static property with a setter a type the source declares, as <c>Namespace.Type.Name</c>.</summary>
    internal ImmutableArray<string> SettableStaticProperties { get; private init; }

    internal static AssemblyFacts Read(string path) => Read(File.ReadAllBytes(path));

    internal static AssemblyFacts Read(byte[] image)
    {
        using var reader = new PEReader(ImmutableArray.Create(image));
        var metadata = reader.GetMetadataReader();
        var provider = new SignatureNames(metadata);
        var name = metadata.GetString(metadata.GetAssemblyDefinition().Name);

        var assemblyReferences = metadata.AssemblyReferences
            .Select(h => metadata.GetString(metadata.GetAssemblyReference(h).Name));

        var typeReferences = metadata.TypeReferences
            .Select(h => SignatureNames.Qualified(metadata, h));

        var memberReferences = metadata.MemberReferences
            .Select(h => DescribeMember(metadata, provider, metadata.GetMemberReference(h)));

        var attributes = metadata.CustomAttributes
            .Select(h => AttributeType(metadata, metadata.GetCustomAttribute(h)))
            .OfType<string>();

        var invokes = metadata.MethodDefinitions
            .Select(metadata.GetMethodDefinition)
            .Where(static d => (d.Attributes & MethodAttributes.PinvokeImpl) != 0)
            .Select(d =>
            {
                var import = d.GetImport();
                var module = import.Module.IsNil ? string.Empty : metadata.GetString(metadata.GetModuleReference(import.Module).Name);
                return module + "!" + metadata.GetString(import.Name.IsNil ? d.Name : import.Name);
            });

        var statics = new List<StaticField>();
        var settable = new List<string>();
        foreach (var handle in metadata.TypeDefinitions)
        {
            var type = metadata.GetTypeDefinition(handle);
            var typeName = SignatureNames.Qualified(metadata, handle);
            if (IsCompilerGenerated(typeName))
                continue;

            foreach (var fieldHandle in type.GetFields())
            {
                var field = metadata.GetFieldDefinition(fieldHandle);
                if ((field.Attributes & FieldAttributes.Static) == 0)
                    continue;
                var fieldName = metadata.GetString(field.Name);
                if (fieldName.StartsWith('<'))
                    continue;
                statics.Add(new StaticField(
                    typeName,
                    fieldName,
                    field.DecodeSignature(provider, null),
                    (field.Attributes & FieldAttributes.InitOnly) != 0,
                    (field.Attributes & FieldAttributes.Literal) != 0));
            }

            foreach (var propertyHandle in type.GetProperties())
            {
                var property = metadata.GetPropertyDefinition(propertyHandle);
                var setter = property.GetAccessors().Setter;
                if (!setter.IsNil && (metadata.GetMethodDefinition(setter).Attributes & MethodAttributes.Static) != 0)
                    settable.Add(typeName + "." + metadata.GetString(property.Name));
            }
        }

        return new AssemblyFacts(name)
        {
            AssemblyReferences = Sorted(assemblyReferences),
            TypeReferences = Sorted(typeReferences),
            MemberReferences = Sorted(memberReferences),
            CustomAttributeTypes = Sorted(attributes),
            PlatformInvokes = Sorted(invokes),
            StaticFields = [.. statics],
            SettableStaticProperties = Sorted(settable),
        };
    }

    /// <summary>A type the compiler made rather than the source: a closure, a lambda cache, a state machine, private implementation details.</summary>
    internal static bool IsCompilerGenerated(string qualifiedName) => qualifiedName.Contains('<', StringComparison.Ordinal);

    private static ImmutableArray<string> Sorted(IEnumerable<string> values) =>
        [.. values.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    private static string DescribeMember(MetadataReader metadata, SignatureNames provider, MemberReference member)
    {
        var parent = member.Parent.Kind switch
        {
            HandleKind.TypeReference => SignatureNames.Qualified(metadata, (TypeReferenceHandle)member.Parent),
            HandleKind.TypeDefinition => SignatureNames.Qualified(metadata, (TypeDefinitionHandle)member.Parent),
            HandleKind.TypeSpecification => GenericDefinition(metadata.GetTypeSpecification((TypeSpecificationHandle)member.Parent).DecodeSignature(provider, null)),
            _ => "?",
        };
        var name = parent + "." + metadata.GetString(member.Name);
        if (member.GetKind() != MemberReferenceKind.Method)
            return name;
        var signature = member.DecodeMethodSignature(provider, null);
        return name + "(" + string.Join(",", signature.ParameterTypes) + ")";
    }

    /// <summary><c>System.Collections.Generic.List`1&lt;System.String&gt;</c> as <c>System.Collections.Generic.List`1</c>.</summary>
    private static string GenericDefinition(string instantiated)
    {
        var bracket = instantiated.IndexOf('<', StringComparison.Ordinal);
        return bracket < 0 ? instantiated : instantiated[..bracket];
    }

    private static string? AttributeType(MetadataReader metadata, CustomAttribute attribute)
    {
        EntityHandle parent;
        if (attribute.Constructor.Kind == HandleKind.MemberReference)
            parent = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
        else if (attribute.Constructor.Kind == HandleKind.MethodDefinition)
            parent = metadata.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
        else
            return null;

        return parent.Kind switch
        {
            HandleKind.TypeReference => SignatureNames.Qualified(metadata, (TypeReferenceHandle)parent),
            HandleKind.TypeDefinition => SignatureNames.Qualified(metadata, (TypeDefinitionHandle)parent),
            _ => null,
        };
    }
}

/// <summary>Renders signature types as names: <c>System.Int32</c>, <c>System.String[]</c>, <c>List`1&lt;System.String&gt;</c>, <c>!0</c>, <c>!!0</c>.</summary>
internal sealed class SignatureNames(MetadataReader metadata) : ISignatureTypeProvider<string, object?>
{
    internal static string Qualified(MetadataReader metadata, TypeReferenceHandle handle)
    {
        var reference = metadata.GetTypeReference(handle);
        var name = metadata.GetString(reference.Name);
        if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
            return Qualified(metadata, (TypeReferenceHandle)reference.ResolutionScope) + "+" + name;
        var ns = metadata.GetString(reference.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    internal static string Qualified(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        var definition = metadata.GetTypeDefinition(handle);
        var name = metadata.GetString(definition.Name);
        var declaring = definition.GetDeclaringType();
        if (!declaring.IsNil)
            return Qualified(metadata, declaring) + "+" + name;
        var ns = metadata.GetString(definition.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[" + new string(',', shape.Rank - 1) + "]";

    public string GetByReferenceType(string elementType) => elementType + "&";

    public string GetFunctionPointerType(MethodSignature<string> signature) => "method " + signature.ReturnType + "*(" + string.Join(",", signature.ParameterTypes) + ")";

    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) => genericType + "<" + string.Join(",", typeArguments) + ">";

    public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>A required modifier is part of the signature a caller binds to (an init accessor's <c>IsExternalInit</c>); an optional one is not.</summary>
    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) =>
        isRequired ? unmodifiedType + " modreq(" + modifier + ")" : unmodifiedType;

    public string GetPinnedType(string elementType) => elementType;

    public string GetPointerType(string elementType) => elementType + "*";

    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.Boolean => "System.Boolean",
        PrimitiveTypeCode.Byte => "System.Byte",
        PrimitiveTypeCode.Char => "System.Char",
        PrimitiveTypeCode.Double => "System.Double",
        PrimitiveTypeCode.Int16 => "System.Int16",
        PrimitiveTypeCode.Int32 => "System.Int32",
        PrimitiveTypeCode.Int64 => "System.Int64",
        PrimitiveTypeCode.IntPtr => "System.IntPtr",
        PrimitiveTypeCode.Object => "System.Object",
        PrimitiveTypeCode.SByte => "System.SByte",
        PrimitiveTypeCode.Single => "System.Single",
        PrimitiveTypeCode.String => "System.String",
        PrimitiveTypeCode.TypedReference => "System.TypedReference",
        PrimitiveTypeCode.UInt16 => "System.UInt16",
        PrimitiveTypeCode.UInt32 => "System.UInt32",
        PrimitiveTypeCode.UInt64 => "System.UInt64",
        PrimitiveTypeCode.UIntPtr => "System.UIntPtr",
        PrimitiveTypeCode.Void => "System.Void",
        _ => typeCode.ToString(),
    };

    public string GetSZArrayType(string elementType) => elementType + "[]";

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => Qualified(reader, handle);

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => Qualified(reader, handle);

    public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    internal MetadataReader Metadata { get; } = metadata;
}
