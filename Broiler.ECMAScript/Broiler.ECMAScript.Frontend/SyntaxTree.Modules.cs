// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   21
// Annotated:        21/21
// Exempt:           25
// Human-reviewed:   0/21
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       21
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>A WithClause's entry (ECMA-262 clause 16.2.2, ImportAttribute): a key and a string value.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=0; Fingerprint=CD1F51
// Broiler-Human:        PENDING
public sealed class ImportAttribute : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=CA06ED
    // Broiler-Human:        PENDING
    internal ImportAttribute(SourceSpan span, SyntaxNode key, StringLiteral value)
        : base(SyntaxKind.ImportAttribute, span)
    {
        Key = key;
        Value = value;
    }

    /// <summary>The AttributeKey: an <see cref="Syntax.IdentifierName"/> or a <see cref="StringLiteral"/>.</summary>
    public SyntaxNode Key { get; }

    /// <summary>The value.</summary>
    public StringLiteral Value { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=37AAC0
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Key);
        into.Add(Value);
    }
}

/// <summary>An ImportSpecifier (ECMA-262 clause 16.2.2): an imported name and the local binding.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=0; Fingerprint=35AF0D
// Broiler-Human:        PENDING
public sealed class ImportSpecifier : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=3637BB
    // Broiler-Human:        PENDING
    internal ImportSpecifier(SourceSpan span, SyntaxNode? imported, BindingIdentifier local)
        : base(SyntaxKind.ImportSpecifier, span)
    {
        Imported = imported;
        Local = local;
    }

    /// <summary>The ModuleExportName before <c>as</c>, an <see cref="Syntax.IdentifierName"/> or a <see cref="StringLiteral"/>; null where none is written and the binding's name is the imported one.</summary>
    public SyntaxNode? Imported { get; }

    /// <summary>The ImportedBinding.</summary>
    public BindingIdentifier Local { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=979820
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        Add(into, Imported);
        into.Add(Local);
    }
}

/// <summary>An ImportDeclaration (ECMA-262 clause 16.2.2), the import-phase declaration forms under their flags (decision D17).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=0; Fingerprint=CE20DD
// Broiler-Human:        PENDING
public sealed class ImportDeclaration : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=6385D7
    // Broiler-Human:        PENDING
    internal ImportDeclaration(
        SourceSpan span,
        ImportPhase phase,
        BindingIdentifier? defaultBinding,
        BindingIdentifier? namespaceBinding,
        IReadOnlyList<ImportSpecifier>? namedImports,
        bool namedImportsTrailingComma,
        StringLiteral source,
        IReadOnlyList<ImportAttribute>? attributes,
        bool attributesTrailingComma)
        : base(SyntaxKind.ImportDeclaration, span)
    {
        Phase = phase;
        DefaultBinding = defaultBinding;
        NamespaceBinding = namespaceBinding;
        NamedImports = namedImports;
        NamedImportsTrailingComma = namedImportsTrailingComma;
        Source = source;
        Attributes = attributes;
        AttributesTrailingComma = attributesTrailingComma;
    }

    /// <summary>The phase the declaration is written with.</summary>
    public ImportPhase Phase { get; }

    /// <summary>The ImportedDefaultBinding, or null.</summary>
    public BindingIdentifier? DefaultBinding { get; }

    /// <summary>The NameSpaceImport's binding, or null.</summary>
    public BindingIdentifier? NamespaceBinding { get; }

    /// <summary>The NamedImports' specifiers; null where no braces are written, empty for <c>{}</c>.</summary>
    public IReadOnlyList<ImportSpecifier>? NamedImports { get; }

    /// <summary>Whether a comma closes the NamedImports' list, a raw fact.</summary>
    public bool NamedImportsTrailingComma { get; }

    /// <summary>The ModuleSpecifier.</summary>
    public StringLiteral Source { get; }

    /// <summary>The WithClause's attributes; null where no <c>with</c> is written, empty for <c>with {}</c>.</summary>
    public IReadOnlyList<ImportAttribute>? Attributes { get; }

    /// <summary>Whether a comma closes the WithClause's list, a raw fact.</summary>
    public bool AttributesTrailingComma { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=744508
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        Add(into, DefaultBinding);
        Add(into, NamespaceBinding);
        if (NamedImports is not null)
            AddAll(into, NamedImports);
        into.Add(Source);
        if (Attributes is not null)
            AddAll(into, Attributes);
    }
}

