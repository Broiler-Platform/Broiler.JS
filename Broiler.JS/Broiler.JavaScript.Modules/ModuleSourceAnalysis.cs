using System;
using System.Collections.Generic;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Runtime;

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
        var program = FastParser.ParseModule(code);
        var analysis = new ModuleSourceAnalysis { HasTopLevelAwait = program.IsAsync };
        analysis.Read(program);
        return analysis;
    }

    private ModuleRequest Request(AstLiteral source, IFastEnumerable<(StringSpan key, AstLiteral value)> attributes)
    {
        string type = null;
        if (attributes != null)
        {
            var e = attributes.GetFastEnumerator();
            while (e.MoveNext(out var attribute))
            {
                if (attribute.key.Equals("type"))
                    type = attribute.value.StringValue;
            }
        }

        var request = new ModuleRequest(source.StringValue, type);
        if (requests.TryGetValue(request.CacheKey, out var existing))
            return existing;

        requests[request.CacheKey] = request;
        RequestedModules.Add(request);
        return request;
    }

    private void Read(AstProgram program)
    {
        var exportEntries = new List<ExportEntry>();
        var statements = program.Statements.GetFastEnumerator();
        while (statements.MoveNext(out var statement))
        {
            switch (statement)
            {
                case AstImportStatement import:
                    var importRequest = Request(import.Source, import.Attributes);
                    if (import.Default != null)
                        ImportEntries.Add(new ImportEntry(importRequest, "default", import.Default.Name.Value));

                    if (import.All != null)
                        ImportEntries.Add(new ImportEntry(importRequest, null, import.All.Name.Value));

                    if (import.Members != null)
                    {
                        var members = import.Members.GetFastEnumerator();
                        while (members.MoveNext(out var member))
                            ImportEntries.Add(new ImportEntry(importRequest, member.name.Value, member.asName.Value));
                    }

                    break;

                case AstExportStatement export:
                    ReadExport(export, exportEntries);
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

    private void ReadExport(AstExportStatement export, List<ExportEntry> entries)
    {
        if (export.Source != null)
        {
            var request = Request((AstLiteral)export.Source, export.Attributes);

            if (export.Members != null)
            {
                var members = export.Members.GetFastEnumerator();
                while (members.MoveNext(out var member))
                    entries.Add(new ExportEntry(member.asName.Value, request, member.name.Value, null));

                return;
            }

            // `export * as ns from 'm'` exports m's namespace as `ns`; `export * from 'm'` is a
            // star entry (all-but-default, no export name).
            var namespaceName = export.Declaration switch
            {
                AstIdentifier identifier => identifier.Name.Value,
                AstLiteral literal => literal.StringValue,
                _ => null,
            };
            entries.Add(new ExportEntry(namespaceName, request, null, null));
            return;
        }

        if (export.Members != null)
        {
            var members = export.Members.GetFastEnumerator();
            while (members.MoveNext(out var member))
                entries.Add(new ExportEntry(member.asName.Value, null, null, member.name.Value));

            return;
        }

        switch (export.Declaration)
        {
            case AstVariableDeclaration declaration:
                var names = new List<string>();
                var declarators = declaration.Declarators.GetFastEnumerator();
                while (declarators.MoveNext(out var declarator))
                    BoundNames(declarator.Identifier, names);

                foreach (var name in names)
                    entries.Add(new ExportEntry(name, null, null, name));
                return;

            case AstFunctionExpression { IsStatement: true, Id: { } id }:
                entries.Add(new ExportEntry(export.IsDefault ? "default" : id.Name.Value, null, null, id.Name.Value));
                return;

            case AstClassExpression { IsDeclaration: true, Identifier: { } identifier }:
                entries.Add(new ExportEntry(export.IsDefault ? "default" : identifier.Name.Value, null, null, identifier.Name.Value));
                return;
        }

        if (export.IsDefault)
            entries.Add(new ExportEntry("default", null, null, "*default*"));
    }

    private static void BoundNames(AstExpression target, List<string> names)
    {
        switch (target)
        {
            case AstIdentifier identifier:
                names.Add(identifier.Name.Value);
                break;

            case AstBinaryExpression assignment:
                BoundNames(assignment.Left, names);
                break;

            case AstSpreadElement spread:
                BoundNames(spread.Argument, names);
                break;

            case Ast.Patterns.AstArrayPattern array:
                var elements = array.Elements.GetFastEnumerator();
                while (elements.MoveNext(out var element))
                {
                    if (element != null)
                        BoundNames(element, names);
                }
                break;

            case Ast.Patterns.AstObjectPattern @object:
                var properties = @object.Properties.GetFastEnumerator();
                while (properties.MoveNext(out var property))
                    BoundNames(property.Value, names);
                break;
        }
    }
}
