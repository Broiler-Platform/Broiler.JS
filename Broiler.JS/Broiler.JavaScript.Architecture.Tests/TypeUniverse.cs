using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Broiler.ECMAScript.Architecture.Tests;

namespace Broiler.JavaScript.Architecture.Tests;

/// <summary>One assembly's metadata, as the universe opened it.</summary>
internal sealed class MetadataModule(string name, MetadataReader metadata)
{
    internal string Name { get; } = name;

    internal MetadataReader Metadata { get; } = metadata;
}

internal enum ShapeKind
{
    Primitive,
    String,
    Object,
    Array,
    Named,
    Generic,
    Other,
}

/// <summary>
/// A signature type, named as <see cref="SignatureNames"/> names it (<c>System.Int32</c>,
/// <c>Ns.Outer+Inner</c>, <c>List`1&lt;System.String&gt;</c>), with what is needed to find its
/// definition: the module and handle of a named type, the definition of a generic instantiation.
/// </summary>
internal sealed record TypeShape(string Name, ShapeKind Kind, MetadataModule? Module = null, EntityHandle Handle = default, TypeShape? Definition = null);

/// <summary>What the clauses ask of a type definition.</summary>
internal sealed record TypeDescription(bool IsEnum, bool IsValueType, bool IsReadOnly, IReadOnlyList<string> MutableFields);

/// <summary>Decodes signatures into <see cref="TypeShape"/>s of one module.</summary>
internal sealed class ShapeProvider(MetadataModule module) : ISignatureTypeProvider<TypeShape, object?>
{
    public TypeShape GetArrayType(TypeShape elementType, ArrayShape shape) =>
        new(elementType.Name + "[" + new string(',', shape.Rank - 1) + "]", ShapeKind.Array);

    public TypeShape GetByReferenceType(TypeShape elementType) => new(elementType.Name + "&", ShapeKind.Other);

    public TypeShape GetFunctionPointerType(MethodSignature<TypeShape> signature) =>
        new("method " + signature.ReturnType.Name + "*(" + string.Join(",", signature.ParameterTypes.Select(static p => p.Name)) + ")", ShapeKind.Other);

    public TypeShape GetGenericInstantiation(TypeShape genericType, ImmutableArray<TypeShape> typeArguments) =>
        new(genericType.Name + "<" + string.Join(",", typeArguments.Select(static a => a.Name)) + ">", ShapeKind.Generic, Definition: genericType);

    public TypeShape GetGenericMethodParameter(object? genericContext, int index) =>
        new("!!" + index.ToString(CultureInfo.InvariantCulture), ShapeKind.Other);

    public TypeShape GetGenericTypeParameter(object? genericContext, int index) =>
        new("!" + index.ToString(CultureInfo.InvariantCulture), ShapeKind.Other);

    public TypeShape GetModifiedType(TypeShape modifier, TypeShape unmodifiedType, bool isRequired) => unmodifiedType;

    public TypeShape GetPinnedType(TypeShape elementType) => elementType;

    public TypeShape GetPointerType(TypeShape elementType) => new(elementType.Name + "*", ShapeKind.Other);

    public TypeShape GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.String => new("System.String", ShapeKind.String),
        PrimitiveTypeCode.Object => new("System.Object", ShapeKind.Object),
        PrimitiveTypeCode.TypedReference => new("System.TypedReference", ShapeKind.Other),
        _ => new(Names.GetPrimitiveType(typeCode), ShapeKind.Primitive),
    };

    public TypeShape GetSZArrayType(TypeShape elementType) => new(elementType.Name + "[]", ShapeKind.Array);

    public TypeShape GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        new(SignatureNames.Qualified(reader, handle), ShapeKind.Named, module, handle);

    public TypeShape GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
        new(SignatureNames.Qualified(reader, handle), ShapeKind.Named, module, handle);

    public TypeShape GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    private SignatureNames Names { get; } = new(module.Metadata);
}

/// <summary>
/// The assemblies a judged assembly's static fields can name: the judged image itself, the
/// assemblies in its directory (the engine's own), and the platform's trusted assemblies, with type
/// forwarders followed. Types are found by metadata alone; nothing is loaded.
/// </summary>
internal sealed class TypeUniverse : IDisposable
{
    private const int MaximumForwards = 8;

    private readonly string directory;
    private readonly Dictionary<string, string> platform;
    private readonly Dictionary<string, MetadataModule?> opened = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PEReader> readers = [];

    internal TypeUniverse(byte[] image, string directory)
    {
        this.directory = directory;
        platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .GroupBy(static p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static g => g.Key, static g => g.First(), StringComparer.OrdinalIgnoreCase);

        var reader = new PEReader(ImmutableArray.Create(image));
        readers.Add(reader);
        var metadata = reader.GetMetadataReader();
        Self = new MetadataModule(metadata.GetString(metadata.GetAssemblyDefinition().Name), metadata);
        opened[Self.Name] = Self;
    }

    /// <summary>The judged assembly.</summary>
    internal MetadataModule Self { get; }

    public void Dispose()
    {
        foreach (var reader in readers)
            reader.Dispose();
    }

