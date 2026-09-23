using System;
using System.Collections.Generic;
using System.Reflection;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Module code: the module environment's prologue, and the ModuleItems that compile to nothing
/// because the module linker already did their work.
/// </summary>
/// <remarks>
/// <para>
/// A module body receives its environment as the argument <c>#module</c> (an
/// <see cref="IJSModuleEnvironment"/>, supplied by the linker). Before the first statement runs,
/// the prologue binds each imported name to the binding the linker created for it — an indirect
/// binding to the exporter's own binding, which is what makes imports live — and publishes each
/// local binding the module exports. It then suspends: the suspension ends InitializeEnvironment,
/// and evaluation resumes the body (see the module branch at the end of the FastCompiler
/// constructor).
/// </para>
/// <para>
/// Import declarations and export lists therefore compile to nothing. An exported declaration
/// compiles as the declaration itself, and <c>export default &lt;expression&gt;</c> initializes the
/// module's <c>*default*</c> binding.
/// </para>
/// </remarks>
partial class FastCompiler
{
    /// <summary>The binding of <c>export default</c> when the default has no name of its own
    /// (ES2024 16.2.3.2). Not an identifier, so no source text can refer to it.</summary>
    internal const string ModuleDefaultBindingName = "*default*";

    /// <summary>The argument a module body receives its environment in.</summary>
    internal const string ModuleEnvironmentArgument = "#module";

    private static readonly MethodInfo ModuleImportMethod = typeof(JSModuleLinkage).GetMethod(nameof(JSModuleLinkage.Import))
        ?? throw new InvalidOperationException("JSModuleLinkage.Import not found");

    private static readonly MethodInfo ModulePublishMethod = typeof(JSModuleLinkage).GetMethod(nameof(JSModuleLinkage.Publish))
        ?? throw new InvalidOperationException("JSModuleLinkage.Publish not found");

    private static readonly MethodInfo ModuleMetaMethod = typeof(JSModuleLinkage).GetMethod(nameof(JSModuleLinkage.Meta))
        ?? throw new InvalidOperationException("JSModuleLinkage.Meta not found");

