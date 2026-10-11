using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ContractCheck.Analyzers;

/// <summary>Whether a file defines the <c>CONTRACTS_FULL</c> preprocessor symbol.</summary>
internal static class ContractsFull
{
    public const string Symbol = "CONTRACTS_FULL";

    /// <summary>
    /// True when the project defines <c>CONTRACTS_FULL</c> or the file's active <c>#define</c>/<c>#undef</c> directives leave it
    /// defined. C# only allows those directives before the first token, so they are all in its leading trivia.
    /// </summary>
    public static bool IsDefined(SyntaxTree tree, CancellationToken cancellationToken)
    {
        bool defined = tree.Options.PreprocessorSymbolNames.Contains(Symbol);
        SyntaxToken first = tree.GetRoot(cancellationToken).GetFirstToken(includeZeroWidth: true);
        foreach (SyntaxTrivia trivia in first.LeadingTrivia)
        {
            switch (trivia.GetStructure())
            {
                case DefineDirectiveTriviaSyntax { IsActive: true } define when define.Name.ValueText == Symbol:
                    defined = true;
                    break;
                case UndefDirectiveTriviaSyntax { IsActive: true } undef when undef.Name.ValueText == Symbol:
                    defined = false;
                    break;
            }
        }

        return defined;
    }
}
