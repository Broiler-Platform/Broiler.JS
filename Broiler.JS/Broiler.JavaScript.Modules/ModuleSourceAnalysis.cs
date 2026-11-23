using System;
using System.Collections.Generic;
using Broiler.ECMAScript.Syntax;
using Broiler.JavaScript.Parser.Bridge;

namespace Broiler.JavaScript.Modules;

/// <summary>A ModuleRequest Record: a specifier and the module type its attributes assert.</summary>
internal sealed record ModuleRequest(string Specifier, string Type)
{
    /// <summary>The key of [[LoadedModules]]: requests are equal when their specifiers and
    /// attributes are (ModuleRequestsEqual).</summary>
    public string CacheKey => Type == null ? Specifier : Specifier + "\0" + Type;
}

/// <summary>An ImportEntry Record. A null <see cref="ImportName"/> is ~namespace-object~.</summary>
internal sealed record ImportEntry(ModuleRequest Request, string ImportName, string LocalName);

/// <summary>An ExportEntry Record. For an indirect entry a null <see cref="ImportName"/> is ~all~
/// (<c>export * as ns from</c>).</summary>
internal sealed record ExportEntry(string ExportName, ModuleRequest Request, string ImportName, string LocalName);

/// <summary>
/// The static structure of an ECMAScript module, read from its syntax tree as ParseModule
/// (ES2024 16.2.1.6.1) does: its requested modules in source order, its import entries, and its
/// export entries split into local, indirect and star entries.
/// </summary>
/// <remarks>
/// It reads the shared syntax library's tree (milestone SJL-14.3, window W2; Broiler.VM's JSD-0083, decision D41),
/// parsed with the module goal through the bridge's request on the library's documented stack (decision D47 (a),
/// <see cref="SharedSyntaxBridge.ParseSharedTree"/>). Until W2 it read the tree <c>FastParser.ParseModule</c> built.
/// </remarks>
internal sealed class ModuleSourceAnalysis
{
    public readonly List<ModuleRequest> RequestedModules = [];
    public readonly List<ImportEntry> ImportEntries = [];
    public readonly List<ExportEntry> LocalExportEntries = [];
    public readonly List<ExportEntry> IndirectExportEntries = [];
    public readonly List<ExportEntry> StarExportEntries = [];

    /// <summary>[[HasTLA]]: whether the module body contains <c>await</c> outside any function.</summary>
    public bool HasTopLevelAwait { get; private set; }

    private readonly Dictionary<string, ModuleRequest> requests = new(StringComparer.Ordinal);

    /// <summary>Parses <paramref name="code"/> with the module goal and reads its entries. The
    /// source must already have compiled, so every early error has been reported.</summary>
    public static ModuleSourceAnalysis Parse(string code)
    {
        var program = SharedSyntaxBridge.ParseSharedTree(code, BridgeRequest.Module);
        var analysis = new ModuleSourceAnalysis { HasTopLevelAwait = program.HasTopLevelAwait };
        analysis.Read(program);
        return analysis;
    }

    private ModuleRequest Request(StringLiteral source, IReadOnlyList<ImportAttribute> attributes)
    {
        string type = null;
        if (attributes != null)
        {
            foreach (var attribute in attributes)
            {
                if (NameOf(attribute.Key) == "type")
                    type = attribute.Value.Value;
            }
        }

        var request = new ModuleRequest(source.Value, type);
        if (requests.TryGetValue(request.CacheKey, out var existing))
            return existing;

        requests[request.CacheKey] = request;
        RequestedModules.Add(request);
        return request;
    }