    private static readonly MethodInfo NameAnonymousFunctionMethod = typeof(JSVariable).GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForField))
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForField not found");

    /// <summary>The module environment, or <c>undefined</c> for module code compiled without one
    /// (whose import bindings then fail when the prologue asks for them).</summary>
    private BExpression ModuleEnvironmentExpression()
        => scope.Top.GetVariable(ModuleEnvironmentArgument)?.Expression ?? JSUndefinedBuilder.Value;

    /// <summary>Whether <c>export default</c> of <paramref name="exported"/> binds <c>*default*</c>
    /// rather than a name of its own.</summary>
    private static bool IsDefaultExportBindingDefault(AstNode exported)
        => exported is not (AstFunctionExpression { IsStatement: true, Id: not null }
            or AstClassExpression { IsDeclaration: true, Identifier: not null });

    /// <summary>Creates a program-scope binding for every name an import declaration binds,
    /// initialized in the prologue from the module environment.</summary>
    private void DeclareModuleImportBindings(AstProgram program, FastFunctionScope programScope)
    {
        var environment = ModuleEnvironmentExpression();
        var statements = program.Statements.GetFastEnumerator();
        while (statements.MoveNext(out var statement))
        {
            if (statement is not AstImportStatement import)
                continue;

            if (import.Default != null)
                DeclareImportBinding(import, import.Default.Name);

            if (import.All != null)
                DeclareImportBinding(import, import.All.Name);

            if (import.Members != null)
            {
                var members = import.Members.GetFastEnumerator();
                while (members.MoveNext(out var member))
                    DeclareImportBinding(import, member.asName);
            }
        }

        void DeclareImportBinding(AstImportStatement import, in StringSpan localName)
        {
            // ImportedBindings are LexicallyDeclaredNames of the module (ES2024 16.2.1.1): a name
            // bound twice, or also declared by the module, is an early SyntaxError.
            if (programScope.TryGetOwnVariable(localName, out _))
                throw new FastParseException(import.Start, $"Identifier '{localName}' has already been declared");

            var binding = programScope.CreateVariable(localName, null, newScope: true);
            binding.SkipRegistration = true;

            // Shared with the function scope for the reason VisitProgram shares a module's
            // lexical bindings: a hoisted function's closure must be able to capture it.
            programScope.Parent?.AddExternalVariable(localName, binding);
            binding.SetInit(BExpression.Call(null, ModuleImportMethod, environment, BExpression.Constant(localName.Value)));
        }
    }

    /// <summary>
    /// Adds the module prologue to the start of the body: the hoisted anonymous default function,
    /// the publication of every local export, and the suspension that ends instantiation.
    /// </summary>
    private void AddModulePrologue(AstProgram program, FastFunctionScope programScope, Sequence<BExpression> blockList)
    {
        var environment = ModuleEnvironmentExpression();
        var published = new HashSet<string>(StringComparer.Ordinal);
        var exportedNames = new List<(string Name, AstStatement Statement)>();

        var statements = program.Statements.GetFastEnumerator();
        while (statements.MoveNext(out var statement))
        {
            if (statement is not AstExportStatement export)
                continue;

            // `export default function () {}` is a HoistableDeclaration bound to `*default*`: its
            // function object exists from instantiation, like any function declaration's, and is
            // named "default" (ES2024 16.2.3.7 InstantiateOrdinaryFunctionObject with "default").
            if (export.IsDefault && export.Declaration is AstFunctionExpression { IsStatement: true, Id: null } anonymous)
            {
                var defaultBinding = programScope.CreateVariable(ModuleDefaultBindingName, null, true);
                defaultBinding.SkipRegistration = true;
                programScope.Parent?.AddExternalVariable(ModuleDefaultBindingName, defaultBinding);
                defaultBinding.SetInit(JSVariableBuilder.New(JSUndefinedBuilder.Value, string.Empty));
                defaultBinding.SetPostInit(CreateFunction(anonymous, inferredFunctionName: "default"));
            }

            foreach (var name in LocalExportNames(export))
                exportedNames.Add((name, export));
        }

        foreach (var (name, statement) in exportedNames)
        {
            if (!published.Add(name))
                continue;

            var binding = programScope.GetVariable(new StringSpan(name));
            if (binding == null)
                throw new FastParseException(statement.Start, $"Export '{name}' is not defined in module");

            if (binding.Variable == null || binding.Variable.Type != typeof(JSVariable))
                throw new InvalidOperationException($"The exported binding '{name}' has no module environment cell.");

            blockList.Add(BExpression.Call(null, ModulePublishMethod, environment, BExpression.Constant(name), binding.Variable));
        }

        blockList.Add(BExpression.Yield(JSUndefinedBuilder.Value));
    }

    /// <summary>The local names an export declaration exports (the [[LocalName]] of its
    /// ExportEntries that have no [[ModuleRequest]]).</summary>
    private static IEnumerable<string> LocalExportNames(AstExportStatement export)
    {
        if (export.Source != null)
            yield break;

        if (export.Members != null)
        {
            var members = export.Members.GetFastEnumerator();
            while (members.MoveNext(out var member))
                yield return member.name.Value;

            yield break;
        }

        switch (export.Declaration)
        {
            case null:
                yield break;

            case AstVariableDeclaration declaration:
                var names = new HashSet<string>(StringComparer.Ordinal);
                var declarators = declaration.Declarators.GetFastEnumerator();
                while (declarators.MoveNext(out var declarator))
                    CollectBindingNames(declarator.Identifier, names);

                foreach (var name in names)
                    yield return name;
                yield break;

            case AstFunctionExpression { IsStatement: true, Id: { } id }:
                yield return id.Name.Value;
                yield break;

            case AstClassExpression { IsDeclaration: true, Identifier: { } identifier }:
                yield return identifier.Name.Value;
                yield break;
        }

        if (export.IsDefault)
            yield return ModuleDefaultBindingName;
    }

    protected override BExpression VisitExportStatement(AstExportStatement exportStatement)
    {
        // An ExportDeclaration is a ModuleItem; the parser only produces one in module code.
        if (!isModuleCompilation)
            throw new FastParseException(exportStatement.Start, "'export' is only valid inside a module");

        // Export lists, re-exports and `export *` are resolved by the linker and run nothing.
        if (exportStatement.Members != null || exportStatement.Source != null)
            return BExpression.Empty;

        var declaration = exportStatement.Declaration;
        switch (declaration)
        {
            case null:
                return BExpression.Empty;

            // A function declaration, named or not, is instantiated by the prologue.
            case AstFunctionExpression { IsStatement: true, Id: null }:
                return BExpression.Empty;

            case AstFunctionExpression { IsStatement: true }:
                Visit(declaration);
                return BExpression.Empty;

            case AstVariableDeclaration:
                return Visit(declaration);

            case AstClassExpression { IsDeclaration: true, Identifier: not null }:
                return Visit(declaration);
        }

        if (!exportStatement.IsDefault)
            throw new FastParseException(exportStatement.Start, $"Unexpected export type {declaration.Type}");

        // `export default <AssignmentExpression>` and `export default class {}`: evaluate, name an
        // anonymous function or class "default" (NamedEvaluation), and initialize `*default*`.
        var value = declaration is AstClassExpression anonymousClass
            ? CreateClass(anonymousClass.Identifier, anonymousClass.Base, anonymousClass)
            : VisitExpression((AstExpression)declaration);

        if (declaration is AstFunctionExpression { Id: null } or AstClassExpression { Identifier: null })
            value = BExpression.Call(null, NameAnonymousFunctionMethod, value, BExpression.Constant("default"));

        var defaultBinding = scope.Top.GetVariable(ModuleDefaultBindingName)
            ?? throw new FastParseException(exportStatement.Start, "The module has no default export binding");
        return BExpression.Assign(defaultBinding.Expression, value);
    }

    protected override BExpression VisitImportStatement(AstImportStatement importStatement)
    {
        if (!isModuleCompilation)
            throw new FastParseException(importStatement.Start, "Cannot use import statement outside a module");

        // The bindings are created by the prologue (DeclareModuleImportBindings); the module the
        // declaration names was loaded, linked and evaluated by the linker.
        return BExpression.Empty;
    }

    /// <summary><c>import.meta</c>: the module environment's meta object.</summary>
    private BExpression ModuleImportMeta() => BExpression.Call(null, ModuleMetaMethod, ModuleEnvironmentExpression());
}
