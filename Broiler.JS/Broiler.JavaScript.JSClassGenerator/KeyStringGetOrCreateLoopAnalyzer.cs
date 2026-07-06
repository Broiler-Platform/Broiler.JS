using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Broiler.JavaScript.JSClassGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class KeyStringGetOrCreateLoopAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BJS0001";

#pragma warning disable RS2008
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Hoist literal KeyString lookup out of loop",
        "Hoist KeyStrings.GetOrCreate(\"{0}\") out of this loop",
        "Performance",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Repeated literal KeyString lookups inside loops should use a static, local, or generated KeyString when the key is stable.");
#pragma warning restore RS2008

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (!TryGetLiteralKeyArgument(invocation, out var literal))
        {
            return;
        }

        if (!IsInsideLoop(invocation))
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (symbol?.Name != "GetOrCreate")
        {
            return;
        }

        if (symbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != "global::Broiler.JavaScript.Storage.KeyStrings")
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, literal.GetLocation(), literal.Token.ValueText));
    }

    private static bool TryGetLiteralKeyArgument(InvocationExpressionSyntax invocation, out LiteralExpressionSyntax literal)
    {
        literal = null!;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        if (memberAccess.Name.Identifier.ValueText != "GetOrCreate")
        {
            return false;
        }

        if (invocation.ArgumentList.Arguments.Count != 1)
        {
            return false;
        }

        if (invocation.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax argumentLiteral
            || !argumentLiteral.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return false;
        }

        literal = argumentLiteral;
        return true;
    }

    private static bool IsInsideLoop(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is ForStatementSyntax
                or ForEachStatementSyntax
                or ForEachVariableStatementSyntax
                or WhileStatementSyntax
                or DoStatementSyntax)
            {
                return true;
            }

            if (current is AnonymousFunctionExpressionSyntax
                or LocalFunctionStatementSyntax
                or BaseMethodDeclarationSyntax
                or AccessorDeclarationSyntax)
            {
                return false;
            }
        }

        return false;
    }
}