    private void Read(ProgramNode program)
    {
        var exportEntries = new List<ExportEntry>();
        foreach (var statement in program.Body)
        {
            switch (statement)
            {
                case ImportDeclaration import:
                    var importRequest = Request(import.Source, import.Attributes);
                    if (import.DefaultBinding != null)
                        ImportEntries.Add(new ImportEntry(importRequest, "default", import.DefaultBinding.Name));

                    if (import.NamespaceBinding != null)
                        ImportEntries.Add(new ImportEntry(importRequest, null, import.NamespaceBinding.Name));

                    if (import.NamedImports != null)
                    {
                        foreach (var member in import.NamedImports)
                            ImportEntries.Add(new ImportEntry(importRequest, member.Imported == null ? member.Local.Name : NameOf(member.Imported), member.Local.Name));
                    }

                    break;

                case ExportNamedDeclaration export:
                    ReadExport(export, exportEntries);
                    break;

                case ExportAllDeclaration exportAll:
                    // `export * as ns from 'm'` exports m's namespace as `ns`; `export * from 'm'` is a
                    // star entry (all-but-default, no export name).
                    exportEntries.Add(new ExportEntry(exportAll.Exported == null ? null : NameOf(exportAll.Exported),
                        Request(exportAll.Source, exportAll.Attributes), null, null));
                    break;

                case ExportDefaultDeclaration exportDefault:
                    ReadDefaultExport(exportDefault, exportEntries);
                    break;
            }
        }

        // ParseModule: an export of an imported binding is rewritten to re-export it from the
        // module it was imported from.
        foreach (var entry in exportEntries)
        {
            if (entry.Request != null)
            {
                if (entry.ExportName == null)
                    StarExportEntries.Add(entry);
                else
                    IndirectExportEntries.Add(entry);

                continue;
            }

            var import = ImportEntries.Find(i => i.LocalName == entry.LocalName);
            if (import == null)
            {
                LocalExportEntries.Add(entry);
                continue;
            }

            IndirectExportEntries.Add(new ExportEntry(entry.ExportName, import.Request, import.ImportName, null));
        }
    }

    private void ReadExport(ExportNamedDeclaration export, List<ExportEntry> entries)
    {
        if (export.Source != null)
        {
            var request = Request(export.Source, export.Attributes);
            foreach (var member in export.Specifiers)
                entries.Add(new ExportEntry(NameOf(member.Exported ?? member.Local), request, NameOf(member.Local), null));

            return;
        }

        if (export.Declaration == null)
        {
            foreach (var member in export.Specifiers)
                entries.Add(new ExportEntry(NameOf(member.Exported ?? member.Local), null, null, NameOf(member.Local)));

            return;
        }

        switch (export.Declaration)
        {
            case VariableDeclaration declaration:
                var names = new List<string>();
                foreach (var declarator in declaration.Declarations)
                    BoundNames(declarator.Target, names);

                foreach (var name in names)
                    entries.Add(new ExportEntry(name, null, null, name));
                return;

            case FunctionDeclaration { Function.Name: { } id }:
                entries.Add(new ExportEntry(id.Name, null, null, id.Name));
                return;

            case ClassDeclaration { Class.Name: { } identifier }:
                entries.Add(new ExportEntry(identifier.Name, null, null, identifier.Name));
                return;
        }
    }

    private static void ReadDefaultExport(ExportDefaultDeclaration export, List<ExportEntry> entries)
    {
        switch (export.Declaration)
        {
            case FunctionDeclaration { Function.Name: { } id }:
                entries.Add(new ExportEntry("default", null, null, id.Name));
                return;

            case ClassDeclaration { Class.Name: { } identifier }:
                entries.Add(new ExportEntry("default", null, null, identifier.Name));
                return;
        }

        entries.Add(new ExportEntry("default", null, null, "*default*"));
    }

    // A ModuleExportName or an AttributeKey: an IdentifierName's name or a string literal's value.
    private static string NameOf(SyntaxNode name) => name switch
    {
        IdentifierName identifierName => identifierName.Name,
        Identifier identifier => identifier.Name,
        BindingIdentifier binding => binding.Name,
        StringLiteral literal => literal.Value,
        _ => throw new InvalidOperationException("Not a module export name: " + name.Kind),
    };

    private static void BoundNames(Pattern target, List<string> names)
    {
        switch (target)
        {
            case BindingIdentifier identifier:
                names.Add(identifier.Name);
                break;

            case AssignmentPattern assignment:
                BoundNames(assignment.Target, names);
                break;

            case RestElement rest:
                BoundNames(rest.Argument, names);
                break;

            case ArrayPattern array:
                foreach (var element in array.Elements)
                {
                    if (element != null)
                        BoundNames(element, names);
                }
                break;

            case ObjectPattern @object:
                foreach (var property in @object.Properties)
                {
                    switch (property)
                    {
                        case PatternProperty patternProperty:
                            BoundNames(patternProperty.Value, names);
                            break;
                        case Pattern pattern:
                            BoundNames(pattern, names);
                            break;
                    }
                }
                break;
        }
    }
}