/// <summary>An ExportSpecifier (ECMA-262 clause 16.2.3): a local name and the name it is exported as.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=0; Fingerprint=955512
// Broiler-Human:        PENDING
public sealed class ExportSpecifier : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=488EC4
    // Broiler-Human:        PENDING
    internal ExportSpecifier(SourceSpan span, SyntaxNode local, SyntaxNode? exported)
        : base(SyntaxKind.ExportSpecifier, span)
    {
        Local = local;
        Exported = exported;
    }

    /// <summary>The ModuleExportName before <c>as</c>: an <see cref="Syntax.IdentifierName"/> or a <see cref="StringLiteral"/>.</summary>
    public SyntaxNode Local { get; }

    /// <summary>The ModuleExportName after <c>as</c>, or null where none is written.</summary>
    public SyntaxNode? Exported { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=C6A1F3
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Local);
        Add(into, Exported);
    }
}

/// <summary>
/// An ExportDeclaration that exports names (ECMA-262 clause 16.2.3): <c>export</c> a declaration, or <c>export { … }</c>
/// with or without a FromClause.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=0; Fingerprint=2FCA4E
// Broiler-Human:        PENDING
public sealed class ExportNamedDeclaration : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=30F0A7
    // Broiler-Human:        PENDING
    internal ExportNamedDeclaration(
        SourceSpan span,
        Statement? declaration,
        IReadOnlyList<ExportSpecifier> specifiers,
        bool specifiersTrailingComma,
        StringLiteral? source,
        IReadOnlyList<ImportAttribute>? attributes,
        bool attributesTrailingComma)
        : base(SyntaxKind.ExportNamedDeclaration, span)
    {
        Declaration = declaration;
        Specifiers = specifiers;
        SpecifiersTrailingComma = specifiersTrailingComma;
        Source = source;
        Attributes = attributes;
        AttributesTrailingComma = attributesTrailingComma;
    }

    /// <summary>The exported VariableStatement, Declaration or class or function declaration, or null for <c>export { … }</c>.</summary>
    public Statement? Declaration { get; }

    /// <summary>The ExportSpecifiers; empty with a declaration.</summary>
    public IReadOnlyList<ExportSpecifier> Specifiers { get; }

    /// <summary>Whether a comma closes the ExportsList, a raw fact.</summary>
    public bool SpecifiersTrailingComma { get; }

    /// <summary>The FromClause's ModuleSpecifier, or null.</summary>
    public StringLiteral? Source { get; }

    /// <summary>The WithClause's attributes; null where none is written.</summary>
    public IReadOnlyList<ImportAttribute>? Attributes { get; }

    /// <summary>Whether a comma closes the WithClause's list, a raw fact.</summary>
    public bool AttributesTrailingComma { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=5C5264
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        Add(into, Declaration);
        AddAll(into, Specifiers);
        Add(into, Source);
        if (Attributes is not null)
            AddAll(into, Attributes);
    }
}

/// <summary><c>export default</c> (ECMA-262 clause 16.2.3): a function or class declaration, its name optional, or an AssignmentExpression.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=0; Fingerprint=60F7F5
// Broiler-Human:        PENDING
public sealed class ExportDefaultDeclaration : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=3E96F6
    // Broiler-Human:        PENDING
    internal ExportDefaultDeclaration(SourceSpan span, SyntaxNode declaration)
        : base(SyntaxKind.ExportDefaultDeclaration, span)
    {
        Declaration = declaration;
    }

    /// <summary>A <see cref="FunctionDeclaration"/>, a <see cref="ClassDeclaration"/> or an <see cref="Expression"/>.</summary>
    public SyntaxNode Declaration { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=14832C
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Declaration);
}

/// <summary><c>export * from</c> or <c>export * as name from</c> (ECMA-262 clause 16.2.3).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=0; Fingerprint=A46FEF
// Broiler-Human:        PENDING
public sealed class ExportAllDeclaration : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=B61FDE
    // Broiler-Human:        PENDING
    internal ExportAllDeclaration(SourceSpan span, SyntaxNode? exported, StringLiteral source, IReadOnlyList<ImportAttribute>? attributes, bool attributesTrailingComma)
        : base(SyntaxKind.ExportAllDeclaration, span)
    {
        Exported = exported;
        Source = source;
        Attributes = attributes;
        AttributesTrailingComma = attributesTrailingComma;
    }

    /// <summary>The ModuleExportName after <c>as</c>, or null.</summary>
    public SyntaxNode? Exported { get; }

    /// <summary>The ModuleSpecifier.</summary>
    public StringLiteral Source { get; }

    /// <summary>The WithClause's attributes; null where none is written.</summary>
    public IReadOnlyList<ImportAttribute>? Attributes { get; }

    /// <summary>Whether a comma closes the WithClause's list, a raw fact.</summary>
    public bool AttributesTrailingComma { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.2 modules tree; IP=Low; Security=Low; Resources=1; Fingerprint=543883
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        Add(into, Exported);
        into.Add(Source);
        if (Attributes is not null)
            AddAll(into, Attributes);
    }
}
