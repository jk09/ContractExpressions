using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using ContractCheck.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace ContractCheck.CodeFixes;

/// <summary>Fixes CCK0006 by adding <c>#define CONTRACTS_FULL</c> as the first line of the file.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DefineContractsFullCodeFix))]
[Shared]
public sealed class DefineContractsFullCodeFix : CodeFixProvider
{
    private const string Title = "Add #define CONTRACTS_FULL";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(Descriptors.AttributeCompiledAway.Id);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() =>
        FixAllProvider.Create(static async (context, document, diagnostics) =>
            diagnostics.IsEmpty ? document : await DefineAsync(document, context.CancellationToken).ConfigureAwait(false));

    /// <inheritdoc />
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(Title, cancellationToken => DefineAsync(context.Document, cancellationToken), equivalenceKey: Title),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> DefineAsync(Document document, CancellationToken cancellationToken)
    {
        SyntaxTree? tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (tree is null || ContractsFull.IsDefined(tree, cancellationToken))
        {
            return document;
        }

        // The file's own line break; a directive must end its line.
        SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        TextLine firstLine = text.Lines[0];
        string newLine = firstLine.EndIncludingLineBreak > firstLine.End
            ? text.ToString(TextSpan.FromBounds(firstLine.End, firstLine.EndIncludingLineBreak))
            : "\n";
        return document.WithText(text.WithChanges(new TextChange(new TextSpan(0, 0), $"#define {ContractsFull.Symbol}{newLine}")));
    }
}
