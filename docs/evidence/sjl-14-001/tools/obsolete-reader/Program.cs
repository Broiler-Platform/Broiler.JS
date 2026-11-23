// Bundle sjl-14-001's part F metadata reader, drafted on 2026-11-19 on window W2's branch before any run of part F.
//
//     dotnet run --project docs/evidence/sjl-14-001/tools/obsolete-reader -c Release -- <assembly.dll | package.nupkg>...
//
// For every public type of each assembly (a package's lib/net10.0/*.dll is read from inside the .nupkg), one line:
//     assembly <TAB> type <TAB> obsolete (yes/no) <TAB> diagnostic identifier <TAB> error (true/false) <TAB> message
// and then the assembly's type forwarders, one line each: assembly <TAB> type <TAB> forwarded <TAB> target assembly.
// A nested type is listed with its declaring type, `Outer+Inner`, and is public when it and every type it is nested in
// are public. The reader decodes the [Obsolete] attribute's constructor arguments (message, error) and its named
// arguments (DiagnosticId, UrlFormat) from the metadata alone; it loads no assembly and runs no code of the engine.
// Part F's reading compares these lines with dispositions.tsv; the reader judges nothing.

using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

if (args.Length == 0)
{
    Console.Error.WriteLine("usage: obsolete-reader <assembly.dll | package.nupkg>...");
    return 2;
}

var output = new StringBuilder();
output.Append("assembly\ttype\tobsolete\tdiagnosticId\terror\tmessage\n");
foreach (var path in args)
{
    if (path.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
    {
        using var package = ZipFile.OpenRead(path);
        foreach (var entry in package.Entries.Where(e => e.FullName.StartsWith("lib/net10.0/", StringComparison.Ordinal)
                     && e.FullName.EndsWith(".dll", StringComparison.Ordinal)).OrderBy(e => e.FullName, StringComparer.Ordinal))
        {
            using var stream = new MemoryStream();
            using (var source = entry.Open())
                source.CopyTo(stream);
            stream.Position = 0;
            Read(stream, output);
        }
    }
    else
    {
        using var stream = File.OpenRead(path);
        Read(stream, output);
    }
}

Console.Out.Write(output.ToString());
return 0;

static void Read(Stream stream, StringBuilder output)
{
    using var pe = new PEReader(stream);
    var reader = pe.GetMetadataReader();
    var assembly = reader.GetString(reader.GetAssemblyDefinition().Name);
    var rows = new List<string>();
    foreach (var handle in reader.TypeDefinitions)
    {
        var type = reader.GetTypeDefinition(handle);
        if (!IsPublic(reader, type))
            continue;

        var name = FullName(reader, type);
        string obsolete = "no", id = "", error = "", message = "";
        foreach (var attributeHandle in type.GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(attributeHandle);
            if (AttributeTypeName(reader, attribute) != "System.ObsoleteAttribute")
                continue;

            var value = attribute.DecodeValue(new Provider());
            obsolete = "yes";
            message = value.FixedArguments.Length > 0 ? value.FixedArguments[0].Value as string ?? "" : "";
            error = value.FixedArguments.Length > 1 ? (value.FixedArguments[1].Value is true ? "true" : "false") : "false";
            foreach (var named in value.NamedArguments)
            {
                if (named.Name == "DiagnosticId")
                    id = named.Value as string ?? "";
            }
        }

        rows.Add($"{assembly}\t{name}\t{obsolete}\t{id}\t{error}\t{message.Replace('\t', ' ').Replace('\n', ' ')}");
    }

    foreach (var handle in reader.ExportedTypes)
    {
        var exported = reader.GetExportedType(handle);
        if (!exported.IsForwarder)
            continue;

        var name = reader.GetString(exported.Namespace) + "." + reader.GetString(exported.Name);
        var target = exported.Implementation.Kind == HandleKind.AssemblyReference
            ? reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)exported.Implementation).Name)
            : "?";
        rows.Add($"{assembly}\t{name}\tforwarded\t\t\t{target}");
    }

    rows.Sort(StringComparer.Ordinal);
    foreach (var row in rows)
        output.Append(row).Append('\n');
}

static bool IsPublic(MetadataReader reader, TypeDefinition type)
{
    var visibility = type.Attributes & TypeAttributes.VisibilityMask;
    if (visibility == TypeAttributes.Public)
        return true;
    if (visibility != TypeAttributes.NestedPublic)
        return false;
    return IsPublic(reader, reader.GetTypeDefinition(type.GetDeclaringType()));
}

static string FullName(MetadataReader reader, TypeDefinition type)
{
    var name = reader.GetString(type.Name);
    var declaring = type.GetDeclaringType();
    if (!declaring.IsNil)
        return FullName(reader, reader.GetTypeDefinition(declaring)) + "+" + name;
    var space = reader.GetString(type.Namespace);
    return space.Length == 0 ? name : space + "." + name;
}

static string AttributeTypeName(MetadataReader reader, CustomAttribute attribute)
{
    EntityHandle owner;
    switch (attribute.Constructor.Kind)
    {
        case HandleKind.MemberReference:
            owner = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
            break;
        case HandleKind.MethodDefinition:
            owner = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
            break;
        default:
            return "";
    }

    return owner.Kind switch
    {
        HandleKind.TypeReference => reader.GetString(reader.GetTypeReference((TypeReferenceHandle)owner).Namespace) + "."
            + reader.GetString(reader.GetTypeReference((TypeReferenceHandle)owner).Name),
        HandleKind.TypeDefinition => reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)owner).Namespace) + "."
            + reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)owner).Name),
        _ => "",
    };
}

// The attribute decoder's type provider: [Obsolete]'s arguments are a string and a bool, and its named arguments strings.
sealed class Provider : ICustomAttributeTypeProvider<string>
{
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
    public string GetSystemType() => "System.Type";
    public string GetSZArrayType(string elementType) => elementType + "[]";
    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => reader.GetString(reader.GetTypeDefinition(handle).Name);
    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => reader.GetString(reader.GetTypeReference(handle).Name);
    public string GetTypeFromSerializedName(string name) => name;
    public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;
    public bool IsSystemType(string type) => type == "System.Type";
}