    /// <summary>
    /// What the type <paramref name="handle"/> of <paramref name="module"/> is, with the mutable
    /// instance fields it and its base types declare; null when a definition cannot be read.
    /// </summary>
    internal TypeDescription? Describe(MetadataModule module, EntityHandle handle)
    {
        var mutable = new List<string>();
        var first = true;
        bool isEnum = false, isValueType = false, isReadOnly = false;

        for (var current = Resolve(module, handle); ; )
        {
            if (current is not { } found)
                return null;

            var (owner, definitionHandle) = found;
            var metadata = owner.Metadata;
            var definition = metadata.GetTypeDefinition(definitionHandle);
            var baseShape = definition.BaseType.IsNil ? null : Shape(owner, definition.BaseType);
            var baseName = baseShape?.Kind == ShapeKind.Generic ? baseShape.Definition!.Name : baseShape?.Name;

            if (first)
            {
                isEnum = baseName == "System.Enum";
                isValueType = isEnum || baseName == "System.ValueType";
                isReadOnly = definition.GetCustomAttributes()
                    .Any(a => AttributeType(metadata, metadata.GetCustomAttribute(a)) == "System.Runtime.CompilerServices.IsReadOnlyAttribute");
                first = false;
            }

            foreach (var fieldHandle in definition.GetFields())
            {
                var field = metadata.GetFieldDefinition(fieldHandle);
                if ((field.Attributes & (FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.Literal)) == 0)
                    mutable.Add(metadata.GetString(field.Name));
            }

            if (baseShape is null || baseName is "System.Object" or "System.ValueType" or "System.Enum")
                return new TypeDescription(isEnum, isValueType, isReadOnly, mutable);

            var next = baseShape.Kind == ShapeKind.Generic ? baseShape.Definition! : baseShape;
            if (next.Kind != ShapeKind.Named)
                return null;
            current = Resolve(next.Module!, next.Handle);
        }
    }

    /// <summary>The attribute type a custom attribute's constructor belongs to, as <c>Namespace.Type</c>.</summary>
    internal static string? AttributeType(MetadataReader metadata, CustomAttribute attribute)
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

    private static TypeShape Shape(MetadataModule module, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.TypeDefinition => new ShapeProvider(module).GetTypeFromDefinition(module.Metadata, (TypeDefinitionHandle)handle, 0),
        HandleKind.TypeReference => new ShapeProvider(module).GetTypeFromReference(module.Metadata, (TypeReferenceHandle)handle, 0),
        HandleKind.TypeSpecification => module.Metadata.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(new ShapeProvider(module), null),
        _ => new TypeShape("?", ShapeKind.Other),
    };

    private (MetadataModule Module, TypeDefinitionHandle Handle)? Resolve(MetadataModule module, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.TypeDefinition => (module, (TypeDefinitionHandle)handle),
        HandleKind.TypeReference => Resolve(module, (TypeReferenceHandle)handle),
        _ => null,
    };

    private (MetadataModule Module, TypeDefinitionHandle Handle)? Resolve(MetadataModule module, TypeReferenceHandle handle)
    {
        var metadata = module.Metadata;
        var reference = metadata.GetTypeReference(handle);
        var ns = metadata.GetString(reference.Namespace);
        var name = metadata.GetString(reference.Name);
        var scope = reference.ResolutionScope;

        switch (scope.Kind)
        {
            case HandleKind.TypeReference:
                return Resolve(module, (TypeReferenceHandle)scope) is { } outer ? Nested(outer.Module, outer.Handle, name) : null;
            case HandleKind.AssemblyReference:
                var target = Open(metadata.GetString(metadata.GetAssemblyReference((AssemblyReferenceHandle)scope).Name));
                return target is null ? null : Find(target, ns, name, 0);
            case HandleKind.ModuleDefinition:
            case HandleKind.ModuleReference:
                return Find(module, ns, name, 0);
            default:
                return null;
        }
    }

    private static (MetadataModule, TypeDefinitionHandle)? Nested(MetadataModule module, TypeDefinitionHandle outer, string name)
    {
        var metadata = module.Metadata;
        foreach (var nested in metadata.GetTypeDefinition(outer).GetNestedTypes())
        {
            if (metadata.StringComparer.Equals(metadata.GetTypeDefinition(nested).Name, name))
                return (module, nested);
        }

        return null;
    }

    private (MetadataModule, TypeDefinitionHandle)? Find(MetadataModule module, string ns, string name, int forwards)
    {
        var metadata = module.Metadata;
        foreach (var handle in metadata.TypeDefinitions)
        {
            var definition = metadata.GetTypeDefinition(handle);
            if (definition.GetDeclaringType().IsNil
                && metadata.StringComparer.Equals(definition.Namespace, ns)
                && metadata.StringComparer.Equals(definition.Name, name))
                return (module, handle);
        }

        if (forwards >= MaximumForwards)
            return null;

        foreach (var handle in metadata.ExportedTypes)
        {
            var exported = metadata.GetExportedType(handle);
            if (exported.Implementation.Kind != HandleKind.AssemblyReference
                || !metadata.StringComparer.Equals(exported.Namespace, ns)
                || !metadata.StringComparer.Equals(exported.Name, name))
                continue;
            var target = Open(metadata.GetString(metadata.GetAssemblyReference((AssemblyReferenceHandle)exported.Implementation).Name));
            return target is null ? null : Find(target, ns, name, forwards + 1);
        }

        return null;
    }

    private MetadataModule? Open(string assemblyName)
    {
        if (opened.TryGetValue(assemblyName, out var module))
            return module;

        var path = Path.Combine(directory, assemblyName + ".dll");
        if (!File.Exists(path) && !platform.TryGetValue(assemblyName, out path))
            path = null;

        if (path is not null)
        {
            var reader = new PEReader(File.OpenRead(path));
            readers.Add(reader);
            module = new MetadataModule(assemblyName, reader.GetMetadataReader());
        }

        opened[assemblyName] = module;
        return module;
    }
}
