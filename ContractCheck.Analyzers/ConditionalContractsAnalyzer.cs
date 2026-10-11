using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ContractCheck.Analyzers;

/// <summary>
/// Reports the two <c>CONTRACTS_FULL</c> pitfalls: contract attributes the compiler removes because the file doesn't define
/// the symbol (CCK0006), and <c>Contract</c> calls in ordinary code that terminate the process without the CodeContracts
/// rewriter (CCK0007).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConditionalContractsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>The attributes ContractCheck reads, all <c>[Conditional("CONTRACTS_FULL")]</c> (<c>ContractClass</c> also on <c>DEBUG</c>).</summary>
    private static readonly string[] AttributeNames = ["Pure", "ContractOption", "ContractRuntimeIgnored", "ContractClass", "ContractClassFor"];

    /// <summary>The <c>Contract</c> methods that call <c>Environment.FailFast</c> unless the CodeContracts rewriter replaced them.</summary>
    private static readonly string[] RewriterMethods = ["Requires", "Ensures", "EnsuresOnThrow", "Invariant"];

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.AttributeCompiledAway, Descriptors.ContractCallTerminatesProcess);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static start =>
        {
            if (KnownSymbols.Create(start.Compilation) is not { } symbols)
            {
                return;
            }

            var attributes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            foreach (string name in AttributeNames)
            {
                if (start.Compilation.GetTypeByMetadataName($"System.Diagnostics.Contracts.{name}Attribute") is { } attribute)
                {
                    attributes.Add(attribute);
                }
            }

            INamedTypeSymbol? expression = start.Compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression");
            start.RegisterSyntaxNodeAction(c => AnalyzeAttribute(c, attributes), SyntaxKind.Attribute);
            start.RegisterSyntaxNodeAction(c => AnalyzeInvocation(c, symbols.Contract, expression), SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context, HashSet<INamedTypeSymbol> attributes)
    {
        var attribute = (AttributeSyntax)context.Node;
        string name = RightmostName(attribute.Name);
        if (name.EndsWith("Attribute", System.StringComparison.Ordinal))
        {
            name = name.Substring(0, name.Length - "Attribute".Length);
        }

        if (!AttributeNames.Contains(name)
            || context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol is not IMethodSymbol { ContainingType: { } type }
            || !attributes.Contains(type)
            || ContractsFull.IsDefined(attribute.SyntaxTree, context.CancellationToken))
        {
            return;
        }

        string display = type.Name.Substring(0, type.Name.Length - "Attribute".Length);
        string when = display == "ContractClass" ? " (in Release builds, where DEBUG isn't defined either)" : "";
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.AttributeCompiledAway, attribute.GetLocation(), display, when));
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol contract, INamedTypeSymbol? expression)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (!RewriterMethods.Contains(InvokedName(invocation.Expression))
            || context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method
            || !SymbolEqualityComparer.Default.Equals(method.ContainingType, contract)
            || !RewriterMethods.Contains(method.Name))
        {
            return;
        }

        // Requires<TException> is not [Conditional]: it is compiled into every build.
        bool alwaysCompiled = method is { Name: "Requires", IsGenericMethod: true };
        if ((!alwaysCompiled && !ContractsFull.IsDefined(invocation.SyntaxTree, context.CancellationToken))
            || InExpressionTree(invocation, context.SemanticModel, expression, context.CancellationToken) != false)
        {
            return;
        }

        string display = method.IsGenericMethod
            ? $"{method.Name}<{method.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}>"
            : method.Name;
        string because = alwaysCompiled ? "Requires<TException> is never compiled away" : "CONTRACTS_FULL is defined";
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.ContractCallTerminatesProcess, invocation.GetLocation(), display, because));
    }

    /// <summary>
    /// True when an enclosing lambda is converted to an expression tree, false when none is, and null when a lambda's
    /// conversion is unknown (code that doesn't bind yet), so nothing is reported.
    /// </summary>
    private static bool? InExpressionTree(SyntaxNode node, SemanticModel model, INamedTypeSymbol? expression, System.Threading.CancellationToken cancellationToken)
    {
        for (SyntaxNode? current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is not AnonymousFunctionExpressionSyntax function)
            {
                continue;
            }

            ITypeSymbol? converted = model.GetTypeInfo(function, cancellationToken).ConvertedType;
            if (converted is null || converted.TypeKind == TypeKind.Error)
            {
                return null;
            }

            for (ITypeSymbol? type = converted; type is not null; type = type.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(type, expression))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string InvokedName(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
        SimpleNameSyntax name => name.Identifier.ValueText,
        _ => "",
    };

    private static string RightmostName(NameSyntax name) => name switch
    {
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
        SimpleNameSyntax simple => simple.Identifier.ValueText,
        _ => "",
    };
}
